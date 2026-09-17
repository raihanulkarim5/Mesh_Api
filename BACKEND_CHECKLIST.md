# Mesh API — Backend Development Checklist

Living document, updated as chunks land. Order follows the frontend's own module
priority; each module's backend work always ends with swapping that module's mock
service in the frontend's `services/index.ts` for the real one — no other frontend
files should need to change, per the mock-first design.

---

## Phase 0 — Foundation

- [x] Solution skeleton: `Mesh.Api` / `Mesh.Domain` / `Mesh.Infrastructure`
- [x] `ApplicationUser` (extends Identity's `IdentityUser`) + `RefreshToken` entities
- [x] `MeshDbContext` (`IdentityDbContext<ApplicationUser>` + `RefreshTokens` table)
- [x] EF Core + Identity + JWT Bearer + Swagger + CORS wired in `Program.cs`
- [x] `GET /api/v1/health` (anonymous) — pipeline smoke test
- [ ] **Verified locally** — `dotnet restore`, migration, `dotnet run`, health check all confirmed working (pending your report back)

## Phase 1 — Auth endpoints

- [ ] `POST /api/v1/auth/register` (email + password, via Identity's `UserManager`)
- [ ] `POST /api/v1/auth/login` (issues access token + refresh token)
- [ ] `POST /api/v1/auth/refresh` (rotates/validates refresh token, issues new access token)
- [ ] `POST /api/v1/auth/logout` (revokes the refresh token — sets `RevokedAtUtc`)
- [ ] `POST /api/v1/auth/google` (verifies Google ID token server-side, finds-or-creates user, auto-links by email, issues Mesh tokens)
- [ ] Google Cloud Console OAuth client set up, Client ID in config (separate from this repo — you'll need to create this in Google's console)
- [ ] Demo account seeded (`demo@meshapp.local` / `Demo@12345`) with mock service demo data
- [ ] Swagger "Authorize" flow tested end-to-end (register → login → paste token → hit a protected endpoint)

## Phase 2 — Entries (first real module, proof of the full pipeline)

- [ ] `Entry` entity (title, type, description, tags, imageUrl, favorite, `UserId`)
- [ ] Migration + DB update
- [ ] Controller: GET (list + single), POST, PUT, DELETE — scoped to the authenticated user
- [ ] Image upload: real file storage (local disk for now) instead of base64-in-JSON
- [ ] Swap `entryService` in the frontend from mock to real, confirm nothing else changes

## Phase 3 — Tasks

- [ ] `Task` entity + `ChecklistItem` (child collection)
- [ ] Controller: full CRUD + status/priority updates + checklist item toggle
- [ ] Swap `taskService`

## Phase 4 — Journal

- [ ] `JournalEntry` entity (logType, date, mood, content, wins/mistakes/learnings/gratitude, tags)
- [ ] Controller: full CRUD + move up/down (ordering)
- [ ] Swap `journalService`

## Phase 5 — Projects & Skills

- [ ] `Project` entity + Milestones (nested) + linked Tasks/JournalEntries/Entries
- [ ] `Skill` entity + Milestones/Syllabus (nested) + linked Tasks/Projects
- [ ] These have the least flat schemas in the app — expect a real design conversation here, not just a mechanical CRUD pass
- [ ] Swap `projectService` / `skillService`

## Phase 6 — Knowledge Base

- [ ] `KnowledgeItem` entity (title, content, category, folder, tags, favorite)
- [ ] Controller: full CRUD
- [ ] Swap `knowledgeService`

## Phase 7 — Finance (biggest, most sensitive module)

- [ ] `BankAccount` entity (branch, notes, OTP flags, currency, balance)
- [ ] `BankCard` (child of account)
- [ ] `BalanceEntry` (child of account, for the monthly balance history)
- [ ] **Credential vault** — separate design conversation before building: real encryption-at-rest for password/PIN (ASP.NET Core Data Protection API), and the "authenticate to retrieve" flow server-side (this is encrypt+decrypt, not hash+verify — different from normal password storage)
- [ ] `Expense`, `Category`, `Budget`, `BudgetPlan` entities
- [ ] `DebtLoan` + `Person` entities
- [ ] Controllers for all of the above, scoped per user
- [ ] Swap `financeService`

## Phase 8 — Cross-module Linking

- [ ] Design the `Link` table for a polymorphic relationship (Task ↔ Journal ↔ Project ↔ anything) — relational DBs don't do this natively, needs its own conversation (a generic type+id pairs table vs. something more rigid)
- [ ] Endpoint(s) to create/remove links
- [ ] Swap the `LinkPickerModal`'s reads and the add/remove calls in Tasks/Journal detail pages from local array mutation to real API calls

## Cross-cutting (address when it actually matters, not all up front)

- [ ] Lock down CORS to real origins once there's a hosting URL (currently wide open for dev)
- [ ] Decide hosting/deployment target (still undecided)
- [ ] Consistent error response shape (`ProblemDetails` is the default plan)
- [ ] Logging strategy (built-in `ILogger` vs. Serilog)
- [ ] Automated tests — currently none anywhere in the project (frontend or backend); worth a deliberate decision on whether/when to start, not an accident of running out of time
- [ ] Rate limiting / basic abuse protection on auth endpoints (Identity's lockout covers brute-force login attempts already; registration/refresh spam is still open)

---

**Not on this list because they're explicitly deferred, not forgotten:**
email verification, password reset, roles/permissions, mobile app (Flutter, "much later"), Apple Sign-In (no iOS plans).
