# Vollständigkeitsprüfung im Graph-Datenmodell (CompletenessCheck)

## Übersicht

Die `CompletenessCheck`-Klasse validiert Graph-Daten gegen UseCase-spezifische Anforderungen und sendet vollständige Subgraphen als Kafka-Messages. Ein Subgraph gilt als **vollständig**, wenn:

1. **Alle erforderlichen Klassifikationen** für den UseCase vorhanden sind
2. **Alle erforderlichen Properties** mit Daten gefüllt sind (nicht leer)
3. **Nur relevante Knoten** im Subgraphen enthalten sind (keine "fremden" Klassen)

---

## Kernkonzepte

### UseCase-Klassifikations-Mapping

Jeder UseCase definiert eine Liste relevanter Klassifikationen über **AccessRights**:

```csharp
// Beispiel-Mapping (initialisiert aus AccessRights)
UseCaseId "A" → [Portfolio, Building, Address]
UseCaseId "B" → [Portfolio, Building]
```

Die Initialisierung erfolgt in `InitializeUseCaseClassificationMapAsync()`:

- Lädt alle AccessRights
- Gruppiert nach UseCaseId
- Extrahiert eindeutige GuidelineClassificationIds

### Vollständigkeitskriterien

Ein Subgraph ist vollständig, wenn **ALLE** folgenden Bedingungen erfüllt sind:

#### 1. Strukturelle Vollständigkeit

**Regel:** Jede im UseCase definierte Klassifikation muss mindestens einmal vorkommen.

```csharp
// IsUseCaseCompleteAsync() - Strukturelle Prüfung
var presentClassifications = queryResult.Select(node => node.ClassificationId).ToHashSet();
if (!relevantClasses.All(presentClassifications.Contains))
    return false; // ✗ Unvollständig
```

**Beispiel:**

```
UseCase A: [Portfolio, Building, Address]

Graph 1: Portfolio P1 → Building B1 → Address A1
→ ✓ VOLLSTÄNDIG (alle 3 Klassen vorhanden)

Graph 2: Portfolio P2 → Building B2
→ ✗ UNVOLLSTÄNDIG (Address fehlt)

Graph 3: Building B3
→ ✗ UNVOLLSTÄNDIG (Portfolio und Address fehlen)
```

#### 2. Property-Vollständigkeit

**Regel:** Alle Properties mit `PropertyRight.Read` müssen für jede Instanz gefüllt sein (nicht null/empty).

```csharp
// AreAllPropertiesCompleteAsync() - Property-Prüfung
var requiredPropertiesByClassification = accessRightsList
    .Where(ar => ar.Right == PropertyRight.Read)
    .GroupBy(ar => ar.GuidelineClassificationId)
    .ToDictionary(g => g.Key, g => g.Select(ar => ar.Name).Distinct().ToList());

foreach (var instance in instances)
{
    if (requiredProperties.Any(propertyName =>
        !instance.Properties.TryGetValue(propertyName, out string propertyValue) ||
        string.IsNullOrEmpty(propertyValue)))
    {
        return false; // ✗ Property fehlt oder leer
    }
}
```

**Beispiel:**

```
AccessRights für UseCase A:
- Portfolio: [Name (Read), Description (Read)]
- Building: [BuildingName (Read), YearBuilt (Read)]
- Address: [Street (Read), City (Read)]

Instance Portfolio P1:
  Properties: { Name: "MyPortfolio", Description: "Test" } → ✓ OK

Instance Building B1:
  Properties: { BuildingName: "Tower A", YearBuilt: "" } → ✗ FEHLER (YearBuilt leer)
```

#### 3. Pfad-Reinheit (keine fremden Knoten)

**Regel:** Alle Knoten auf dem Pfad zwischen relevanten Knoten müssen selbst relevant sein.

```groovy
// Gremlin Query mit Pfad-Validierung
g.V().Has("Instance", "Id", instanceId)
  .Repeat(__.Both().HasLabel("Instance").SimplePath())
  .Until(__.Loops().Is(P.Gte(255)))
  .Emit()
  .Has("ClassificationId", P.Within(relevantClasses))
  .Dedup()
```

**Beispiel:**

```
UseCase A: [Portfolio, Building, Address]

Graph 1: Portfolio → Building → Address
→ ✓ VALIDE (alle Knoten relevant)

Graph 2: Portfolio → Vehicle → Building
→ ✗ INVALIDE (Vehicle ist nicht relevant für UseCase A)
→ Building wird NICHT gefunden, da Pfad "unrein"
```

---

## Ausführung im Hintergrund

Die Prüfung traversiert den gesamten Subgraphen einer Instanz und ist damit auf großen
Graphen zu langsam, um innerhalb einer Schreiboperation zu laufen. Schreibende Consumer
(Instanz anlegen/aktualisieren/löschen, Relationen anlegen/löschen) sowie der
`GraphDataModelConsumer` rufen den `CompletenessCheck` deshalb **nicht** direkt auf,
sondern melden die betroffenen Instanz-IDs an den `ICompletenessCheckScheduler`:

```csharp
await _repository.UpdateInstance(request.InstanceId, request.Name, updatableProperties);
_completenessCheckScheduler.Schedule(request.InstanceId);   // blockiert nicht
```

Die Response geht damit sofort raus; die Prüfung läuft im `CompletenessCheckWorker`
(`BackgroundService`) weiter.

**Ablauf:**

```
1. Schreiboperation committet die Änderung
2. Schedule(instanceId) schreibt die ID in einen unbounded Channel (nicht blockierend)
3. Response an den Client
4. Worker liest die erste ID und wartet 500 ms auf weitere IDs (Debounce,
   maximal 5 s Sammelfenster)
5. Worker führt CheckAndSendAsync(string[]) für den gesammelten Batch aus
   → eigener DI-Scope pro Batch
```

**Warum ein Batch:** Eine Änderung betrifft meist denselben Subgraphen wie die
unmittelbar folgenden (z. B. ein Import, der viele Relationen anlegt). Das Sammeln
nutzt die Duplikats-Vermeidung von `CheckAndSendAsync(string[])`, statt denselben
Subgraphen einmal pro berührter Instanz zu senden.

**Konsequenzen:**

- Die Kafka-Message eines vollständigen Subgraphen erscheint ~0,5 s nach der Änderung,
  asynchron zur Response.
- Fehler beim Prüfen werden geloggt und erreichen den Client nicht. Eine einzelne
  fehlerhafte Instanz bricht den Batch nicht ab.
- Die Warteschlange liegt nur im Prozessspeicher. Ein Neustart verwirft offene Prüfungen;
  das ist unkritisch, weil die Prüfung idempotent ist und von der nächsten Änderung am
  Subgraphen erneut ausgelöst wird.

---

## API-Methoden

### 1. `CheckAndSendAsync(string instanceId)`

**Event-basierte Prüfung für eine einzelne Instanz**

Wird über den `ICompletenessCheckScheduler` nach Änderungen an einer Node getriggert
(siehe [Ausführung im Hintergrund](#ausführung-im-hintergrund)).

**Ablauf:**

```
1. Initialisiere UseCase-Mappings
2. Für jeden UseCase:
   a. Prüfe Vollständigkeit mit IsUseCaseCompleteAsync()
   b. Bei vollständig:
      - Lade Subgraph mit ExecuteCompletenessQueryAsync()
      - Sende Kafka-Message mit SendGraphDataAsync()
```

**Beispiel-Aufruf:**

```csharp
await _completenessCheck.CheckAndSendAsync("Address-123");
// Prüft alle UseCases, die Address-Klasse enthalten
// Sendet Messages für alle vollständigen Subgraphen
```

---

### 2. `CheckAndSendAsync(string[] instanceIds)`

**Batch-Prüfung für multiple Instanzen**

Optimiert für die gleichzeitige Verarbeitung mehrerer Instanzen mit **Duplikats-Vermeidung**.

**Ablauf:**

```
1. Initialisiere UseCase-Mappings
2. Erstelle Tracking-Dictionary: sentSubgraphsByUseCase
   { UseCaseId → HashSet<processedInstanceIds> }
3. Für jeden instanceId:
   Für jeden UseCase:
     a. IF instanceId bereits in sentSubgraphsByUseCase[UseCaseId] → SKIP
     b. ELSE: Prüfe Vollständigkeit
     c. Bei vollständig:
        - Sende Message
        - Füge ALLE Instanzen des Subgraphen zu sentSubgraphsByUseCase[UseCaseId] hinzu
```

**Duplikats-Vermeidung:**

```
Gegeben:
- instanceIds: [P1, B1, A1, P2, B2]
- UseCase A: [Portfolio, Building, Address]
- Graph: P1 → B1 → A1 (vollständig)

Ablauf:
1. P1: Vollständig → Sende Message → sentSubgraphs[A] = {P1, B1, A1}
2. B1: B1 in sentSubgraphs[A] → SKIP (keine doppelte Message)
3. A1: A1 in sentSubgraphs[A] → SKIP
4. P2: P2 nicht in sentSubgraphs[A] → Prüfe...
```

---

### 3. `IsUseCaseCompleteAsync(string instanceId, string useCaseId)`

**Prüft einen einzelnen UseCase für eine Instanz**

**Ablauf:**

```
1. Validierung: instanceId, useCaseId, relevantClasses vorhanden?
2. Lade Instance: Prüfe, ob ClassificationId relevant für UseCase
3. Query: Lade Subgraph mit ExecuteCompletenessQueryAsync()
4. Strukturelle Prüfung: Alle Klassifikationen vorhanden?
5. Property-Prüfung: AreAllPropertiesCompleteAsync()
6. Return: true nur wenn ALLES erfüllt
```

**Exception:**

```csharp
if (!queryResult.Any())
    throw new InvalidOperationException(
        $"No related instances found for instance {instanceId} and use case {useCaseId}");
```

→ Mindestens die Start-Instanz muss zurückkommen!

---

### 4. `FindAndSendCompleteSubgraphsAsync(string useCaseId)`

**Globale Suche ohne Start-Instanz**

Durchsucht den **gesamten Graph** nach vollständigen Subgraphen für einen UseCase.

**Anwendungsfälle:**

- Nächtliche Batch-Jobs
- Initiale Synchronisation
- Recovery nach Ausfall
- Manuelle Daten-Exports

**Algorithmus:**

#### Phase 1: Kandidaten-Identifikation

```csharp
var candidateInstances = await _graphQueryExecutor.FindCandidateInstancesAsync(relevantClasses);
// Findet ALLE Instanzen mit relevanten Klassifikationen im gesamten Graph
```

#### Phase 2: Iterative Validierung mit Duplikats-Schutz

```csharp
var processedInstances = new HashSet<string>();
var completeSubgraphRoots = new List<string>();

foreach (var instanceId in candidateInstances.Select(i => i.Id))
{
    if (processedInstances.Contains(instanceId))
        continue; // ← DUPLIKATS-SCHUTZ

    if (await ProcessInstanceForCompleteness(instanceId, useCaseId, relevantClasses, processedInstances))
    {
        completeSubgraphRoots.Add(instanceId);
        // processedInstances enthält jetzt ALLE Instanzen des Subgraphen
    }
    else
    {
        processedInstances.Add(instanceId); // Auch bei Fehlschlag markieren
    }
}
```

#### Phase 3: Message-Generierung

Für jeden vollständigen Subgraphen wird `SendGraphDataAsync()` aufgerufen.

**Beispiel-Durchlauf:**

```
UseCase A: [Portfolio, Building, Address]

Graph:
  P1 → B1 → A1 (vollständig)
  P2 → B2 → A2 (vollständig)
  B3 (isoliert, unvollständig)

Kandidaten: [P1, P2, B1, B2, B3, A1, A2]
processedInstances = {}

1. P1:
   - Vollständig → Message 1: (P1, B1, A1)
   - processedInstances = {P1, B1, A1}

2. P2:
   - Vollständig → Message 2: (P2, B2, A2)
   - processedInstances = {P1, B1, A1, P2, B2, A2}

3. B1: IN processedInstances → SKIP
4. B2: IN processedInstances → SKIP
5. B3: Unvollständig → processedInstances = {..., B3}
6. A1: IN processedInstances → SKIP
7. A2: IN processedInstances → SKIP

Ergebnis:
- 2 Messages gesendet
- completeSubgraphRoots = [P1, P2]
```

---

## Kafka-Message-Struktur

### Methode: `SendGraphDataAsync()`

Generiert und sendet eine `GraphDataModel`-Message für einen vollständigen Subgraphen.

#### Message-Komponenten

```csharp
var graphDataModel = new GraphDataModel
{
    GraphTemplate = "@prefix ex: <http://example.org/> .\nNode1 hasRelation Node2 .",
    GraphMetadata = metaDataNodes,      // 1. Instanz-Details
    GraphData = turtleRelations,        // 2. Relations in RDF/Turtle
    UseCase = new UseCase { Id = useCaseId },
    AccessRights = relevantAccessRights // 3. Zugriffsrechte
};
```

#### 1. GraphMetadata

```csharp
var metaDataNodes = subgraphInstances.Select(node => new MetaDataNode
{
    Id = node.Id,                        // z.B. "Portfolio-123"
    ClassType = node.ClassificationId,   // z.B. "Portfolio"
    PropertiesValues = node.Properties   // z.B. { "Name": "Test", "Description": "..." }
}).ToList();
```

#### 2. GraphData (Relations)

```csharp
// Nur Relations INNERHALB des Subgraphen
var instanceIds = subgraphInstances.Select(i => i.Id).ToHashSet();

foreach (var instance in subgraphInstances)
{
    foreach (var relation in instance.Relations.Where(r =>
        instanceIds.Contains(r.SubjectId) &&
        instanceIds.Contains(r.ObjectId))) // ← Beide Enden im Subgraphen
    {
        var subjectNode = graph.CreateUriNode(new Uri($"http://example.org/instances/{relation.SubjectId}"));
        var objectNode = graph.CreateUriNode(new Uri($"http://example.org/instances/{relation.ObjectId}"));
        var predicateNode = /* Label als URI */;

        relations.Add(new Triple(subjectNode, predicateNode, objectNode));
    }
}

// Konvertierung zu Turtle-Format
var turtleString = GraphProcessingService.ConvertRelationsToTurtle(relations, graph);
```

#### 3. Kafka-Topic und Headers

```csharp
var topicName = $"ekkodale.gaeco.instance.public.{useCaseId}.gaecoExport";

var headers = new Dictionary<string, object>
{
    { "useCase", useCaseId },
    { "usergroup", "gaeco" },
    { "version", "v1" },
    { "event", "dump" },
    { "entity", "GraphDataModel" }
};
```

---

## Gremlin-Queries

### Subgraph-Traversierung

**ExecuteCompletenessQueryAsync():**

```groovy
g.V().Has("Instance", "Id", instanceId)
  .Repeat(__.Both().HasLabel("Instance").SimplePath())
  .Until(__.Loops().Is(P.Gte(255)))
  .Emit()
  .Has("ClassificationId", P.Within(relevantClasses))
  .Dedup()
  .Values("Id")
```

**Parameter:**

- `instanceId`: Start-Instanz
- `relevantClasses`: Liste der UseCase-Klassifikationen

**Funktionsweise:**

- `Both()`: Bidirektionale Traversierung (keine Pfeilrichtung)
- `SimplePath()`: Verhindert Zyklen durch Ausschluss bereits besuchter Knoten
- `Until(Loops().Is(P.Gte(255)))`: Maximal 255 Hops
- `Emit()`: Gibt jeden besuchten Knoten aus (nicht nur den letzten)
- `Has("ClassificationId", P.Within(...))`: Filtert nach relevanten Klassifikationen
- `Dedup()`: Eindeutige Instanzen zurückgeben

**Warum bidirektional?**

```
Portfolio → Building → Address
    ↑           ↓
    └───────────┘

Start bei Address:
- Unidirektional: Findet nur Address (folgt nicht "rückwärts")
- Bidirektional: Findet Address, Building, Portfolio ✓
```

### Kandidaten-Suche

**FindCandidateInstancesAsync():**

```groovy
g.V().HasLabel("Instance")
  .Has("ClassificationId", P.Within(relevantClasses))
  .ElementMap()
```

Findet alle Instanzen, die potenziell Teil eines vollständigen Subgraphen sein könnten.

---

## Performance-Optimierungen

### 1. AccessRights-Caching

```csharp
private IEnumerable<AccessRight>? _accessRightsCache;

private async Task<IEnumerable<AccessRight>> GetAccessRightsAsync()
{
    return _accessRightsCache ??= await _accessRightsFetcher.GetAccessRightsAsync();
}
```

→ AccessRights werden nur einmal pro CompletenessCheck-Instanz geladen.

### 2. UseCase-Mapping-Cache

```csharp
private readonly ConcurrentDictionary<string, List<string>> _useCaseClassificationMap = new();

private async Task InitializeUseCaseClassificationMapAsync()
{
    if (_useCaseClassificationMap.Any())
        return; // ← Bereits initialisiert
    // ...
}
```

→ UseCase-Klassifikations-Zuordnungen werden nur einmal berechnet.

### 3. Duplikats-Vermeidung

- **CheckAndSendAsync(string[]):** HashSet pro UseCase
- **FindAndSendCompleteSubgraphsAsync():** Globales HashSet
  → Verhindert mehrfaches Senden desselben Subgraphen.

---

## Fehlerbehandlung

### Validierungen

```csharp
// Leere Parameter
if (string.IsNullOrEmpty(instanceId))
    return; // Stiller Abbruch

// Keine Kandidaten
if (!candidateInstances.Any())
{
    _logger.LogInformation("No candidate instances found for use case {UseCaseId}", useCaseId);
    return new List<string>();
}
```

### Exceptions

```csharp
// No related instances found
throw new InvalidOperationException(
    $"No related instances found for instance {instanceId} and use case {useCaseId}");

// Null-Parameter
ArgumentNullException.ThrowIfNull(subgraphInstances);
```

### Logging

```csharp
// Erfolg
_logger.LogInformation("Use case {UseCaseId} complete for instance {InstanceId}", useCaseId, instanceId);

// Fehler
_logger.LogError(ex, "Failed to send message for use case {UseCaseId}", useCaseId);

// Warnung
_logger.LogWarning("No instance IDs provided for completeness check");
```

---

## Zusammenfassung: Vollständigkeitskriterien

Ein Subgraph wird als **vollständig** erkannt und gesendet, wenn:

| Kriterium             | Prüfung                                         | Methode                                                |
| --------------------- | ----------------------------------------------- | ------------------------------------------------------ |
| **1. Strukturell**    | Alle UseCase-Klassifikationen vorhanden         | `presentClassifications.All(relevantClasses.Contains)` |
| **2. Properties**     | Alle Read-Properties gefüllt (nicht leer)       | `AreAllPropertiesCompleteAsync()`                      |
| **3. Pfad-Reinheit**  | Keine fremden Knoten zwischen relevanten Knoten | Gremlin `SimplePath()` + `Has("ClassificationId", P.Within(...))` |
| **4. Start-Relevanz** | Start-Instanz hat relevante Klassifikation      | `relevantClasses.Contains(instance.ClassificationId)`  |

**Alle 4 Kriterien müssen erfüllt sein!** Fehlt auch nur eines, ist der Subgraph unvollständig und wird nicht gesendet.
