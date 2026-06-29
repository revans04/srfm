-- 20260624_email_verification_tokens_rls.sql
--
-- Secure public.email_verification_tokens against the Supabase/PostgREST anon path.
--
-- Context:
--   This table (user_id, token, created_at, updated_at) is created lazily at
--   runtime by api/Services/UserService.cs (EnsureEmailVerificationTokensTable)
--   and is read/written exclusively by the .NET API, which connects as the
--   privileged Supabase `postgres` role over the pooler. Nothing in the frontend
--   talks to Supabase directly. Supabase flagged the table as "exposed via API
--   without RLS" because it contains the sensitive `token` column.
--
-- Fix:
--   Enable RLS with NO policies, and revoke the public PostgREST grants. The
--   `postgres` role bypasses RLS, so the backend is unaffected; the anon /
--   authenticated PostgREST roles can no longer read the tokens.
--
-- Idempotent: safe to run repeatedly. Mirrors the DDL in UserService.cs so a
-- freshly-created table is secured the same way.

CREATE TABLE IF NOT EXISTS public.email_verification_tokens (
  user_id TEXT PRIMARY KEY,
  token TEXT NOT NULL UNIQUE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

ALTER TABLE public.email_verification_tokens ENABLE ROW LEVEL SECURITY;

REVOKE ALL ON public.email_verification_tokens FROM anon, authenticated;
