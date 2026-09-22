# Current Project Status

## Latest update: development account (2026-09-22)

- Created the requested adminInventory account in the existing Keycloak realm and verified all six API client-role assignments through the Admin REST API. Generated a cryptographically random password in local .env as INVENTORY_DEV_PASSWORD; credentials are not recorded here.
- Added keycloak-init to Compose. It provisions the account on development startup, ensures the roles, Swagger permission/audience mappers and scope mappings exist, and gates API startup on success. The user receives API permissions, not Keycloak administration privileges.
- Ran the initializer twice against the running Keycloak: first execution created the user; the second preserved it and verified permissions again. Existing passwords and unrelated users are preserved. No database or volume was removed.
- Compose configuration and solution build passed (zero warnings/errors). Tests: 173 passed, 33 SQL cases skipped without a configured SQL test connection. No application source or SQL schema changed in this update.
- Interactive Swagger login was not executed. Use adminInventory with INVENTORY_DEV_PASSWORD from .env; log out of the previous Swagger authorization first. The running API was not rebuilt or restarted.

## Latest update: related sample inventory

- Added and applied V003__SeedSampleInventory.sql to the existing InventoryManagement database. V001 and V002 were left unchanged to preserve their recorded checksums.
- The seed reuses General and ensures Electronics and Office Supplies exist, then creates three products and three corresponding Entry movements. Storage Box has 10 units, USB Keyboard 20 and Notebook 30, each in its respective category. Existing unrelated records are preserved; total table counts may exceed three if data already exists.
- Categories are created before products and products before movements. The migration transaction keeps initial stock and history atomic; the migration journal prevents reseeding on later runs.
- Build passed with zero warnings/errors. The full existing-database test suite passed: 206 tests, zero failures and zero skipped. Fixture cleanup preserves the seed data because migrations run before its baseline snapshot. The database was not deleted.

## Verified on 2026-09-21: implementation plan step 8

The source implementation includes explicit permissions, versioned schema deployment and completed architectural decisions. Previously pending SQL cases for steps 5, 6 and 7 passed against the existing InventoryManagement database at localhost:1433, with the user's authorization to modify its data. No temporary database was created and no database was dropped.

### Results

| Validation | Verified result |
| --- | --- |
| dotnet build InventoryManagement.sln --no-restore --nologo -m:1 -v:minimal | Passed; zero warnings and errors |
| powershell -NoProfile -File scripts/Test-Sql.ps1 | 206 passed, 0 failed, 0 skipped |
| Domain tests | 36 passed |
| Application tests | 33 passed |
| API tests | 102 passed: 82 contract and 20 real JWT cases |
| Infrastructure tests | 35 passed: 33 SQL cases and 2 read-context guards |
| docker compose config --quiet | Passed |
| dotnet publish src/InventoryManagement.Api/InventoryManagement.Api.csproj --no-restore --nologo -c Release -m:1 -v:minimal | Passed |
| Source type declaration scan | No files with multiple explicit type declarations found |
| Git status | Unavailable: workspace is not a Git repository |

The first SQL execution found five failing catalog cases: filtering or ordering after constructing CategoryDto could not be translated by EF Core. CategoryQueries now filters/orders entities before projection. The complete suite passed after correction and passed again after adding cleanup assertions.

### SQL coverage and cleanup

- All 13 inventory/product regression cases passed: duplicate SKU races, missing/inactive resources, stock constraints, overflow, concurrent exits and atomic rollback after each write. Both exits are observed waiting for locks before release.
- All 11 catalog cases passed: CRUD, duplicate names, active-product deletion guards, inactive records, preserved stock/history and category deletion racing with product creation/reassignment.
- All five inventory query cases passed: inactive products, missing versus empty history, timestamp ties, deterministic SQL ordering, inclusive combined filters and page boundaries.
- Four migration cases passed: repeated/concurrent deployment, changed-checksum rejection, older-release rejection and DDL/journal rollback after an injected SQL failure.
- The shared fixture runs serially, creates its own category and records, and deletes newly added movements, products and categories after the suite. Teardown assertions verified preservation of the original row-ID sets. Product-scoped fault triggers are removed by their tests; the failed-migration probe table was verified absent.
- The database and schema remain. SchemaVersions retains V001 and V002, and migration indexes remain as intended. The fixture never creates/drops databases and rejects system databases.
- Cleanup assumes no external writers: new rows are identified against the initial ID snapshot. Interrupted execution may require manual cleanup. This development-data workflow was explicitly authorized.

### Authorization and delivery

- All 13 business endpoints require the corresponding products.read/write, categories.read/write or inventory.read/write permission. Write does not imply read.
- Tests use the production JWT Bearer handler with locally signed RSA tokens and fixed metadata. Every route is checked without a token (401), without permission (403), with an unrelated permission (403), and with the required permission (authorization succeeds). Invalid issuer, audience, signature, expiration, future validity, missing expiration and malformed tokens return 401. Error envelopes and Bearer challenges are verified.
- The Keycloak import declares six client roles and a multivalued permissions access-token mapper. README explains role assignment and updating existing realms without deleting their volume.
- Embedded SQL migrations use Dapper writes, EF journal reads, SHA-256 checksums, a transaction-owned application lock and atomic DDL/journal updates. The --migrate entry point does not start the API. Compose gates API startup on schema-migrate, using the same configurable release image.
- ADR-011 through ADR-014 document permissions, schema delivery, provider selection and focused persistence ports. Earlier verification notes were corrected.
- Messaging interfaces, inventory command/handler and Program's test-access declaration were split into individual files. Domain and Application remain independent of authentication and persistence libraries.
- README, AGENTS and environment examples describe delivery. Docker/Git ignore files exclude local secrets and build output.

## Functional implementation

- Category CRUD with logical deletion, uniqueness and active-product guards.
- Product creation, retrieval, listing, update and deactivation; active-category validation under transaction locks.
- Immutable inventory entry/exit registration, stock validation, shared EF/Dapper transactions and SQL constraints.
- Balance and history queries with type/date filters, inclusive bounds, CreatedAt DESC / Id DESC ordering and bounded pagination.
- Required fields, string movement types, consistent errors, domain length/price/stock limits and distinct 400/401/403/404/409 responses.
- Swagger OAuth2 Authorization Code with PKCE configuration and Compose services for API, SQL Server, Keycloak, database bootstrap and schema migration.

## Remaining verification boundaries

- The new Docker image was not built or deployed during this step. Release publication and Compose validation passed; container startup of this release has not been certified.
- Existing Keycloak roles/mappers were not changed live, and interactive Swagger login was not repeated with the new policies. Local JWT tests verify API validation/authorization, not live metadata discovery or browser login. Apply the documented realm configuration and assign permissions before using the new API release.
- Earlier status reports recorded successful Docker startup and interactive PKCE login for the earlier deployment; those observations do not certify this release.
- History count/page retrieval does not share a snapshot; concurrent inserts may shift pages. Baseline adoption assumes the known original schema and is not a general drift validator. Migrations are forward-only.

## Previous checkpoints

- Step 7: 153 passed with 29 SQL cases skipped; that SQL gap is now closed.
- Steps 5 and 6: 117 passed with 24 SQL cases skipped; catalog/lifecycle SQL verification now passes.
- Steps 1 through 4: previously reported 88 passed, including 13 SQL cases in a disposable database. The current fixture replaces that workflow and preserves the existing database.

Do not include credentials, tokens or .env values in this document.
