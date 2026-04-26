-- ============================================================================
-- Migration: Persist entity-level templateBudget and taxFormIds.
--
-- Background. The frontend `EntityForm.vue` lets the user assemble a
-- `templateBudget.categories` list and pick `taxFormIds` per entity, but
-- `FamilyService.CreateEntity` / `UpdateEntity` / `GetFamilyById` only
-- round-trip `(id, name, type, created_at, updated_at)`. Both fields were
-- silently dropped on save and absent on read — see CLAUDE.md "Risk #1" and
-- AGENTS.md change risk map.
--
-- This migration adds two nullable columns so the existing API surface (which
-- already accepts the fields in JSON) starts persisting them. Data shapes:
--   * template_budget JSONB matches the frontend `TemplateBudget` interface:
--       { "categories": [{ "name", "groupName", "target", "isFund" }, ...] }
--   * tax_form_ids TEXT[] holds opaque form IDs (e.g. "form_1040",
--     "schedule_e", "ca_form_540"). Default '{}' so callers never have to
--     differentiate NULL from empty.
--
-- Safe to re-run (IF NOT EXISTS guards). No data destruction.
-- ============================================================================

BEGIN;

ALTER TABLE entities
  ADD COLUMN IF NOT EXISTS template_budget JSONB,
  ADD COLUMN IF NOT EXISTS tax_form_ids    TEXT[] NOT NULL DEFAULT '{}';

-- Backfill any rows that pre-existed the column with `NOT NULL DEFAULT`
-- (Postgres applies DEFAULT to existing rows on ADD COLUMN, but be explicit
-- so re-runs against a partially-applied state are idempotent).
UPDATE entities
   SET tax_form_ids = '{}'::TEXT[]
 WHERE tax_form_ids IS NULL;

-- Optional GIN index for future filtering by tax form. Cheap; drop later if
-- no report ever materializes that filter.
CREATE INDEX IF NOT EXISTS idx_entities_tax_form_ids
    ON entities USING GIN (tax_form_ids);

COMMIT;
