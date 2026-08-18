# GraphDataModelValidationService Documentation

## Overview
The `GraphDataModelValidationService` validates graph data models by checking class types, relationships, and access rights to ensure data integrity and compliance.

## Dependencies
- `IGuidelineProvider` - Provides class type classifications
- `IOntologyProvider` - Provides relationship validation rules
- `AccessRightHelper` - Validates user permissions
- `ILogger` - Logging service

## Core Methods

### ValidateAsync(GraphDataModel model)
Main validation method that orchestrates all validation checks.

**Process:**
1. Validates class types against guideline classifications
2. Validates relationships via `ValidateRelationshipsAsync`
3. Returns merged validation results

**Returns:** `ValidationResult` with any validation errors

### ValidateRelationshipsAsync(GraphDataModel model)
Validates RDF relationships against ontology rules.

**Process:**
1. Parses graph data using Turtle format
2. Retrieves valid relationships from ontology
3. Validates each triple (subject-predicate-object)
4. Reports invalid relationships

### ValidateAccessRightsAsync(model, groupIds, useCaseId, accessRights)
Validates user permissions for graph operations.

**Validates:**
- **Create Rights** - Can create instances of class types
- **Property Rights** - Can set properties on instances  
- **Relationship Rights** - Can create relationships between classes

## Helper Methods

| Method | Purpose |
|--------|---------|
| `GetValidRelationshipsFromOntology()` | Retrieves valid relationship patterns as HashSet |
| `ValidateRelationship()` | Validates single triple against ontology rules |
| `ExtractNodeId(uri)` | Extracts node ID from URI |
| `ValidateRelationAccessRights()` | Validates relationship creation permissions |

## ValidationResult Class
```csharp
public class ValidationResult
{
    public List<string> Errors { get; }
    public bool IsValid => !Errors.Any();
    
    public void AddError(string error)
    public void MergeErrors(ValidationResult other)
}
```

## Error Types
- **Invalid ClassType** - Class not found in guidelines
- **Invalid Relationship** - Relationship not allowed by ontology
- **Insufficient Access Rights** - User lacks required permissions
- **Internal Errors** - Exception handling fallbacks

## Usage Example
```csharp
var result = await validationService.ValidateAsync(graphDataModel);
if (!result.IsValid)
{
    foreach (var error in result.Errors)
        Console.WriteLine(error);
}
```

## Integration
- **Used by:** `GraphDataModelConsumer` for validating incoming data
- **Tested by:** `GraphDataModelValidationServiceTests` with comprehensive unit tests