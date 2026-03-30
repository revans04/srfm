# AGENTS.md

## 1. System Purpose
SRFM is a multi-tenant household finance system for shared budgeting and reconciliation. It is intended to replace EveryDollar, Mint (discontinued), Quicken, and Excel for families tracking budgets, transactions, accounts, and net worth.

The user base spans two personas:
- **Family budget managers** (majority): Non-experts migrating from EveryDollar, Mint, or spreadsheets. Expect guided workflows, plain language, and mobile-first convenience.
- **Accountants/bookkeepers** (minority): Power users migrating from Quicken or Excel. Expect data density, reconciliation workflows, and detailed reporting.

Core intent inferred from code:
- Manage a `Family` as the primary collaboration boundary.
- Scope most financial data to `family_id`, and increasingly to `entity_id` (Family/Business/Rental/etc).
- Support month-based budgets with category targets, transactions, carryover funds, account/snapshot tracking, import/reconciliation workflows, and goal tracking.
- Use Firebase identity for authentication while persisting primary domain data in Supabase/PostgreSQL.

Migration state is mixed:
- Most domain CRUD is Supabase-backed (`FamilyService`, `BudgetService`, `AccountService`, `GoalService`, `UserService`).
- Auth/email verification still uses Firestore in `AuthController`.
- Statements and account import are partially implemented (`NotImplementedException` paths exist).

UI/UX review status:
- A comprehensive UI/UX review was completed March 2026 (see `UI_UX_Review_SteadyRise.md`). Findings have been integrated into `docs/design_system.md`, `docs/needed_features.md`, and `docs/product_requirements.md`. Agents implementing UI changes should consult these documents and the review for context on known issues.

## 2. High-Level Architecture
Backend (`/api`):
- HTTP layer: `Controllers/*`.
- Domain/data layer: `Services/*` with direct SQL through `Npgsql` (no repository abstraction).
- Models/DTOs: `Models/*` shared by controller/service serialization.
- Auth filter: `Filters/AuthorizeFirebaseAttribute.cs` verifies Firebase ID token and injects `UserId` / `Email` into `HttpContext.Items`.
- Infra: `SupabaseDbService` (connection factory), logging providers (`Logging/*`), external email service (`BrevoService`).

Frontend (`/q-srfm`):
- Quasar + Vue 3 + Pinia SPA.
- `src/dataAccess.ts` is the primary API adapter and contract surface.
- Pinia stores (`src/store/*`) own client state; `budgets` store is intentionally not persisted to localStorage.
- Composition utilities (`src/composables/*`) orchestrate reconciliation, goals, statement UI flow.

Dependency direction:
- Frontend UI -> stores/composables -> `dataAccess` -> REST API.
- API controllers -> services -> Supabase/Postgres.
- Cross-cutting auth/logging/email are injected into controllers/services.
- No domain layer independent of SQL currently; SQL is the source of behavior.

## 3. Core Invariants (Must Not Be Broken)
- Firebase Bearer token is the auth primitive for protected endpoints; `AuthorizeFirebase` populates `HttpContext.Items["UserId"]` and `HttpContext.Items["Email"]`.
- `family_members` membership is the primary authorization relation for family-scoped resources (accounts, snapshots, statements endpoints).
- Budget carryover logic only applies to `BudgetCategory.IsFund == true` and clamps negative carryover to zero.
- Transaction category rows are replaced on write; every persisted transaction must have at least one category split (`Income` or `Uncategorized` fallback in backend).
- `entity_id` is a first-class partition key for budgets/goals and must stay consistent when merging budgets or creating derived transactions.
- Goal-linked budget categories are intentionally hidden from regular budget/category queries via `goals_budget_categories` exclusion logic in `BudgetService.LoadBudgetDetails`.
- Numeric/account enum domains are constrained in API logic and/or DB enum casting (`account_type`, `account_category`); keep frontend values aligned (`Bank/CreditCard/Investment/Property/Loan`, `Asset/Liability`).
- Date/month ordering is semantic (`YYYY-MM` strings); many flows assume lexicographic sort works for chronology.

## 4. Module Responsibilities
`/api/Program.cs`
- Service registration, Firebase bootstrapping from `GOOGLE_APPLICATION_CREDENTIALS_JSON`, CORS allowlist, logging providers, Swagger.

`/api/Filters`
- Token verification and request user context wiring.

`/api/Controllers/BudgetController.cs`
- Budget/transaction/import/reconcile API surface; delegates business logic to `BudgetService`.

`/api/Services/BudgetService.cs`
- Largest business module: budget CRUD, transaction CRUD, carryover propagation, merge, import docs/rows, reconciliation updates, edit history logging.

`/api/Controllers/FamilyController.cs` + `/api/Services/FamilyService.cs`
- Family creation/membership/entity management and invite flows, mostly owner-gated operations.

`/api/Controllers/AccountController.cs` + `/api/Services/AccountService.cs`
- Accounts/snapshots CRUD with family membership checks and personal-account ownership checks (`account.user_id`).

`/api/Controllers/GoalController.cs` + `/api/Services/GoalService.cs`
- Goal lifecycle and goal-to-budget-category association.

`/api/Controllers/AuthController.cs`
- Google login -> Firebase custom token issuance, ensure-profile, email verification; still Firestore-backed.

`/api/Services/StatementService.cs`
- Read support present; write/unreconcile methods are placeholders.

`/q-srfm/src/dataAccess.ts`
- Canonical frontend API client and shape mapper. Any API contract changes should be reflected here first.

`/q-srfm/src/store`
- Client state ownership. `family`, `budgets`, `statements`, `ui`, `auth`, etc.

`/q-srfm/src/composables`
- Cross-page business workflows (transaction registry/reconciliation, goals behavior, placeholder statement logic).

## 5. Data & State Ownership Rules
Server ownership:
- Source of truth: Supabase/Postgres tables (`families`, `family_members`, `entities`, `budgets`, `budget_categories`, `transactions`, `transaction_categories`, `goals`, `goals_budget_categories`, `accounts`, `snapshots`, `imported_*`, etc.).
- Firestore remains source for auth-related user verification metadata in `AuthController`.

Client ownership:
- `useBudgetStore` owns in-memory budget cache by `budgetId`.
- Budget cache is intentionally excluded from persistence plugin to avoid localStorage quota failures.
- `useFamilyStore` owns current family and selected entity context used across pages.
- `dataAccess` performs runtime normalization (timestamps, optional fields, shape coercion) before state enters stores.

Flow constraints:
- Most pages assume `familyStore.selectedEntityId` is set; entity selection drives data filtering and budget loading.
- Many operations optimistically mutate Pinia after API success; preserve these update paths to avoid stale UI.

## 6. API & Contract Rules
Behavioral rules inferred from implementation:
- Protected endpoints expect `Authorization: Bearer <Firebase ID Token>`.
- IDs are predominantly UUIDs in DB, but some API fields use string IDs (`budget.id` stored as string).
- Several routes accept/return mixed casing and optional fields; frontend uses mappers to normalize.

Important contract mismatches/drift to preserve awareness of:
- Frontend calls `POST /api/statements/finalize` (`dataAccess.finalizeStatement`) but no matching API controller route exists.
- `StatementService` write operations are not implemented; UI paths exist for save/delete/unreconcile.
- Frontend `AcceptInvitePage` links to `/signup`, but router does not define `/signup`.
- `q-srfm/src/store/entities.ts` duplicates the family store (`defineStore('family')`) and appears unused; avoid introducing new references to it unless intentional cleanup is performed.

Validation assumptions:
- Some controllers enforce explicit validation (account types/categories, statement ID match, merge preconditions).
- Other controllers rely on service/DB failures for invalid IDs (e.g., many `Guid.Parse` paths).
- Do not silently broaden accepted enum/domain values without DB/schema changes.

## 7. Security Model
Trust boundaries:
- External client -> API boundary authenticated via Firebase ID token verification.
- API -> Supabase/Postgres trusted by connection string secret.
- API -> Brevo/Stripe/Plaid trusted via configured secrets.

Current enforcement model:
- Most sensitive controllers use `[AuthorizeFirebase]` and then perform resource-level checks (membership/owner).
- Family owner-only operations are enforced in `FamilyController` by comparing `family.OwnerUid` with authenticated `uid`.

Security assumptions/risks visible in code:
- Some endpoints perform weaker/no resource authorization beyond authentication (example: `BudgetController.GetBudget` does not verify caller has access to requested budget; `GoalController` accepts entityId without membership check).
- `AuthorizeFirebaseAttribute` parses email claim via JWT read without null guards; malformed token claim shape can throw.
- CORS origins are hardcoded allowlist in `Program.cs`.
- API currently runs with `UseAuthorization()` but without ASP.NET auth scheme middleware; endpoint security relies on custom filter attributes.

## 8. Concurrency & Side Effects
Concurrency model:
- Request handlers are async and mostly stateless.
- `BudgetService` uses `SemaphoreSlim` to memoize existence check for `budget_edit_history` table.
- `SupabaseLogger` serializes DB log writes with a static lock.

Transactional/side-effect expectations:
- Budget/category upsert in `SaveBudget` runs in DB transaction; transaction rows may be persisted afterward via separate calls.
- Batch transaction saves use `NpgsqlBatch` + transaction for write grouping.
- Carryover recalculation performs read of sequential budgets and writes updated carryover values in a transaction.
- Reconciliation updates imported and budget transactions in separate statements without explicit DB transaction wrapping; treat as eventually consistent within a request.
- Email sending in invite flow is compensating: pending invite is deleted if send fails.

Idempotency:
- Most writes are upsert-style by ID; callers often generate IDs client-side (UUID).
- Delete endpoints are generally safe to re-run but may emit different status codes.

## 9. Extension Guidelines for Agents
Adding new features:
- Backend: add endpoint in relevant controller, put business/data logic in matching service, keep SQL parameterized via `NpgsqlCommand`.
- Frontend: add method to `src/dataAccess.ts` first, then wire store/composable/page.
- Prefer existing family/entity scoping patterns (`family_id`, `entity_id`, membership checks).

UI/UX implementation governance:
- `docs/design_system.md` is the authoritative implementation standard for all frontend UI/UX work.
- For UI tasks, agents must follow markers in `docs/design_system.md` exactly:
  - `[ENFORCED]`: mandatory, no exceptions without explicit human approval.
  - `[SUGGESTED]`: preferred default; deviations must be intentional and justified in change notes.
  - `[ASSUMPTION/UNCLEAR]`: do not guess silently; call out uncertainty and request clarification when required to proceed safely.
- Reuse existing Quasar primitives and existing SRFM components before creating new UI components.
- Use tokenized styling only (Quasar theme tokens and app semantic CSS variables); avoid introducing new hard-coded visual values when a token exists.
- Keep responsive behavior aligned with the current Quasar breakpoint/grid patterns and existing layout shell behavior.
- Preserve accessibility minimums from `docs/design_system.md` (contrast, keyboard operation, dialog behavior, semantic labels, reduced motion support).
- When touching legacy inconsistent UI code, move changed areas toward the canonical direction defined in `docs/design_system.md` instead of extending legacy patterns.
- If a change requires a new visual token, component variant, or theming mode, update `docs/design_system.md` in the same change.

Mobile-first UI requirements:
- Mobile is a first-class target; the app is used on phones at least 50% of the time, primarily for on-the-go transaction entry. Users migrating from EveryDollar and Mint expect a polished, mobile-native experience.
- Any UI change that affects transaction entry, budget views, navigation, or account overview must be evaluated at 375–430px viewport widths before being considered complete.
- All interactive elements must meet 44×44px tap target minimums.
- Forms must use correct mobile input types (`number`, `date`, `tel`, etc.) and minimize required scrolling to submit.
- Prefer bottom sheets or full-screen modals over inline panels for forms opened on mobile.
- Do not rely solely on Quasar's default breakpoints without validating the result on phone-sized viewports.
- The sidebar drawer must NOT auto-open on page navigation at mobile breakpoints — this is a known critical bug. The bottom tab bar is the primary mobile nav.
- Data tables must use responsive patterns on mobile (card views, expandable rows, or scrollable containers with visible affordance). Simply truncating columns is a known violation.
- Base font must be at least 16px on mobile viewports to prevent iOS auto-zoom on input focus.
- The viewport meta must not include `user-scalable=no` or `maximum-scale=1` — these are WCAG 2.1 AA violations.

User experience and terminology rules:
- Accounting terminology (Reconciled, Cleared, Uncleared, Budget Register, Account Register) must include tooltips via `q-tooltip` on hover/tap. **Labels remain unchanged; tooltips bridge the gap.** See `docs/design_system.md` Section 9 for specific tooltip text.
- Empty states must include guidance and actionable next steps — never just "No data" or "No items."
- Destructive actions must always require confirmation with a dialog that names the affected item and states the impact. Destructive buttons must use `color="negative"`, never plain text links.
- Budget page unmatched-transaction trash icons perform a **soft delete** (restorable from "Deleted" tab). Restyle to communicate this: muted/outlined icon, tooltip explaining restore capability.
- All write operations must provide visible success feedback via `$q.notify`.
- Page titles must be semantic `h1` elements. Every page must have a proper heading hierarchy.
- The Transactions page subtitle must be removed entirely (no subtitle).
- The default post-login landing page is the **Budget page**, not the Dashboard. Update router accordingly.

Onboarding approach:
- The existing SetupWizardPage handles initial setup (family → entities → accounts) and should NOT be expanded with budget/goal steps.
- Instead, implement a **guided tour system**: auto-triggered contextual prompts for new users + a persistent "Getting Started" checklist accessible from sidebar/settings.
- The SetupWizardPage must be migrated from Vuetify-era CSS to Quasar-native styling.

Modifying existing logic:
- Preserve month-string conventions (`YYYY-MM`) and carryover semantics.
- Preserve category exclusion behavior for goal-bound categories unless intentionally redesigning goals.
- Keep transaction split guarantees (never persist split-less transaction).
- Keep optimistic store update behavior synchronized with API responses.

Adding dependencies:
- API favors direct SDK usage and lightweight services; avoid introducing heavy ORMs without clear migration plan.
- Frontend currently uses fetch in `dataAccess`; adding a second HTTP client abstraction is likely architectural drift.

Refactoring safely:
- Treat `src/dataAccess.ts` and API controller routes as a coupled contract surface.
- If changing route shapes, update all callers in `dataAccess`, pages, and composables in one change.
- Remove/merge duplicate store implementations (`store/entities.ts` vs `store/family.ts`) only as a dedicated cleanup with full import audit.

Naming conventions:
- Backend: `*Controller`, `*Service`, PascalCase DTO properties.
- Frontend: Pinia hooks `useXStore`, composables `useX`, TypeScript interfaces in `src/types.ts`.

Error handling conventions:
- API generally returns `BadRequest` for operation failures and logs details.
- Frontend throws on non-OK responses and surfaces messages through page-level handling.

## 10. Anti-Patterns to Avoid
- Bypassing `family_members`/owner checks for new family-scoped endpoints.
- Writing directly from UI components to `fetch` endpoints instead of extending `dataAccess`.
- Persisting large budget datasets to localStorage (already explicitly avoided).
- Mixing entity-scoped and non-entity-scoped data in one query without explicit `entity_id` logic.
- Introducing non-transactional multi-table writes in hot paths where existing code expects atomic behavior.
- Creating new parallel store implementations for the same domain (existing duplication is already a risk).
- Ignoring `docs/design_system.md` rules for frontend changes.
- Adding inline styles, direct hex values, or arbitrary spacing/radius values where tokens/components already cover the use case.
- Mixing Vuetify-era classes/APIs into Quasar components.
- **Extending the transfer-as-income pattern.** The current workaround of recording an income-like transaction in a destination category to represent a fund/goal draw is a known defect that causes category spending to net to zero in reports. Do not build new flows that replicate this pattern; a first-class transfer transaction type is the intended solution.
- Building UI that only works on desktop. Any new page, dialog, or form must be usable at 375px without horizontal scroll, broken layout, or inaccessible tap targets.
- Using `user-scalable=no` or `maximum-scale=1` in viewport meta (WCAG violation).
- Displaying empty states without actionable guidance or displaying error states while data is still loading.
- Styling destructive actions as plain text links or small unlabeled icons.
- Using placeholder-only form labels that disappear on focus.
- Setting base font below 16px on mobile viewports.
- Opening the sidebar drawer automatically on mobile page navigation.
- Adding accounting terminology without accompanying tooltips or plain-language help.

## 11. Testing Expectations
Current baseline:
- Frontend has Node-based TS tests in `q-srfm/tests/transactions.test.ts` covering duplicate/date-window helpers and budget month sorting.
- No meaningful backend automated tests are present in repository.

Minimum expectations for new changes:
- Add/update unit tests for pure logic in composables/utils (especially reconciliation matching, carryover math, sorting/filtering behavior).
- For API behavior changes, at minimum validate with targeted endpoint checks (manual/integration style) and include negative authorization cases.
- For contract changes, verify both API endpoint and `dataAccess` caller paths together.

Unclear from code:
- No established integration-test harness for API.
- No CI definition in repository indicating enforced test gates.

## 12. Change Risk Map
High-risk zones:
- `api/Services/BudgetService.cs`: central financial state mutations, carryover propagation, reconciliation, import updates.
- `api/Controllers/FamilyController.cs` + `api/Services/FamilyService.cs`: tenancy and ownership boundaries.
- `api/Filters/AuthorizeFirebaseAttribute.cs`: authentication context foundation for most protected routes.
- `q-srfm/src/dataAccess.ts`: single frontend API contract adapter; drift here breaks multiple pages.
- `q-srfm/src/pages/TransactionsPage.vue` and `q-srfm/src/components/TransactionRegistry.vue`: reconciliation workflows depend on many interrelated assumptions.

Migration-sensitive zones:
- Firestore/Supabase seam in `AuthController` and Firebase profile setup.
- Statement/account import paths with partial implementations (`NotImplementedException`).
- Optional-table feature flags (`budget_edit_history`, `goals_budget_categories`) that allow mixed schema states.
- `q-srfm/src/pages/SetupWizardPage.vue`: uses Vuetify-era CSS selectors and variables in a Quasar app; needs migration to Quasar-native styling before feature expansion.

Known UI/UX violations requiring remediation (from March 2026 review):
- Viewport meta `user-scalable=no` and `maximum-scale=1` — WCAG violation, one-line fix.
- Sidebar auto-open on mobile navigation — critical mobile UX bug in `MainLayout.vue`.
- 14px base font on mobile — needs 16px minimum for iOS compatibility.
- Missing heading hierarchy (dashboard H4 only, no H1) — across all pages.
- Data tables not responsive on mobile — Transactions and Accounts pages.
- Bottom tab bar missing Reports, Data Mgmt, Settings — mobile nav incomplete.
- No delete confirmation on Accounts — destructive action without safety gate.
- Data Mgmt loading race condition — error renders before fetch completes.

Cross-system impact contracts:
- DTO shapes in `api/Models/*` used directly by frontend TS types and data mapping.
- Route names and payload shapes in `BudgetController`, `FamilyController`, `AccountController`, `GoalController`.
