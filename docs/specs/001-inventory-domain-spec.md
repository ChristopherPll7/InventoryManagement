# Inventory Management API — Domain Specification

## 1. Document Purpose

This document defines the expected domain behavior, business rules, constraints, use cases, and API-level contracts for the Inventory Management API before implementation begins.

The purpose of this specification is to:

- Define system behavior before implementation code is generated or written.
- Establish clear domain rules and invariants.
- Provide a shared contract for implementation and testing.
- Support Test-Driven Development by defining expected behaviors in advance.
- Reduce ambiguity during AI-assisted development.
- Serve as evidence of Spec-Driven Development.

This document intentionally avoids implementation details except where required by the technical constraints of the assessment.

---

## 2. System Objective

The system must provide a RESTful API for managing:

- Products.
- Product categories.
- Inventory movements.

Authenticated clients must be able to:

- Create, retrieve, update, and delete products.
- Create, retrieve, update, and delete categories.
- Register inventory entries.
- Register inventory exits.
- Retrieve the current inventory for a product.
- Retrieve the inventory movement history for a product.

The system must prevent inventory inconsistencies such as negative stock.

---

## 3. Domain Scope

The first version of the system contains the following primary domain concepts:

- Product.
- Category.
- Inventory Movement.
- Inventory Stock.

The following concepts are application or infrastructure concerns and remain outside the core inventory domain:

- Authentication provider.
- OAuth2 implementation.
- Database connection.
- Logging provider.
- Swagger configuration.
- Docker configuration.
- SQL Server configuration.

---

## 4. Domain Model

### 4.1 Product

A Product represents an item that may exist in inventory.

#### Attributes

| Attribute | Type | Required | Description |
|---|---|---:|---|
| Id | Guid | Yes | Unique identifier |
| Name | String | Yes | Product name |
| Description | String | No | Product description |
| Sku | String | Yes | Unique inventory code |
| Price | Decimal | Yes | Product unit price |
| CategoryId | Guid | Yes | Category identifier |
| IsActive | Boolean | Yes | Indicates whether the product is active |
| CreatedAt | DateTime | Yes | Creation date and time |
| UpdatedAt | DateTime | No | Last modification date and time |

#### Product Rules

- A product must have a name.
- A product name cannot contain only whitespace.
- The SKU must be unique.
- The SKU cannot be empty.
- The price cannot be negative.
- A product must belong to an existing category.
- A product must be active by default when created.
- Inactive products cannot receive new inventory movements.
- A product identifier cannot be modified after creation.

---

## 5. Category

A Category groups products with similar characteristics.

### Attributes

| Attribute | Type | Required | Description |
|---|---|---:|---|
| Id | Guid | Yes | Unique identifier |
| Name | String | Yes | Category name |
| Description | String | No | Category description |
| IsActive | Boolean | Yes | Indicates whether the category is active |
| CreatedAt | DateTime | Yes | Creation date and time |
| UpdatedAt | DateTime | No | Last modification date and time |

### Category Rules

- A category must have a name.
- A category name cannot contain only whitespace.
- Category names must be unique.
- A product cannot reference a nonexistent category.
- A category containing active products cannot be deleted.
- A category may be marked as inactive.
- Inactive categories cannot be assigned to new products.

---

## 6. Inventory Movement

An Inventory Movement represents a change in the stock level of a product.

The system supports two movement types:

- Entry.
- Exit.

### Attributes

| Attribute | Type | Required | Description |
|---|---|---:|---|
| Id | Guid | Yes | Unique movement identifier |
| ProductId | Guid | Yes | Product affected by the movement |
| Type | InventoryMovementType | Yes | Entry or Exit |
| Quantity | Int | Yes | Number of units |
| Reason | String | No | Optional business reason |
| CreatedAt | DateTime | Yes | Movement date and time |

---

## 7. Inventory Movement Types

### Entry

An Entry increases the available stock of a product.

```text
Current inventory: 20
Movement: Entry 5
Result: 25
```

### Exit

An Exit decreases the available stock of a product.

```text
Current inventory: 20
Movement: Exit 5
Result: 15
```

---

## 8. Inventory Rules and Invariants

- Inventory movements are immutable after creation.
- An inventory movement cannot be updated.
- An inventory movement cannot be deleted through the public API.
- A movement quantity must always be greater than zero.
- The referenced product must exist.
- The referenced product must be active.
- An Entry increases product stock.
- An Exit decreases product stock.
- An Exit cannot reduce inventory below zero.
- The current stock of a product can never be negative.
- The system must validate stock availability before registering an Exit.
- Inventory modification and movement registration must behave atomically.
- If any operation fails while registering a movement, no partial inventory modification may be persisted.

---

## 9. Inventory Stock

The system must expose the current stock quantity for each product.

Inventory represents the current number of available units.

Inventory may be explicitly persisted or calculated from inventory movements, depending on the final architecture decision.

Regardless of implementation strategy, the following invariant must hold:

```text
CurrentStock >= 0
```

The conceptual inventory calculation is:

```text
CurrentStock =
SUM(Inventory Entries)
-
SUM(Inventory Exits)
```

---

## 10. UC-001 Create Category

### Actor

Authenticated API client.

### Preconditions

The client is authenticated.

### Input

- Name.
- Description.

### Expected Behavior

- The system validates the category.
- The system verifies that another category with the same name does not exist.
- The system creates the category.

### Successful Result

```text
HTTP 201 Created
```

### Error Conditions

- `400 Bad Request` when the request contains invalid data.
- `409 Conflict` when the category name already exists.
- `401 Unauthorized` when authentication is missing or invalid.

---

## 11. UC-002 Get Category

### Actor

Authenticated API client.

### Input

Category identifier.

### Expected Behavior

The system retrieves the requested category.

### Successful Result

```text
HTTP 200 OK
```

### Error Conditions

- `404 Not Found` when the category does not exist.
- `401 Unauthorized` when authentication is missing or invalid.

---

## 12. UC-003 Update Category

### Preconditions

The category exists.

### Expected Behavior

- The system validates the requested changes.
- The system verifies category name uniqueness.
- The system updates the category.

### Successful Result

```text
HTTP 200 OK
```

### Error Conditions

- `400 Bad Request` for invalid data.
- `404 Not Found` when the category does not exist.
- `409 Conflict` when the new category name already exists.

---

## 13. UC-004 Delete Category

### Preconditions

The category exists.

### Expected Behavior

The system verifies whether the category contains associated active products.

### Successful Result

```text
HTTP 204 No Content
```

### Error Conditions

- `404 Not Found` when the category does not exist.
- `409 Conflict` when the category contains active products.

---

## 14. UC-005 Create Product

### Actor

Authenticated API client.

### Input

- Name.
- Description.
- SKU.
- Price.
- CategoryId.

### Preconditions

- The referenced category exists.
- The referenced category is active.

### Expected Behavior

- The system validates the product.
- The system validates SKU uniqueness.
- The system validates the referenced category.
- The product is created with initial inventory equal to zero.

### Successful Result

```text
HTTP 201 Created
```

### Error Conditions

- `400 Bad Request` for invalid data.
- `404 Not Found` when the category does not exist.
- `409 Conflict` when the SKU already exists.

---

## 15. UC-006 Get Product

### Input

Product identifier.

### Expected Behavior

- The system retrieves the product.
- The response must contain the current inventory quantity.

### Successful Result

```text
HTTP 200 OK
```

### Error Conditions

- `404 Not Found` when the product does not exist.

---

## 16. UC-007 Update Product

### Preconditions

The product exists.

### Expected Behavior

- The system validates the updated information.
- SKU changes are permitted only when the new SKU is not assigned to another product.
- The referenced category must exist and be active.

### Successful Result

```text
HTTP 200 OK
```

### Error Conditions

- `400 Bad Request` for invalid data.
- `404 Not Found` when the product or category does not exist.
- `409 Conflict` when the SKU already exists.

---

## 17. UC-008 Delete Product

### Preconditions

The product exists.

### Expected Behavior

- The product must not be physically deleted when inventory movement history exists.
- The preferred domain behavior is logical deletion by marking the product as inactive.
- Historical inventory movements must remain available.

### Successful Result

```text
HTTP 204 No Content
```

### Error Conditions

- `404 Not Found` when the product does not exist.

---

## 18. UC-009 Register Inventory Entry

### Input

- ProductId.
- Quantity.
- Reason.

### Preconditions

- The product exists.
- The product is active.
- Quantity is greater than zero.

### Expected Behavior

- The system creates an Entry inventory movement.
- Current stock increases by the specified quantity.

### Example

```text
Current inventory: 10
Command: Entry 5
Expected inventory: 15
```

### Successful Result

```text
HTTP 201 Created
```

### Error Conditions

- `400 Bad Request` for an invalid quantity.
- `404 Not Found` when the product does not exist.
- `409 Conflict` when the product is inactive.

---

## 19. UC-010 Register Inventory Exit

### Input

- ProductId.
- Quantity.
- Reason.

### Preconditions

- The product exists.
- The product is active.
- Quantity is greater than zero.
- Available inventory is greater than or equal to the requested quantity.

### Expected Behavior

- The system creates an Exit inventory movement.
- Current stock decreases by the specified quantity.

### Example

```text
Current inventory: 10
Command: Exit 4
Expected inventory: 6
```

### Insufficient Inventory Example

```text
Current inventory: 3
Command: Exit 5
Expected behavior: The movement is rejected.
Inventory remains: 3
No inventory movement is persisted.
```

### Successful Result

```text
HTTP 201 Created
```

### Error Conditions

- `400 Bad Request` for an invalid quantity.
- `404 Not Found` when the product does not exist.
- `409 Conflict` when inventory is insufficient.
- `409 Conflict` when the product is inactive.

---

## 20. UC-011 Get Product Inventory

### Input

Product identifier.

### Expected Behavior

The system returns:

- Product identifier.
- SKU.
- Product name.
- Current inventory.

### Successful Result

```text
HTTP 200 OK
```

### Error Conditions

- `404 Not Found` when the product does not exist.

---

## 21. UC-012 Get Inventory Movements

### Input

Product identifier.

Optional filters:

- Movement type.
- Start date.
- End date.
- Page.
- Page size.

### Expected Behavior

- The system returns the product movement history.
- Results are ordered from newest to oldest by default.
- Pagination must be supported.

---

## 22. Proposed REST Contract

### Products

```text
POST /api/products
GET /api/products
GET /api/products/{id}
PUT /api/products/{id}
DELETE /api/products/{id}
```

### Categories

```text
POST /api/categories
GET /api/categories
GET /api/categories/{id}
PUT /api/categories/{id}
DELETE /api/categories/{id}
```

### Inventory

```text
POST /api/inventory/movements
GET /api/inventory/products/{productId}
GET /api/inventory/products/{productId}/movements
```

---

## 23. Inventory Movement Request Contract

Entry example:

```json
{
  "productId": "fa846eb4-22dd-42a4-a43a-d8a24e532db5",
  "type": "Entry",
  "quantity": 10,
  "reason": "Initial inventory"
}
```

Exit example:

```json
{
  "productId": "fa846eb4-22dd-42a4-a43a-d8a24e532db5",
  "type": "Exit",
  "quantity": 3,
  "reason": "Customer order"
}
```

---

## 24. Expected Error Contract

API errors must use a consistent structure.

```json
{
  "code": "INSUFFICIENT_STOCK",
  "message": "The requested quantity exceeds the available stock."
}
```

Suggested domain error codes:

```text
PRODUCT_NOT_FOUND
CATEGORY_NOT_FOUND
DUPLICATE_PRODUCT_SKU
DUPLICATE_CATEGORY_NAME
INVALID_PRODUCT_PRICE
INVALID_INVENTORY_QUANTITY
INSUFFICIENT_STOCK
PRODUCT_INACTIVE
CATEGORY_IN_USE
CATEGORY_INACTIVE
```

---

## 25. Authentication Specification

- All business endpoints must require authentication.
- OAuth2 must be used as the authentication mechanism.
- The API acts as a protected resource.
- Unauthenticated requests must return `401 Unauthorized`.
- Authenticated clients without the required permission must return `403 Forbidden`.
- Authentication configuration must be obtained from environment variables.
- Authentication infrastructure must not contain domain business logic.

---

## 26. CQRS Specification

The system must separate operations that modify state from operations that retrieve state.

### Commands

Commands represent operations that modify system state.

```text
CreateProductCommand
UpdateProductCommand
DeleteProductCommand

CreateCategoryCommand
UpdateCategoryCommand
DeleteCategoryCommand

RegisterInventoryMovementCommand
```

Commands must use Dapper for database write operations.

### Queries

Queries retrieve data without modifying state.

```text
GetProductByIdQuery
GetProductsQuery

GetCategoryByIdQuery
GetCategoriesQuery

GetProductInventoryQuery
GetProductInventoryMovementsQuery
```

Queries must use Entity Framework Core.

---

## 27. Data Access Constraints

### Read Operations

All read operations must use Entity Framework Core.

Examples:

```text
GetProduct
GetProducts
GetCategory
GetCategories
GetInventory
GetInventoryMovements
```

### Write Operations

All write operations must use Dapper.

Examples:

```text
CreateProduct
UpdateProduct
DeleteProduct
CreateCategory
UpdateCategory
DeleteCategory
RegisterInventoryMovement
```

The Application and Domain layers must not depend directly on Entity Framework Core or Dapper. Persistence details must remain infrastructure concerns.

---

## 28. Transaction Requirements

`RegisterInventoryMovement` requires transactional consistency.

The following steps must either all succeed or all fail:

```text
Validate product
Validate current inventory
Create inventory movement
Update inventory balance
```

If any step fails, the transaction must be rolled back. This requirement is especially important for inventory exits.

---

## 29. Concurrency Requirement

The system must prevent concurrent inventory exits from producing negative stock.

Example:

```text
Initial stock: 5
Request A: Exit 4
Request B: Exit 4
```

Both requests must not succeed. Final inventory must never become `-3`.

The implementation must include a database-level concurrency strategy. The specific mechanism is defined during architecture design.

---

## 30. Validation Rules

### Product

```text
Name: required
Sku: required and unique
Price: >= 0
CategoryId: required and must exist
```

### Category

```text
Name: required and unique
```

### Inventory Movement

```text
ProductId: required
Type: Entry or Exit
Quantity: > 0
```

---

## 31. Non-Functional Requirements

### Maintainability

The system must follow:

- SOLID principles.
- Clean Code.
- Object-Oriented Programming principles.
- Clear separation of responsibilities.

### Testability

Business rules must be testable independently from:

- SQL Server.
- Docker.
- Authentication provider.
- HTTP infrastructure.

### Extensibility

The architecture should allow future inventory capabilities such as:

- Warehouses.
- Multiple inventory locations.
- Suppliers.
- Purchase orders.
- Stock reservations.

These capabilities are outside the current implementation scope.

---

## 32. Clean Code Constraints

- All source code and code comments must be written in English.
- Spanish may appear in API responses only when explicitly required.
- Methods and functions must use descriptive names.
- Methods should have a single responsibility.
- Methods longer than approximately 25 lines should be reviewed for responsibility violations.
- Nested loops and conditionals should be minimized.
- Configuration values must come from environment variables.
- Each class or enum must exist in its own file.
- Commented-out code is prohibited.
- Domain objects should be passed as parameters when appropriate instead of multiple unrelated primitive values.
- Blank lines inside implementation code must follow the technical challenge rules.

---

## 33. Unit Testing Specification

Tests must validate domain behavior rather than infrastructure implementation details.

### Product

```text
Creating a product with a valid category succeeds.
Creating a product with a negative price fails.
Creating a product with a duplicate SKU fails.
Creating a product with a nonexistent category fails.
```

### Category

```text
Creating a category with a valid name succeeds.
Creating a duplicate category fails.
Deleting a category with active products fails.
```

### Inventory

```text
An inventory entry increases stock.
An inventory exit decreases stock.
An inventory exit with insufficient stock fails.
An inventory movement with quantity zero fails.
An inventory movement with negative quantity fails.
An inventory movement for a nonexistent product fails.
An inventory movement for an inactive product fails.
```

---

## 34. TDD Workflow

Critical domain functionality should follow:

```text
Red
↓
Write a failing test describing expected behavior.

Green
↓
Implement the minimum required code to satisfy the test.

Refactor
↓
Improve the implementation without changing behavior.
```

Inventory movements should be implemented first with this approach because they contain the most important domain invariants.

---

## 35. Initial Acceptance Criteria

The solution is functionally complete when:

- Products can be created.
- Products can be retrieved.
- Products can be updated.
- Products can be deleted or deactivated.
- Categories can be created.
- Categories can be retrieved.
- Categories can be updated.
- Categories can be deleted when permitted by domain rules.
- Inventory entries can be registered.
- Inventory exits can be registered.
- Negative inventory cannot occur.
- Inventory history can be retrieved.
- All protected endpoints require OAuth2 authentication.
- Reads are implemented using Entity Framework Core.
- Writes are implemented using Dapper.
- Critical domain behavior is covered by unit tests.
- The API can run using Docker Compose.
- SQL Server runs as a Docker service.
- Swagger documents the API endpoints.

---

## 36. Out of Scope

The first version does not include:

- User administration.
- Customer management.
- Supplier management.
- Orders.
- Purchase orders.
- Multiple warehouses.
- Product images.
- Notifications.
- Inventory reservations.
- Batch or serial number tracking.
- Multi-currency support.

These capabilities may be incorporated in future versions without altering the initial domain requirements.

---

## 37. Architecture Constraints Derived from the Specification

```text
Domain logic must remain independent from infrastructure.

Queries and commands must be separated.

Entity Framework Core must only be used for reads.

Dapper must only be used for writes.

Inventory changes must be transactional.

The domain must prevent negative stock.

Application logic must be independently unit testable.

OAuth2 authentication must remain outside the Domain layer.

Database configuration must use environment variables.
```

---

## 38. Decisions Requiring Architecture Definition

The following decisions are intentionally not resolved by this domain specification and must be documented during architecture design:

1. Whether current inventory is persisted or calculated from movements.
2. How inventory concurrency will be controlled.
3. Whether logical deletion or physical deletion will be used for categories.
4. Which OAuth2 authorization server will be used.
5. Whether CQRS will use MediatR or a custom command/query dispatcher.
6. How database migrations will be executed.
7. Whether repositories will be introduced or persistence abstractions will be implemented directly through command and query handlers.

Each decision must document:

```text
Problem
Options considered
AI recommendation
Decision made
Reason
Trade-offs
```

---

## 39. Definition of Done

A feature is complete when:

- Its expected behavior is defined.
- Its tests describe the expected behavior.
- Tests pass.
- Domain rules are satisfied.
- The implementation respects CQRS boundaries.
- Reads use Entity Framework Core.
- Writes use Dapper.
- No configuration value is hardcoded.
- The implementation follows the defined Clean Code constraints.
- Swagger exposes the API contract when applicable.
- Relevant architecture decisions are documented.
