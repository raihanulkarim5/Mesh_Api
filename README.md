# Mesh API

Backend for [Mesh](https://github.com/raihanulkarim5/NecleusOS_Frontend) — a personal
productivity system (tasks, journal, finance, projects, skills, knowledge base, all
cross-linked). This repo is the ASP.NET Core Web API + SQL Server backend that the
frontend's mock services are being swapped out for, module by module.

## Status: Chunk 1 — solution skeleton + Identity/JWT wiring

This is the *first* reviewable piece, per our plan of building in small chunks since I
can't compile or run .NET in the sandbox I write this code from. What's here:

- Solution structure: `Mesh.Api` (web/controllers) / `Mesh.Domain` (entities) /
  `Mesh.Infrastructure` (EF Core + Identity)
- `ApplicationUser` (extends Identity's `IdentityUser`) and `RefreshToken` entities
- `MeshDbContext` (`IdentityDbContext<ApplicationUser>` + a `RefreshTokens` table)
- `Program.cs` wired for: SQL Server via EF Core, ASP.NET Core Identity, JWT Bearer auth
  (overriding Identity's default cookie scheme), Swagger with a Bearer-token "Authorize"
  button, permissive dev CORS
- One endpoint: `GET /api/v1/health` (anonymous) — just proves the pipeline is up

**Not here yet** (next chunks): register/login/refresh/logout endpoints, Google
Sign-In verification, the Entries module's own entity + endpoints, the demo-account
seed data.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (current LTS)
- SQL Server LocalDB (ships with Visual Studio's "Data storage and processing"
  workload, or install [SQL Server Express LocalDB](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb) standalone)
- The `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`

## First-time setup

From the repo root:

```bash
dotnet restore
```

Set the JWT signing key as a user secret (never committed — this is what makes
`appsettings.json`'s `Jwt:Key` intentionally blank):

```bash
cd src/Mesh.Api
dotnet user-secrets set "Jwt:Key" "<any long random string, 32+ characters>"
cd ../..
```

A quick way to generate one: `openssl rand -base64 32`.

(`Google:ClientId` is also blank in `appsettings.json` — leave it for now, it's not
wired into any code yet. Will matter once the Google Sign-In endpoint shows up.)

Create and apply the initial migration:

```bash
dotnet ef migrations add InitialCreate --project src/Mesh.Infrastructure --startup-project src/Mesh.Api
dotnet ef database update --project src/Mesh.Infrastructure --startup-project src/Mesh.Api
```

The `--project`/`--startup-project` split is because `MeshDbContext` lives in
`Mesh.Infrastructure`, but configuration (the connection string) is only resolved
through `Mesh.Api`'s `Program.cs`/`appsettings.json`.

Run it:

```bash
dotnet run --project src/Mesh.Api
```

This should open a Swagger UI page automatically. Hitting `GET /api/v1/health` should
return `{ "status": "ok", "utc": "..." }`. If that works, the whole pipeline —
config → DB connection → Identity schema → JWT setup → routing — is confirmed working
end to end, and the next chunk (actual auth endpoints) can build on top of it safely.

## Please report back

Since I can't verify any of this myself: specifically flag whether `dotnet restore`,
the migration commands, and `dotnet run` + hitting `/api/v1/health` all worked cleanly,
or paste back whatever error shows up if not. Small chunks, verified one at a time.
