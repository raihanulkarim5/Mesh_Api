# Finance Module — Backend Specification

Draft spec, not code. Finance is the biggest and most sensitive module (real
money data + a credential vault), so this gets written down and agreed on
before any entity/controller work starts — same reasoning as why Projects/
Skills and cross-module Linking each got flagged for their own design pass
in `BACKEND_CHECKLIST.md`.

All entities below follow the same conventions already established in
Entries/Tasks: `Guid Id`, `string UserId` (FK to `AspNetUsers`, cascade
delete), `CreatedAtUtc`/`UpdatedAtUtc`, simple string lists as JSON columns,
child collections that need individual identity as real tables. One thing
that's *new* here and matters a lot: **every money column is `decimal(18,2)`,
never `float`/`double`** — floating point has no place representing currency.

---

## Entities

### BankAccount
`BankName`, `Branch`, `AccountType` (Checking/Savings/Credit, plain string),
`AccountNumberMasked` (e.g. `****2891` — only the last 4 digits are ever
stored/known; the real number never touches this system), `Currency`,
`Balance` (decimal), `Notes`, `OtpEmailEnabled`, `OtpMobileEnabled`, `Order`.

Plus the two encrypted fields — see **Credential Vault** below.

### BankCard (child of BankAccount)
`CardNumberMasked` (e.g. `**** **** **** 4432`), `CardholderName`,
`ExpiryMonth`, `ExpiryYear`, `IsDefault`. CVV and card PIN are **not stored
at all** — unlike the account's password/PIN, there's no "retrieve later"
requirement for these in the frontend, so there's nothing to encrypt or
reveal; they're accepted on entry (for UI completeness / masking) and
discarded rather than persisted.

### BalanceEntry (child of BankAccount)
`Date`, `Amount` (decimal, positive = deposit, negative = withdrawal),
`Note`. This is the running history the frontend's monthly filter reads.

### Category
`Name`, `ColorHex`. Shared across Expenses and Budgets. Per-user (not
global) — matches the "fully isolated" multi-user decision.

### Expense
`Amount` (decimal), `Date`, `CategoryId` (FK), `BankAccountId` (FK,
nullable — a cash expense has no account), `PaymentMethod` (Cash/Card/Bank
transfer), `Note`, `Order`.

### Budget
Monthly, per category: `CategoryId` (FK), `MonthlyLimit` (decimal), `Month`
(stored as `YYYY-MM` string — simplest match for how the frontend already
keys by month, no need for a real `DateOnly` here), `Order`.

### BudgetPlan
The "future plans" concept (saving toward an asset/business/goal — separate
from monthly budgets): `Name`, `PlanType` (plain string, frontend already
made this free-text with suggestions, not a fixed enum), `TargetAmount`
(decimal), `CurrentAmount` (decimal), `TargetDate` (nullable), `Notes`,
`Order`.

### Person
`Name`, `Phone` (nullable), `Email` (nullable), `Address` (nullable).
Reusable across multiple DebtLoan records for the same person, per-user.

### DebtLoan
`Type` ("Loan Given" or "Debt" — merged from an earlier 3-type design once
it became clear "Loan Received" and "Debt" were the same thing from the
user's side), `Amount` (decimal), `PersonId` (FK to Person — frontend
currently duplicates person fields onto each DebtLoan record directly;
worth normalizing to a real FK now that this is a real database, see
**Open question** below), `Purpose`, `Date`, `DueDate` (nullable),
`PaidDate` (nullable), `Status` (Open/Partial/Settled), `AmountRemaining`
(decimal), `Notes`, `Order`.

---

## Credential Vault

This is the one genuinely different piece of security work in the whole
backend — every other "password" in this system (user login, refresh
tokens) is **hashed** (one-way, only ever verified, never recovered). The
bank account password/PIN is the opposite: the whole point, per your
original ask, is **"if I forget it, I can retrieve it."** That means
**encryption, not hashing** — reversible by design.

**Approach:** ASP.NET Core's built-in Data Protection API
(`Microsoft.AspNetCore.DataProtection`, already part of the shared
framework — no new NuGet package needed). Get an `IDataProtector` scoped
to a purpose string like `"BankAccountCredentials"`, and use
`.Protect(plaintext)` / `.Unprotect(ciphertext)` to encrypt/decrypt the
password and PIN before storing/after reading. Data Protection handles key
generation, rotation, and storage itself — nothing to build there.

- `BankAccount.EncryptedPassword` / `EncryptedPin` — the `Protect()` output,
  stored as-is in the DB. Never returned by any `GET` endpoint in plain form.
- `PUT /accounts/{id}/credentials` — accepts the real password + PIN in the
  request body (over HTTPS), encrypts immediately, discards the plaintext
  after the response is sent. This is a write-only operation from the API's
  perspective past this point.
- `POST /accounts/{id}/credentials/reveal` — decrypts and returns the
  plaintext password + PIN.

**Known gap, matching what's already deferred on the frontend:** the
frontend's "reveal" flow is OTP-gated in the UI, but real OTP delivery
(email/SMS) is explicitly on the waiting list, same as Google OAuth. So for
now, `reveal` is gated by nothing more than a valid JWT (i.e., "is this
authenticated user's own account") — not a true second factor. This should
be called out plainly, not quietly treated as "done," since it's the one
place in the system where "encrypted" could read as more secure than it
currently is. Revisit together with whenever OTP delivery actually gets
built.

**Key storage for Data Protection itself:** by default, keys land in a
local folder (fine for a single-instance LocalDB dev setup). If this ever
runs on more than one server instance, the keys need a shared store (e.g.
a DB table via `PersistKeysToDbContext`, or Azure Blob) — noted here so it
doesn't get forgotten once hosting is decided, not something to solve now.

---

## Open question before building this

**DebtLoan → Person: FK now, or keep duplicating fields?** The frontend
currently stores `personName`/`personPhone`/`personEmail`/`personAddress`
directly on each `DebtLoan` record (denormalized), with a separate
`Person` list used only to populate the "pick an existing person" dropdown
when creating a new one — the two aren't actually linked in the frontend's
data model today. Backend has a choice:
1. **Match the frontend exactly** — duplicate person fields directly on
   `DebtLoan`, `Person` is just a separate convenience list, no FK between
   them. Simplest, but means editing a person's phone number doesn't update
   their past debt records (and shouldn't necessarily — a debt's contact
   info might be a snapshot at the time).
2. **Real FK** — `DebtLoan.PersonId` referencing `Person`, name/contact
   info only lives in one place. Cleaner data, but changes the API
   contract slightly (frontend would send a `personId` instead of the raw
   fields) and needs a small frontend update alongside the backend work.

No strong pull either way from what's built so far — flagging it here so
it's a deliberate choice, not an accident of whichever way I default when
actually writing the entity.

---

## Endpoints (once the above is settled)

Same shape as `EntriesController`/`TasksController` — everything scoped to
`GetUserId()`, standard CRUD per entity, plus:
- `GET /accounts/{id}/balance-history?month=YYYY-MM`
- `PUT /accounts/{id}/credentials`, `POST /accounts/{id}/credentials/reveal`
- `POST /accounts/{id}/cards`, `PUT/DELETE /accounts/{id}/cards/{cardId}`
- `POST /accounts/{id}/balance-entries` (the "add balance" / top-up flow)

Not written yet — this document is the design conversation, not the
implementation.
