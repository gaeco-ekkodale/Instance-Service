## Federated Query Engine

A small version of a `Federated Query Engine` is implemented inside the `InstanceService`. This engine provides limited capabilities for pre-filtering the graph you wish to query using a Cypher-like filter syntax.

A query submitted to the `Instance Service` must consist of the following three clauses:

### 1. `MATCH` clause

The `MATCH` clause is used to specify the pattern of nodes and relationships to search for in the graph.

- **Structure:** It must always follow the pattern `(a)-[r]->(b)`.
  - Round braces represent nodes.
  - Square braces represent relations.
  - The arrow represents the direction.
- **Directionality:** The direction of the relationship is significant and can be reversed (e.g., `(a)<-[r]-(b)`).
- **Node Labels:** You can specify a label for a node using a colon (e.g., `(instance:Instance)`).
- **Purpose:** The primary purpose of this clause is to declare the variables (e.g., `a`, `r`, `b`) that will be used in subsequent `WHERE` and `RETURN` clauses.

### 2. `WHERE` clause

The `WHERE` clause is used to filter the instances specified inside the `MATCH` clause. 

- **Supported Functions:** Supported query functions are currently limited to the following string operations:
    - `CONTAINS`
    - `ENDS WITH`
    - `STARTS WITH`

- **Logical Operators:** Standard logical operators are also supported for combining conditions:
    - `AND`
    - `OR`
    - `NOT`

- **Assignment Operators:** Assignment operators are also supported for comparisons:    
    - `NOT EQUAL`
    - `EQUAL`
    - `!=`
    - `=`

### 3. `RETURN` clause

The `RETURN` clause specifies which variables declared in the `MATCH` clause should be included in the final output.

- **Syntax:** List the variables you want to return, separated by commas.

### Key Concepts & Limitations

- **Accessing Properties:** Properties of a node or relationship can be accessed using the syntax: `variable.Properties.PropertyName`.

- **Data Type Limitation:** All comparisons are performed lexicographically.

- **Query Failure Condition:** A query will fail if you filter a large group of instances and then try to use a specific property that isn't present in every instance from that group.

- **Internal Object Model:** The graph entities available for filtering align with the following internal structures:
    - `Instance`: Represents nodes (instances).
    - `InstanceRelation`: Represents relationships.

  For more information consult the developer `Data Model` documentation.    

## Example queries

### Example 1

This query will return every Building Node.

```text
MATCH (a)-[r]->(b)
WHERE r.Label CONTAINS 'Building'
RETURN b
```

### Example 2

This query will return every `Address` Node with the Property Country `Deutschland` or `Schweiz`.

```text
MATCH (instance:Instance)-[r]->(b)
WHERE instance.ClassificationId CONTAINS 'Address'
   AND instance.Properties.Country = 'Deutschland'
   OR  instance.ClassificationId CONTAINS 'Address'
   AND instance.Properties.Country = 'Schweiz'
RETURN instance
```