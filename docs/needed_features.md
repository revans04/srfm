# SRFM Needed Features (New / Requested)

## 1) Security and Access Gaps
- Enforce resource-level authorization consistently on all budget and goal endpoints (membership checks for every `budgetId` / `entityId`).
- Add null-safe claim handling and hardened error paths in auth filter/token parsing.
- Standardize owner/admin checks for high-impact admin operations.

## 2) Reconciliation and Statements Completion
- Implement missing statement write flows end-to-end:
  - Finalize statement endpoint support from frontend contract.
  - Statement delete and unreconcile behavior with full DB updates.
- Replace current placeholder or partial service paths with complete transactional logic.
- Guarantee atomic reconcile operations where multiple tables/statuses are updated.

## 3) API/Frontend Contract Alignment
- Resolve route and shape drift between controllers and `q-srfm/src/dataAccess.ts`.
- Align invite acceptance navigation path with existing frontend routes.
- Normalize endpoint naming conventions (`/api/budget` vs `/api/budgets`) and DTO field casing.

## 4) Merchant Domain Completion
- Implement first-class merchant APIs and persistence where currently implied by docs/UX but not fully realized.
- Add merge/rename side effects that correctly update linked transactions.
- Add usage-count maintenance jobs/logic for ranking merchants in UI.

## 5) Reporting Backend Maturity
- Add/complete report endpoints for Monthly Overview, Register, YoY, and Net Worth.
- Move heavy report calculations to server-side aggregation with performance-aware queries.
- Add consistency checks between dashboard tiles and report totals.

## 6) Import/Export Hardening
- Complete validation-first import commit pipeline for each import type.
- Add deterministic duplicate-handling policies (merge/replace/skip) and explicit user-visible outcomes.
- Improve long-running import progress feedback and retry/error recovery.
- Ensure export bundles are complete and versioned for re-import compatibility.

## 7) Family and Entity Administration Enhancements
- Add explicit ownership transfer workflow with audit logging.
- Add safer entity deletion options (archive/reassign/delete with clear dependency handling).
- Improve member-role management granularity beyond simple owner/member flows where needed.

## 8) Budget and Transaction Advanced Capabilities
- Expand deterministic budget merge conflict handling (categories, transactions, fund balances).
- Add stronger recurring-transaction controls in budget duplication flows.
- Improve audit coverage for batch transaction operations and admin-triggered maintenance actions.

## 9) Test Coverage and Quality Gates
- Add backend automated tests for authorization boundaries and critical financial mutations.
- Add integration tests for budget save/merge/carryover and reconciliation workflows.
- Add API + `dataAccess.ts` contract tests to prevent route/payload drift regressions.
- Extend frontend tests for reconciliation matching, carryover math, and month ordering behavior.

## 10) Legacy/Documentation Cleanup Follow-ups
- Remove legacy or duplicate store patterns that risk split state ownership.
- Keep a single living contract reference for routes and DTOs to prevent future divergence.
- Track migration completion of mixed Firestore/Supabase responsibilities in auth-related flows.
