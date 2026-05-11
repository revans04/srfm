-- ============================================================================
-- Migration: Add `notes` to goals.
--
-- Background. Users want narrative context on a savings goal — which vendor,
-- which trip, which school — beyond what fits in the goal name. The goal
-- edit dialog already had a plain-text textarea bound to `notes`, but the
-- field was silently dropped on save because neither the API model nor the
-- DB had a column for it. This migration closes that loop and stores HTML
-- emitted by Quasar's q-editor on the frontend (sanitized at render time
-- via DOMPurify in GoalDetailsPanel).
--
-- TEXT (not VARCHAR) because rich-text content has no natural length limit
-- and Postgres TEXT has no performance penalty vs VARCHAR. Nullable so
-- existing rows stay untouched and "no notes" is distinct from an empty
-- string written by the editor.
--
-- Safe to re-run (IF NOT EXISTS guard).
-- ============================================================================

BEGIN;

ALTER TABLE goals
  ADD COLUMN IF NOT EXISTS notes TEXT;

COMMIT;
