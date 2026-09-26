# Supabase

The sync backend: a Postgres table behind Supabase's REST API, with Supabase Auth for sign-in.
[docs/SYNC-SETUP.md](../docs/SYNC-SETUP.md) explains how to set up a project and run it locally.

| Path | Holds |
| --- | --- |
| `migrations/` | Numbered schema migrations, applied in order |
| `migrations.lock.json` | SHA-256 of every migration; an applied file never changes |
| `migration-tests/` | Fixtures for each migration's isolated test (`NNNN_before.sql`, `NNNN_after.sql`) |
| `tests/database/` | pgTAP tests for row security, run by `npx supabase test db` |
| `templates/` | The sign-in code email |
| `config.toml` | Local stack settings (ports 546xx, email codes) |

## Migrations

| File | What it does |
| --- | --- |
| `0001_init.sql` | The `subscriptions` table with one typed row per subscription, anonymous policies, and removal of the old `subtrackr_docs` table |
| `0002_per_user_rows.sql` | Rows belong to a signed-in user: `user_id`, primary key `(user_id, id)`, own-rows policies, no anonymous access. Older anonymous rows move to `subscriptions_legacy_0002` |

`0001_init.sql` was called `001_init.sql` until 0.2.2. Its content never changed; projects that
applied it by hand do not track file names, so the rename changes nothing for them.

## Adding a migration

1. Create the next number, `NNNN_description.sql`, and make it safe to run twice.
2. Add `migration-tests/NNNN_before.sql` (representative rows at `NNNN-1`) and `NNNN_after.sql`
   (checks that raise an exception on failure).
3. Add or extend a pgTAP file in `tests/database/` for new policies.
4. `python tools/supabase_migrations.py lock`, then `python tools/supabase_migrations.py test`.

Never edit, rename, or delete a migration after it has been applied anywhere.
