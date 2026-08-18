# Instance Service

Der Instance Service ist verantwortlich für die Verwaltung und Klassifizierung von BIM-Instanzen im Gaeco-Ökosystem. Er stellt eine API zur Verfügung, über die Instanzen abgerufen, erstellt und klassifiziert werden können. Instanzdaten werden über eine Gremlin/TinkerPop-kompatible Graphdatenbank, aktuell ArcadeDB, sowie PostgreSQL persistiert; Ontologie- und Guideline-Artefakte werden aus MinIO geladen. Import-Events werden über Kafka konsumiert.

## Enthaltene Dienste

- **Instance Server** (`instance-server`) – .NET Backend, erreichbar über Traefik unter `INSTANCE_SERVER_HOSTNAME`
- **Instance Client** (`instance-client`) – Vue-Frontend, eingebunden unter dem Pfad `MOUNT_ROUTE`
- **instance-postgres** – PostgreSQL-Datenbank für den Instance Service
- **instance-arcadedb** – ArcadeDB mit aktiviertem Gremlin-Server für die Instanzklassifizierung
