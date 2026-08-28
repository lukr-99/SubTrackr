# Sync setup (Supabase)

SubTrackr syncs your **subscriptions** across devices through a free Supabase project.
Base currency and other settings stay per-device (they're viewing preferences). The merge is
last-writer-wins per subscription — see [SPEC §8](../contracts/SPEC.md#8-sync).

## 1. Create the project
1. Sign up at <https://supabase.com> (free tier is plenty).
2. **New project** → name it, set a strong DB password → wait for it to provision (~2 min).

## 2. Create the table
Open **SQL Editor → New query**, paste this, and **Run**:

```sql
create table if not exists subscriptions (
  id             uuid primary key,
  name           text    not null default '',
  cost_currency  text    not null default 'EUR',
  cost_minor     bigint  not null default 0,
  cost_exponent  int     not null default 2,
  billing_cycle  text    not null default 'MONTHLY',
  custom_days    int     not null default 0,
  next_renewal   text    not null default '',
  category       text    not null default '',
  icon_ref       text    not null default '',
  auto_pay       boolean not null default false,
  status         text    not null default 'ACTIVE',
  uses_per_month double precision not null default 0,
  worth_mode     text    not null default 'AUTO',
  trial_end      text    not null default '',
  website        text    not null default '',
  notes          text    not null default '',
  created_at     text    not null default '',
  updated_at     text    not null default '',
  deleted_at     text    not null default ''
);
alter table subscriptions enable row level security;
create policy "anon read"   on subscriptions for select to anon using (true);
create policy "anon insert" on subscriptions for insert to anon with check (true);
create policy "anon update" on subscriptions for update to anon using (true) with check (true);

-- Removes the single-JSON-blob store from the first sync version (safe if it never existed).
drop table if exists subtrackr_docs;
```

Each subscription is a real row (typed columns) — queryable, per-row policies, and ready for
real-time later. Conflict handling stays last-writer-wins per row via `updated_at`; deletes are
soft (a `deleted_at` timestamp), so they propagate to your other device on the next sync.

## 3. Get your keys
**Project Settings → API**:
- **Project URL** — e.g. `https://abcdxyz.supabase.co`
- **anon public** key (a long JWT string)

## 4. Connect the apps
In **Settings → Sync** on the desktop app *and* on Android, paste the Project URL + anon key,
then tap **Sync now**. Run it on both — each device pulls, merges, and pushes, so within two
syncs both hold the same data.

## Security note
The anon key plus a permissive policy means anyone with **both** your URL and anon key can
read/write your data. Keep them private — they live only inside your own app installs. For a
personal tracker that's an acceptable trade; we can layer on per-device auth later if you want
stronger protection.
