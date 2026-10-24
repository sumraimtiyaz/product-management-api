# Product Management API

A RESTful API for managing **products** and their **items**, built with .NET 8, ASP.NET Core Web API, EF Core and SQL Server.
It follows the layered structure from the assessment brief and uses JWT access tokens with single-use, rotating refresh tokens.

## Contents

- [Tech stack](#tech-stack)
- [Architecture](#architecture)
- [API](#api)
- [Authentication flow](#authentication-flow)
- [Getting started](#getting-started)
- [Run with Docker](#run-with-docker)
- [Testing](#testing)
- [Deployment](#deployment)
- [Decisions on gaps in the brief](#decisions-on-gaps-in-the-brief)
- [Design principles used in the code](#design-principles-used-in-the-code)

## Tech stack

| Concern | Choice |
|---|---|
| Framework | .NET 8, ASP.NET Core Web API (controllers) |
| Database | SQL Server, Entity Framework Core 8 (code-first, migrations) |
| Authentication | JWT bearer access tokens + rotating refresh tokens, users and roles in ASP.NET Core Identity |
| Validation | FluentValidation |
| API versioning | Asp.Versioning (URL segment, `/api/v1/...`) |
| Documentation | Swagger / OpenAPI (Swashbuckle) with XML comments |
| Logging | Serilog, structured JSON on the console |
| Testing | xUnit, Moq, `WebApplicationFactory`, SQLite in-memory |
| Containers | Docker, Docker Compose |

## Architecture

```
src/
  API              Controllers, filters, middleware, DI/pipeline extensions, Program.cs, appsettings
  Application      DTOs, interfaces, mapping, services, validators
  Domain           Entities, enums, exceptions   (Events/ is reserved; nothing needs it yet)
  Infrastructure   EF Core (Data), Identity, Logging  (Services/ is reserved; there are no external services yet)
tests/
  Application.Tests     Unit tests: domain rules, validators, services (Moq)
  Infrastructure.Tests  Repositories, audit interceptor, identity and tokens against a real SQLite database
  API.Tests             End-to-end tests through the HTTP pipeline (WebApplicationFactory)
```

Dependencies point inwards only: `API → Application → Domain` and `Infrastructure → Application → Domain`.
`API` references `Infrastructure` only to register it at startup (the composition root); controllers depend on
`Application` interfaces and never on EF Core. `Domain` references nothing.

Request flow: `SecurityHeaders → request logging → ExceptionHandling → HSTS/HTTPS redirect → compression → status-code pages → Swagger → CORS → authentication → authorization → controller`
(a validation filter runs just before each action).

## API

All routes are under `/api/v1`. Every endpoint requires a bearer token except the four `/auth` endpoints.
Interactive documentation is at **`/swagger`** (use *Authorize* and paste an access token).

| Method | Route | Purpose | Access |
|---|---|---|---|
| POST | `/auth/register` | Create a user (role `User`) and sign in → `201` | anonymous |
| POST | `/auth/login` | Sign in → `200` | anonymous |
| POST | `/auth/refresh` | Exchange a refresh token for a new pair → `200` | anonymous |
| POST | `/auth/revoke` | Revoke a refresh token (sign out) → `204` | anonymous |
| GET | `/products?pageNumber=1&pageSize=20` | List products (paged) | signed in |
| GET | `/products/{id}` | Get one product | signed in |
| POST | `/products` | Create → `201` + `Location` | signed in |
| PUT | `/products/{id}` | Update | signed in |
| DELETE | `/products/{id}` | Delete → `204` (`409` while the product still has items) | **Admin** |
| GET | `/products/{productId}/items` | List a product's items (paged) | signed in |
| GET | `/products/{productId}/items/{itemId}` | Get one item | signed in |
| POST | `/products/{productId}/items` | Create → `201` + `Location` | signed in |
| PUT | `/products/{productId}/items/{itemId}` | Update | signed in |
| DELETE | `/products/{productId}/items/{itemId}` | Delete → `204` | signed in |

Conventions:

- JSON in and out, camelCase property names, UTC timestamps with a `Z` suffix.
- Collections are paged: `pageSize` is 1–100 (default 20). The response contains `items`, `pageNumber`, `pageSize`, `totalCount` and `totalPages`.
- Every error is an RFC 7807 `application/problem+json` response with `status`, `title`, `detail` (where useful), `instance` and `traceId`.
  Validation failures add an `errors` object keyed by field name.

| Status | Meaning |
|---|---|
| 400 | Validation failed or a business rule was broken |
| 401 | Missing, invalid or expired token; bad credentials; bad refresh token |
| 403 | Signed in but not allowed (for example a non-admin deleting a product) |
| 404 | Resource not found (an item is only found under its own product) |
| 409 | Conflict: duplicate email, or deleting a product that still has items |
| 500 | Unexpected error (details are only included in the Development environment) |

`createdBy` / `modifiedBy` are filled in automatically from the signed-in user (their email) by one EF Core interceptor.
Clients never send them.

## Authentication flow

```
Client                                         API
  | POST /auth/register  or  /auth/login        |
  |-------------------------------------------->|  check credentials (lock out after 5 failures)
  |                                             |  issue access JWT (15 min) + refresh token (7 days)
  |<--------------------------------------------|  only a SHA-256 hash of the refresh token is stored
  |                                             |
  | GET /products   Authorization: Bearer <jwt> |
  |-------------------------------------------->|  validate signature, issuer, audience, lifetime, role
  |                                             |
  | (access token expires)                      |
  | POST /auth/refresh  { refreshToken }        |
  |-------------------------------------------->|  token valid  -> revoke it, issue a NEW pair (rotation)
  |<--------------------------------------------|
  |                                             |
  | POST /auth/refresh  { an already-used one } |
  |-------------------------------------------->|  reuse detected -> revoke ALL of that user's tokens, 401
```

- Roles are `User` (default for registration) and `Admin`. Only `Admin` may delete products.
- The first administrator is created at startup from `AdminSeed:Email` / `AdminSeed:Password`, if supplied.
- Passwords are hashed by ASP.NET Core Identity (PBKDF2). Password policy: at least 8 characters with upper case, lower case, a digit and a symbol.

## Getting started

Prerequisites: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and a SQL Server instance
(SQL Server LocalDB on Windows works out of the box; elsewhere use Docker or point the connection string at any instance).

Secrets are never stored in the repository. Supply them with [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) for local development:

```bash
cd src/API
dotnet user-secrets set "Jwt:SigningKey" "<a random string of at least 32 characters>"
dotnet user-secrets set "AdminSeed:Email" "admin@example.com"
dotnet user-secrets set "AdminSeed:Password" "<a password that meets the policy>"
dotnet run --launch-profile https
```

In the Development environment the API uses LocalDB (`appsettings.Development.json`) and applies the EF Core migrations automatically
on startup. Then open <https://localhost:7080/swagger>.

To use a different SQL Server, override the connection string:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=ProductManagement;User Id=...;Password=...;TrustServerCertificate=True"
```

### Configuration reference

| Setting | Purpose | Where it comes from |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection | `appsettings.Development.json` (LocalDB), otherwise secrets/environment |
| `Jwt:SigningKey` | Signs access tokens (**required**, 32+ chars, the app refuses to start without it) | secrets/environment |
| `Jwt:Issuer`, `Jwt:Audience` | Token issuer and audience | `appsettings.json` |
| `Jwt:AccessTokenMinutes`, `Jwt:RefreshTokenDays` | Token lifetimes (15 / 7) | `appsettings.json` |
| `AdminSeed:Email`, `AdminSeed:Password` | First administrator (optional) | secrets/environment |
| `Database:MigrateOnStartup` | Apply EF Core migrations at startup | `true` in Development and in Docker, `false` otherwise |
| `Cors:AllowedOrigins` | Browser origins allowed to call the API; empty means none | `appsettings.json` |
| `Swagger:Enabled` | Serve `/swagger` | `appsettings.json` |

ASP.NET Core maps `Section__Key` environment variables onto these settings (for example `Jwt__SigningKey`).

### Database

The schema is created by the EF Core migration in `src/Infrastructure/Data/Migrations`. It contains the `Product` and `Item` tables exactly as in the brief
(same columns, types, identity keys and foreign key) plus the ASP.NET Core Identity tables, a `RefreshToken` table and the index `IX_Item_ProductId`.
`database/schema.sql` is a reference copy of the two brief tables (with the creation order fixed) for anyone who wants to create them by hand.

Add a migration after changing the model (the EF tool is pinned in `.config/dotnet-tools.json`):

```bash
dotnet tool restore
dotnet ef migrations add <Name> --project src/Infrastructure --startup-project src/API --output-dir Data/Migrations
```

## Run with Docker

`docker compose up` starts SQL Server and the API. The API listens on HTTP (8080, redirects to HTTPS) and HTTPS (8443, TLS 1.2 and 1.3).

1. Copy `.env.example` to `.env` and replace every placeholder (the file is git-ignored).
2. Create a development HTTPS certificate for the container:

   ```bash
   dotnet dev-certs https -ep ./certs/aspnetapp.pfx -p <the HTTPS_CERT_PASSWORD you put in .env>
   dotnet dev-certs https --trust
   ```
3. Start everything:

   ```bash
   docker compose up --build
   ```
4. Open <https://localhost:8443/swagger>.

The API container waits for SQL Server to be healthy, applies the migrations and seeds the roles and the administrator on startup.

The very first start of a new database volume is slow: SQL Server upgrades its system databases, which took about 100 seconds on a modest laptop,
and the API then applies its migration before it starts listening. Give it a couple of minutes the first time; later starts are quick.
Stop with `docker compose down` (keeps the data) or `docker compose down -v` (deletes it).

### Only the database in Docker (day-to-day development)

You do not need to run the API in Docker to develop. Run just SQL Server in a container and the API from Visual Studio or `dotnet run`:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<a strong password>" -e "MSSQL_MEMORY_LIMIT_MB=1536" -p 1433:1433 --name productmgmt-sql -v productmgmt-sql-data:/var/opt/mssql -d mcr.microsoft.com/mssql/server:2022-latest
```

SQL Server logs "ready for client connections" well before logins work, so do not trust that line on a first start.
Wait until this prints `1` (repeat it if it says the login failed):

```bash
docker exec productmgmt-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "<the same password>" -Q "SELECT 1"
```

Then point `ConnectionStrings:DefaultConnection` at it (in `appsettings.Development.json` or user secrets) and run the API; with `Database:MigrateOnStartup` set to `true` it creates the database and tables itself:

```
Server=localhost,1433;Database=ProductManagement;User Id=sa;Password=<the same password>;TrustServerCertificate=True
```

To apply the migrations by hand instead (the EF tool ignores appsettings, so the connection is passed explicitly):

```bash
dotnet ef database update --project src/Infrastructure --startup-project src/API --connection "<the connection string above>"
```

## Testing

```bash
dotnet test
```

| Project | Covers |
|---|---|
| `Application.Tests` | Domain invariants, validators, product/item/auth services with mocked repositories (rotation, reuse detection, delete rules) |
| `Infrastructure.Tests` | Repositories (tracking, paging, scoping, foreign-key restriction), audit interceptor, JWT service, identity service and seeder against real Identity stores |
| `API.Tests` | The full HTTP pipeline: authentication and refresh flows, authorization, CRUD for products and items, validation and error shape, paging, Swagger, security headers, CORS and compression |

The data-access and API tests run against **SQLite in memory**, so they need no SQL Server or Docker.
The checked-in migrations target SQL Server, so the API tests create their schema from the EF model instead.

## Deployment

At a high level:

1. Build the image from the `Dockerfile` (multi-stage; runs as a non-root user) and push it to your registry.
2. Provide the configuration as environment variables or a secret store: the connection string, `Jwt__SigningKey`, the HTTPS certificate (or terminate TLS at a load balancer), and optionally `AdminSeed__*`.
3. Decide how the schema is applied. Either set `Database__MigrateOnStartup=true` (convenient, used by Compose), or leave it `false` and run the migrations from your pipeline
   (`dotnet ef migrations script --idempotent`).
4. Set `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ... to the browser origins that may call the API, and consider `Swagger__Enabled=false` for public production environments.
5. Logs are JSON events on stdout, ready for any log collector.

## Decisions on gaps in the brief

The brief left a few things open or inconsistent. These are the choices made:

| Gap in the brief | Decision |
|---|---|
| The DDL creates `Item` before `Product`, but `Item` references `Product` | Order fixed (`database/schema.sql`); the migration creates `Product` first |
| "JSDoc comments" for a C# project | XML documentation comments (`///`), which also feed Swagger |
| xUnit/Moq/WebApplicationFactory are required but the testing section says "or any other framework" | Used all three as listed in the tech stack; unit and integration tests both exist |
| "Proper indexing strategy" but the DDL has only primary keys | Added `IX_Item_ProductId`, because items are always read by product and SQL Server does not index foreign keys |
| `Delete` vs. a foreign key with no `ON DELETE` rule | Kept the foreign key as written (no cascade); deleting a product that still has items returns `409 Conflict` |
| `CreatedBy NOT NULL` needs an identity, but the brief never says which endpoints are authenticated | Every endpoint requires a token except the auth endpoints; `CreatedBy`/`ModifiedBy` hold the user's email |
| JWT + refresh-token rotation + roles, but no user, role or token tables | ASP.NET Core Identity tables plus a `RefreshToken` table (stores only hashes) |
| Roles are mentioned but not defined | `User` and `Admin`; only `Admin` can delete products |
| `Item` appears in the schema, but the task says "CRUD around Products" | Full CRUD for items, nested under their product |
| Repository / unit-of-work interfaces have no designated folder | `Application/Interfaces`, so `Infrastructure` implements contracts owned by `Application` |
| FluentValidation, API versioning and a mapper are mentioned but not in the tech stack | FluentValidation and Asp.Versioning added; mapping is plain extension methods (no mapper library needed for two small entities) |
| `Domain/Events` and `Infrastructure/Services` exist, but nothing in the brief needs them | Folders kept, intentionally empty |
| Secrets (JWT key, SQL password) vs. a public repository | No secrets committed: user secrets locally, `.env` (git-ignored) for Compose |
| `Item` has no audit columns | Kept as in the brief; only `Product` is audited |
| `DATETIME` rounds to about 3 ms, so a create response could differ slightly from later reads | Audit timestamps are recorded to the whole second, which `DATETIME` stores exactly |
| An item quantity rule is not stated | Quantity must be zero or greater |

## Design principles used in the code

- **Single responsibility** – controllers translate HTTP; services hold use-case logic; validators validate; repositories query; middleware maps exceptions to responses; token creation, user storage and the current-user lookup are separate classes.
- **Open/closed** – a new rule is a new validator class; the exception-to-status mapping is a table; audit columns are stamped in one interceptor rather than in every service.
- **Liskov substitution** – every implementation honours its interface contract (`null` for "not found", no `NotImplementedException`), so real and mocked repositories are interchangeable.
- **Interface segregation** – small interfaces (`IProductRepository`, `IItemRepository`, `ITokenService`, `ICurrentUserService`, `IUnitOfWork`) instead of one generic repository.
- **Dependency inversion** – `Application` owns the interfaces, `Infrastructure` implements them, and the composition root wires them; time comes from `TimeProvider`.
- **Rich domain model** – entities have private setters and create/change themselves through methods that enforce their own rules.
- **Fail fast and secure by default** – guard clauses, options validated at startup, a fallback authorization policy that requires sign-in unless an endpoint opts out.
- **Performance** – `AsNoTracking` for reads, stable ordering and paging on every collection, async with `CancellationToken` throughout, response compression.
