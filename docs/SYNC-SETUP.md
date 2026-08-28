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
create table if not exists subtrackr_docs (
  id text primary key,
  subscriptions jsonb not null default '[]'::jsonb
);
alter table subtrackr_docs enable row level security;
create policy "anon read"   on subtrackr_docs for select to anon using (true);
create policy "anon insert" on subtrackr_docs for insert to anon with check (true);
create policy "anon update" on subtrackr_docs for update to anon using (true) with check (true);
insert into subtrackr_docs (id) values ('main') on conflict (id) do nothing;
```

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
