# Inventory Management API

Initial implementation based on the domain specification. It uses Clean Architecture, CQRS, EF Core for reads, Dapper for writes, SQL Server, OAuth2/JWT and Docker Compose.

## Architecture

- `Domain`: entities and invariants without infrastructure dependencies.
- `Application`: commands, queries, handlers and persistence ports.
- `Infrastructure`: EF Core read model and transactional Dapper writes.
- `Api`: HTTP endpoints, JWT authentication, Swagger and error mapping.
- `Domain.Tests`: independent unit tests for domain behavior.

The inventory balance is persisted in `Products.CurrentStock`. Inventory movements use a serializable SQL transaction plus `UPDLOCK` and `HOLDLOCK`. The database also has a `CHECK (CurrentStock >= 0)` constraint as a final safety barrier.

## Run with Docker

1. Copy `.env.example` to `.env` and configure the SQL Server and Keycloak administrator passwords plus a unique `INVENTORY_DEV_PASSWORD` of at least 16 characters. Keep `INVENTORY_DEV_USERNAME=adminInventory`.
2. Run `docker compose up --build`.
3. Open Keycloak at `http://keycloak.localhost:8081/admin` and sign in with the administrator credentials from `.env`.
4. The `keycloak-init` service creates `adminInventory` in `inventory-management` with all six API permissions. It also ensures the client roles, Swagger scope mappings and access-token mappers exist in an already imported realm.
5. Open `http://localhost:8080/swagger`, select **Authorize**, and sign in with `INVENTORY_DEV_USERNAME` and `INVENTORY_DEV_PASSWORD` from your local `.env`.

The `database-init` service creates `InventoryManagement` if absent. Next, `schema-migrate` applies embedded, versioned SQL migrations; the API starts only after migration success. The initial migration can adopt the existing application schema and seeds the default category only if no categories exist. Development startup also runs the same idempotent migrator.

Keycloak uses `http://keycloak.localhost:8081` both outside and inside Docker. Its Docker network alias and internal HTTP port deliberately match the public hostname and port, so the JWT `iss` claim matches the API authority exactly.

## Run tests

For an ordered Postman acceptance workflow covering all 13 business endpoints, import the collection and local environment from [postman](postman/README.md). The Spanish guide explains obtaining an adminInventory token through Swagger, running the 40 requests, the optional 403 case and the test data retained after logical deletion.

```bash
dotnet test
```

Domain, application and HTTP tests run without SQL Server or Keycloak. Contract tests use test authentication; authorization tests use the real JWT Bearer handler with locally signed RSA tokens and fixed test metadata. They verify permissions on every business endpoint and reject invalid signature, issuer, audience, lifetime and malformed tokens.

SQL tests connect to the existing application database named by `INVENTORY_TEST_SQL_CONNECTION`. They never create or drop databases. On Windows, run `powershell -NoProfile -File scripts/Test-Sql.ps1`; when the variable is absent, this script uses the existing local `InventoryManagement` database and reads `SQL_PASSWORD` from `.env` without printing it. A plain `dotnet test` without the variable skips SQL cases.

The fixture applies migrations, records existing row IDs, runs SQL cases serially and deletes newly added movements, products and categories in dependency order. Teardown verifies that the original ID sets remain. Run against the authorized development database with no other writers during the suite: cleanup treats all rows inserted after its baseline as test data. An interrupted process may require manual cleanup. Database, schema, migration journal and existing records are retained. Supply credentials through the environment or untracked `.env`, never source files or command history.

The SQL test account also needs permission to create test triggers and observe blocked requests in `sys.dm_exec_requests`; the local Compose SQL administrator account meets these prerequisites. Tests cover rollback after each inventory write, non-aborting SQL errors, insufficient stock, inactive/missing products, the database stock constraint and two exits confirmed to be waiting concurrently. Temporary fault triggers are removed before retrying the movement. Read-context tests verify that every `SaveChanges` overload rejects writes without requiring SQL Server.

The API accepts inventory movement types as the strings `Entry` and `Exit`. Invalid requests return `{ "code": "...", "message": "..." }`. Product prices must fit `decimal(18,2)` exactly; values requiring rounding are rejected. Missing resources return 404, inactive resources and duplicate SKUs return 409.

## Category and product lifecycle

All business endpoints require authentication. Category CRUD is available at `/api/categories` and `/api/categories/{id}`. POST returns 201 with a Location header, GET returns 200, PUT returns the updated category with 200, and DELETE returns 204. POST/PUT accept a required `name` and optional `description`.

`PUT /api/products/{id}` accepts required `name`, `sku`, `price`, `categoryId` and optional `description`, and returns the updated product with current stock. The category must exist and be active. Omitting the optional description in either PUT clears it.

Both category and product DELETE operations perform logical deletion. A category with active products returns 409 (`CATEGORY_IN_USE`). Product deactivation preserves its stock and all movement history and prevents new movements. Repeating a deletion returns 204; a nonexistent identifier returns 404. GET/list operations include inactive records. Their names and SKUs remain reserved, and PUT does not reactivate them.

## Important scope note

Category CRUD, product lifecycle and inventory queries have passed SQL Server integration tests on the existing application database. Business permission policies and versioned migrations are implemented and tested. See `docs/current-status.md` for verification evidence and the remaining live Keycloak/deployment limitations.

## Inventory queries

`GET /api/inventory/products/{productId}` returns `productId`, `sku`, `name` and `currentStock`.

`GET /api/inventory/products/{productId}/movements` returns `items`, `page`, `pageSize`, `totalCount` and `totalPages`. Each movement contains `id`, `productId`, `type`, `quantity`, `reason` and `createdAt`. Both endpoints require authentication and include inactive products. A missing product returns 404 (`PRODUCT_NOT_FOUND`); an existing product without movements returns an empty page with a zero total.

History query parameters:

| Parameter | Contract |
| --- | --- |
| `type` | Optional `Entry` or `Exit`, case insensitive; numeric types are rejected. |
| `startDate` | Optional inclusive lower timestamp bound. Use ISO 8601 with `Z` or an explicit offset. |
| `endDate` | Optional inclusive upper timestamp bound; cannot precede `startDate`. |
| `page` | Defaults to 1; allowed range 1 through 10000. |
| `pageSize` | Defaults to 20; allowed range 1 through 100. |

Example: `/api/inventory/products/{productId}/movements?type=Entry&startDate=2026-01-01T00:00:00Z&endDate=2026-01-31T23:59:59Z&page=1&pageSize=20`.

Dates filter timestamps, not whole calendar days. Results use `CreatedAt DESC, Id DESC`; the unique identifier breaks timestamp ties using SQL Server's GUID ordering. Totals reflect the filters before pagination. A page beyond the matching results is empty but retains the total. Invalid pagination, date ranges or parameter formats return 400 with the standard error envelope. For histories beyond the page limit, narrow the date filters.

Offset pagination is deterministic for unchanged data. It does not provide a snapshot across requests; new movements can shift pages. Count and item retrieval are separate read queries and can also observe concurrent changes.

## Permissions

The Compose development environment provisions one explicit full-access API user through `keycloak/bootstrap-user.py`, using the Keycloak Admin REST API. This user is not a Keycloak realm administrator and other users do not inherit its permissions. Secrets are read from environment variables and are not included in the realm JSON or logs. The initializer preserves an existing user's password; changing `.env` alone does not rotate it. Reset it explicitly through Keycloak if needed. Reapply configuration without restarting other services using `docker compose run --rm --no-deps keycloak-init`. API startup waits for initialization success. This account and local HTTP configuration are intended for development.

| Endpoints | GET | POST / PUT / DELETE |
| --- | --- | --- |
| `/api/products` and resource IDs | `products.read` | `products.write` |
| `/api/categories` and resource IDs | `categories.read` | `categories.write` |
| Inventory balance/history and movement registration | `inventory.read` | `inventory.write` (POST movements) |

Permissions are independent: write does not imply read. Missing or invalid tokens return 401 with a Bearer challenge; valid tokens without the exact required `permissions` claim return 403. Both use the standard error envelope. Health and Swagger remain public. Authority, audience and Swagger client are configured through `Authentication__Authority`, `Authentication__Audience` and `Authentication__SwaggerClientId`. HTTPS metadata is required by default; Compose disables it for local HTTP development only.

The realm import declares these six `inventory-api` client roles and an `inventory-swagger` mapper that exposes assigned roles as a multivalued `permissions` access-token claim. Assign only the required roles to each user; there is no default full-access grant. For an existing realm, update it through the administration console: create the missing client roles, and add a **User Client Role** mapper to the Swagger client's dedicated client scope, selecting client `inventory-api`, token claim `permissions`, JSON type `String`, multivalued and access-token inclusion. Keep the existing audience mapper. Obtain a new access token after role changes. The import file is an initial configuration, not an automatic update of an existing realm; do not delete the Keycloak volume to apply it.

## Versioned schema delivery

`V003__SeedSampleInventory.sql` supplies three sample categories (reusing General), three products and three initial Entry movements. Storage Box belongs to General (10 units), USB Keyboard to Electronics (20 units), and Notebook to Office Supplies (30 units). Categories are inserted before products, and products before movements, in the migration transaction. Existing unrelated data is retained, so a populated database may contain more than three rows per table. The migration journal prevents repeated seeding; conflicting sample SKUs or inactive seed categories cause rollback rather than overwriting existing data.

`V001__InitialSchema.sql` adopts/creates the baseline; `V002__QueryIndexes.sql` adds catalog and movement-query indexes. Scripts are embedded in Infrastructure and applied through Dapper. `dbo.SchemaVersions` records version, filename, SHA-256 checksum and application time. A transaction-owned application lock serializes deployments; failed migrations roll back DDL and journal entries together. Applied scripts must never be edited: add the next consecutive `Vnnn__Name.sql`. Checksum mismatches, history gaps and deployment of a release older than the database are rejected. This is forward-only delivery; automatic destructive rollback is not provided. Baseline adoption assumes the existing schema is the project's original schema, not an arbitrary manually modified database.

Set `INVENTORY_API_IMAGE` to a versioned image tag for each release. Build with `docker compose build api`, then `docker compose up -d`; the API and migration service use the same image. For a prebuilt release, pull that image and use `docker compose up -d --no-build`. Do not reuse release tags. The dependency chain is SQL health -> database bootstrap -> schema migration -> API. No volume reset is required.

Outside Docker, supply `ConnectionStrings__InventoryDatabase` and execute `dotnet InventoryManagement.Api.dll --migrate` from the published application directory. This mode exits after migration and does not require authentication configuration. A deployment account needs schema permissions; the runtime account does not need database-creation permissions. Development's automatic migration also requires schema permissions. Production deployment should invoke the migration step explicitly before starting the API.
