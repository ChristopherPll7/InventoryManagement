# Architecture Decisions

## ADR-001: Persist the current inventory balance

- Problem: calculate stock from all movements or persist the current balance.
- Options: aggregate immutable movements on every read; store a balance and movement history.
- AI recommendation: store the balance for efficient reads and update it atomically with the movement.
- Decision: persist `CurrentStock` in `Products`.
- Reason: product reads are frequent and concurrency control is simpler around one locked row.
- Trade-offs: duplicated derived data requires strict transactional consistency.

## ADR-002: Prevent concurrent negative stock

- Problem: two exits can read the same balance and both succeed.
- Options: optimistic versioning; serializable transaction with row locks; stored procedure.
- AI recommendation: a serializable SQL transaction with `UPDLOCK, HOLDLOCK`, plus a database check constraint.
- Decision: use the recommended locking strategy with an EF Core locked read and Dapper writes sharing one connection and transaction, as specified in ADR-006.
- Reason: it is explicit, testable with SQL Server, and keeps both writes atomic.
- Trade-offs: concurrent movements for the same product are serialized and may wait.

## ADR-003: CQRS dispatch

- Problem: choose MediatR or a custom abstraction.
- Options: MediatR; small command/query handler interfaces.
- AI recommendation: use explicit interfaces for this challenge.
- Decision: custom `ICommandHandler` and `IQueryHandler` interfaces.
- Reason: avoids an unnecessary dependency while keeping CQRS boundaries visible.
- Trade-offs: cross-cutting pipelines must be implemented separately if later needed.

## ADR-004: Product deletion

- Problem: retain inventory history when a product is removed.
- Options: physical deletion; logical deletion.
- AI recommendation: logical deletion.
- Decision: deactivate products through `IsActive`.
- Reason: historical movements keep a valid product reference.
- Trade-offs: every relevant query must deliberately include or exclude inactive products.

## ADR-005: Local OAuth2 provider hostname

- Problem: Keycloak tokens can contain a host-facing issuer while the API retrieves metadata through a different Docker-only address.
- Options: disable issuer validation; configure separate metadata and issuer values; expose one resolvable hostname and port everywhere.
- AI recommendation: use one hostname and port without weakening token validation.
- Decision: Keycloak publishes `http://keycloak.localhost:8081`, listens internally on port `8081`, and has the `keycloak.localhost` Docker network alias.
- Reason: the browser, Swagger, Keycloak and API all use the same authority, so the JWT `iss` value remains consistent.
- Trade-offs: the development setup relies on the standard `.localhost` loopback hostname behavior.

## ADR-006: Read validation state with EF Core inside the inventory transaction

- Problem: distinguishing inactive products from missing products requires reading both status and stock without weakening concurrency protection; the specification forbids Dapper reads.
- Options: read status outside the transaction; retain Dapper reads as an exception; enlist an EF Core read context in the existing SQL transaction.
- AI recommendation: enlist EF Core in the same connection and transaction used by Dapper writes.
- Decision: the inventory store owns a serializable SQL transaction. A temporary EF Core context uses that connection and transaction to read the product with `UPDLOCK, HOLDLOCK`, including inactive products. The domain rejects inactive products before any write. Dapper inserts the movement and updates the balance. Product creation validation also uses EF Core through a separate application read port.
- Reason: preserves atomicity and locking while complying with the read/write technology constraints. This clarifies ADR-002: Dapper performs the writes, not the locked read.
- Trade-offs: infrastructure coordinates two persistence libraries; SQL Server integration tests must verify the shared transaction and concurrent exits.

Implementation safeguards and verification:

- All business reads, including category/SKU validation and locked inventory state, use EF Core with `AsNoTracking()`. Dapper executes the business writes. Domain and Application have no EF Core or Dapper dependencies.
- `InventoryReadDbContext` rejects every synchronous and asynchronous `SaveChanges` overload. This is a guard against accidental tracked writes; EF bulk/raw write APIs must still be excluded during code review.
- Database initialization executes DDL and seed mutations with Dapper. Existence predicates inside those idempotent mutation batches do not return application query results.
- SQL Server tests inject failures after the movement insert and after the stock update. Both server-aborting errors and a non-aborting SQL error leave stock, history and the product modification timestamp unchanged. The same movement can be retried after removing the fault.
- The concurrency test holds an external product lock, observes both exit requests waiting for locks, then releases it. From stock 5, exactly one Exit 4 succeeds; the other returns `INSUFFICIENT_STOCK`, with final stock 1 and only one persisted exit.
- With the user's explicit authorization, SQL tests now run in the existing development database. Product-scoped fault triggers are removed after each test. Teardown removes inserted test rows and verifies preservation of the original ID sets; it never drops the database. The suite must run without concurrent external writers.

## ADR-007: Validate the public contract before persistence

- Problem: invalid enums, values outside SQL column limits, duplicate write races and framework-generated errors previously caused inconsistent responses or generic server errors.
- Options: rely on SQL and framework defaults; silently truncate or round values; reject invalid inputs explicitly and translate known persistence conflicts.
- AI recommendation: explicit validation and one error envelope.
- Decision: HTTP requests require the specified properties and string movement types (`Entry`, `Exit`); numeric movement types are rejected. The domain validates normalized text lengths against the existing schema, a price range of 0 through 9999999999999999.99 with at most two decimal places, and stock within the nonnegative Int32 range. Prices are never silently rounded. Known SKU constraint violations become `DUPLICATE_PRODUCT_SKU`; inactive entities and stock overflow return 409. Empty identifiers return 400, whereas nonexistent identifiers return 404. Binding errors, authentication challenges and other HTTP failures use `{code,message}`.
- Reason: predictable client behavior without leaking SQL errors or changing persisted values silently.
- Trade-offs: clients sending numeric movement types or prices requiring rounding must correct their requests. Boundary limits must remain synchronized with future schema changes. Business permission policies remain a separate feature.

## ADR-008: Category deletion and inactive catalog records

- Problem: category deletion must reject active products while preserving references from inactive products and their inventory history.
- Options: physical deletion when no references remain; mixed physical/logical deletion; consistent logical deletion.
- AI recommendation: logical deletion for categories and products.
- Decision: category DELETE sets `IsActive` to false and returns 204, but returns `CATEGORY_IN_USE` (409) if active products reference it. Product DELETE also deactivates the record. Repeated deletion returns 204 without changing the original deactivation timestamp. Missing records return 404. Reads and lists include inactive records and expose their status. Names and SKUs remain reserved by the existing unique constraints.
- Reason: preserves relationships and history with predictable behavior independent of whether a record already has inventory movements.
- Trade-offs: DELETE does not remove a record from GET/list results. PUT may edit inactive records but cannot reactivate them; product updates still require an active target category. Reactivation and active-only filtering are not part of this change.

## ADR-009: Transactional catalog lifecycle operations

- Problem: an application-level active-category check can become stale before a product is inserted or reassigned. Stale updates must also not reactivate deleted records or overwrite stock.
- Options: prechecks alone; database triggers; serializable transactions with locked EF Core reads and Dapper writes.
- AI recommendation: reuse the shared-transaction strategy for catalog operations.
- Decision: product create/update locks and revalidates the target category inside the write transaction. Category deletion locks that category before checking active products. Product update/deactivation locks the product row, serializing with inventory operations. Updates write only editable details and modification dates; they never replace current stock, creation dates or activation state from stale application objects. Category updates likewise preserve activation state. SQL unique-constraint conflicts are translated to the existing duplicate-name/SKU domain errors.
- Reason: enforces the category rule at the persistence boundary while retaining independently testable domain rules and EF-read/Dapper-write separation.
- Trade-offs: mutations assigning products to the same category serialize. Integration cases covering category deletion racing with creation/reassignment have passed against the existing SQL Server database; see current-status.md.

## ADR-010: Bounded inventory history queries

- Problem: inventory history needs composable filters, a deterministic newest-first order and bounded result sizes without losing access to inactive products' history.
- Options: unbounded lists; offset pagination with a unique tie-breaker; cursor pagination with a snapshot boundary.
- AI recommendation: bounded offset pagination to match the specified page/page-size contract.
- Decision: expose separate authenticated balance and history queries through Application handlers and an EF Core read port. All reads use `AsNoTracking()` and SQL-side projection/filtering. History filters combine product ID, optional movement type and inclusive DateTimeOffset bounds. Order by `CreatedAt DESC, Id DESC`, then apply Skip/Take. Return filtered total count and page metadata. Default page size is 20, maximum size 100, and maximum page 10000; these are API contract limits validated in Application before persistence access. Invalid filters return 400; missing products return 404; existing products with no matches return empty pages. Inactive products remain readable.
- Reason: matches UC-011/UC-012, prevents unbounded response materialization, avoids offset overflow and resolves equal-timestamp ordering.
- Trade-offs: SQL Server GUID ordering defines the tie-breaker, not chronological GUID order. Counting large histories still has a cost. Offset pagination can shift when new movements arrive, and the count/items queries do not share a snapshot. Cursor pagination or snapshot isolation may be considered if a future contract requires stable browsing under concurrent writes.

## ADR-011: Explicit business permissions

- Problem: authentication alone cannot distinguish readers from clients allowed to modify inventory.
- Options: a single administrator role; six independent resource/action permissions; an external authorization engine.
- AI recommendation: read/write policies for products, categories and inventory.
- Decision: require the exact `permissions` claim on every business endpoint. Keycloak client roles populate a multivalued access-token claim. Writes do not imply reads. Validate signature, issuer, audience and lifetime; return 401 for invalid authentication and 403 for insufficient permission.
- Reason: clear least-privilege behavior without domain dependencies on the identity provider. Twenty JWT tests exercise all 13 routes plus seven invalid-token conditions using real Bearer validation and local signing keys.
- Trade-offs: administrators must assign roles and update existing realm mappers. These automated tests do not certify interactive Keycloak login or live metadata discovery.

## ADR-012: Forward-only versioned SQL deployment

- Problem: Development-only table initialization cannot reliably evolve or deploy the schema.
- Options: EF migrations; a third-party migration runner; embedded ordered SQL scripts with a journal.
- AI recommendation: embedded SQL migrations, retaining the Dapper-write constraint.
- Decision: apply consecutive scripts through Dapper under one transaction and an exclusive application lock. Read the journal with EF Core; record filename, version, checksum and timestamp. Reject edited, missing or newer applied versions. Expose `--migrate` and a Compose migration service that gates API startup using the same release image.
- Reason: reproducible delivery with atomic failure, concurrent-deployment protection and compatibility with the existing database. SQL tests verify repeat/concurrent deployment, checksum rejection, older-release rejection and DDL/journal rollback.
- Trade-offs: migrations are forward-only, require schema permissions and may hold locks. V001 adopts the known original schema using existence checks; it is not a full schema-drift detector. Recovery from an incompatible release requires an explicit corrective migration or an operational restore strategy.

## ADR-013: Keycloak as the authorization server

- Problem: provide OAuth2/OIDC login without implementing identity management inside the inventory domain.
- Options: Keycloak; a hosted identity provider; a custom token issuer.
- AI recommendation: Keycloak for the assessment's self-contained Docker environment.
- Decision: Keycloak issues access tokens through Authorization Code with PKCE for the public Swagger client; ASP.NET Core validates them as a protected resource.
- Reason: supports the required protocol, local reproducibility and centrally managed client roles without custom password handling in the API.
- Trade-offs: the realm, users and roles require administration. The local HTTP/start-dev configuration is for development; production needs provider hardening and HTTPS. ADR-005 defines local issuer consistency.

## ADR-014: Focused persistence ports

- Problem: separate persistence concerns while keeping Application independent of EF Core and Dapper.
- Options: generic repositories; persistence inside Application handlers; use-case-oriented command stores and query interfaces.
- AI recommendation: focused persistence ports implemented by Infrastructure.
- Decision: Application handlers depend on command-store and query/validation interfaces. Infrastructure owns EF projections and shared SQL transactions with Dapper writes. No generic repository or additional unit-of-work abstraction is introduced.
- Reason: explicit CQRS direction, testable orchestration and transaction ownership where the database invariants can be enforced atomically.
- Trade-offs: additional small interfaces and DTOs; some application prechecks are deliberately repeated under transaction locks to handle races.

## ADR-015: Explicit development user provisioning

- Problem: development needs a ready-to-use API account with all six permissions, including when the realm already exists.
- Options: grant permissions to all authenticated users; embed user credentials in the import; provision one named user through the Admin REST API.
- AI recommendation: an explicit development initializer with environment-provided credentials.
- Decision: Compose runs keycloak-init before API startup to ensure application roles, Swagger mappers/scope mappings and the adminInventory user. Passwords come from the untracked .env. Existing user passwords and unrelated users are preserved. Only the named user receives all six API roles.
- Reason: works with fresh imports and existing realms while retaining the authorization policies and avoiding committed credentials. Uses the documented [Keycloak Admin REST API](https://www.keycloak.org/docs-api/26.7.4/rest-api/index.html) for user creation and client role mappings.
- Trade-offs: development initialization requires administrator credentials and a small Python container. Password rotation remains explicit; the development account does not grant Keycloak administration privileges.
