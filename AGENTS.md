# Inventory Management API — Codex Instructions

## Project objective

This repository implements an Inventory Management REST API as part of a technical assessment.

The system manages:

- Products.
- Categories.
- Inventory entries and exits.
- Current product stock.
- Inventory movement history.

Read these files before proposing changes:

- `README.md`
- `docs/specs/001-inventory-domain-spec.md`
- `docs/architecture-decisions.md`

## Technology stack

- .NET 8
- ASP.NET Core Web API
- SQL Server 2022
- Entity Framework Core for read operations
- Dapper for write operations
- OAuth2 and OpenID Connect through Keycloak
- Swagger/OpenAPI
- xUnit
- Docker Compose

## Architecture

The solution follows Clean Architecture:

- `InventoryManagement.Domain`
  - Entities, value objects, domain rules and invariants.
  - Must not depend on Application, Infrastructure or API.

- `InventoryManagement.Application`
  - Commands, queries, handlers and persistence abstractions.
  - Must not depend directly on Entity Framework Core or Dapper.

- `InventoryManagement.Infrastructure`
  - Entity Framework Core read operations.
  - Dapper write operations.
  - SQL Server persistence.

- `InventoryManagement.Api`
  - Controllers, authentication, Swagger and middleware.
  - Must not contain domain business rules.

- `InventoryManagement.Domain.Tests`
  - Unit tests for domain behavior.

## CQRS constraints

Commands modify state and must use Dapper.

Queries retrieve information and must use Entity Framework Core with
`AsNoTracking()` when appropriate.

Do not use Entity Framework Core for writes.

Do not use Dapper for reads.

## Inventory rules

- Movement quantities must be greater than zero.
- Inventory movements are immutable.
- An entry increases stock.
- An exit decreases stock.
- Current stock must never become negative.
- Inactive products cannot receive inventory movements.
- Movement registration and stock modification must be atomic.
- Concurrent exits must not produce negative stock.
- Inventory writes use a serializable SQL transaction with `UPDLOCK` and
  `HOLDLOCK`.
- SQL Server includes a `CHECK (CurrentStock >= 0)` constraint.

## Authentication

The API uses OAuth2 Authorization Code Flow with PKCE through Keycloak.

Development authority:

`http://keycloak.localhost:8081/realms/inventory-management`

Expected audience:

`inventory-api`

Swagger client:

`inventory-swagger`

The API must validate token signature, issuer, audience and expiration.

Do not disable issuer or audience validation to solve authentication errors.

Do not store passwords, tokens or secrets in source-controlled files.

## Docker environment

Docker Compose runs:

- `api`
- `sqlserver`
- `database-init`
- `schema-migrate`
- `keycloak`
- `keycloak-init`

`database-init` creates the `InventoryManagement` database before the API
starts.

The versioned `schema-migrate` service must succeed before the API starts.
The API also applies the same idempotent migrations in Development.

Do not delete Docker volumes unless the user explicitly requests a clean reset.

## Clean Code requirements

- All source code and code comments must be written in English.
- Use descriptive names.
- Keep methods focused on one responsibility.
- Avoid unnecessary abstractions and dependencies.
- Each class or enum should have its own file.
- Do not leave commented-out code.
- Do not hardcode configuration values or credentials.
- Preserve dependency direction between layers.
- Domain behavior must be independently unit testable.
- Treat compiler warnings as errors.

## Expected error contract

API domain errors use:

```json
{
  "code": "ERROR_CODE",
  "message": "Human-readable message."
}
```

Use the appropriate HTTP status:
- 400 for invalid input.
- 401 for missing or invalid authentication.
- 403 for insufficient permissions.
- 404 for nonexistent resources.
- 409 for domain conflicts.
Working rules
Before modifying code:
1. Read the relevant specification and architecture decision.
2. Inspect the existing implementation.
3. Explain any important architectural decision.
4. Do not modify unrelated files.
After modifying code:
1. Run dotnet build.
2. Run dotnet test.
3. Run docker compose config when Compose changes.
4. Report what changed and which validations passed.
5. State clearly if a validation could not be executed.
Do not claim that a test or build passed unless it was actually executed.
