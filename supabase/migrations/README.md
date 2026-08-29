# Database migrations (Supabase / Postgres)

Numbered SQL migrations for the sync backend, applied **in order** (`001`, `002`, …).
Each file is **idempotent** (safe to re-run) and, applied in sequence on a fresh project,
produces the current schema.

## How to apply

- **Supabase dashboard:** SQL Editor → **New query** → paste each file **in numeric order** → Run.
  On a brand-new project, run them all `001 → NNN`. On an existing project, run only the new ones.
- Keep note of the highest migration you've applied per project.

> If you later adopt the Supabase CLI, it expects `<timestamp>_name.sql` filenames; these
> sequential `NNN_name.sql` files are for the paste-into-SQL-editor workflow this project uses.

## Adding a migration

Create the next number (`002_…​.sql`), keep it idempotent, and describe it below. Never edit an
already-applied migration — add a new one.

| File | What it does |
|---|---|
| `001_init.sql` | `subscriptions` table (typed row per subscription) + anon RLS policies; drops the old `subtrackr_docs` blob table |
