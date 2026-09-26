-- 0002_per_user_rows.sql: every subscription row belongs to one signed-in Supabase Auth user.
--
-- 0001 let the anonymous role read and write every row, so anyone holding the project URL and the
-- publishable key could see all data. From here on:
--   * rows carry user_id and the primary key is (user_id, id), because the first-run sample rows use
--     the same fixed IDs on every install;
--   * row-level security limits select, insert, and update to user_id = auth.uid();
--   * the anon role loses every privilege on the table. Nobody deletes rows (sync uses tombstones).
--
-- Rows written anonymously before this migration have no owner. They move to
-- subscriptions_legacy_0002, which no API role can read, instead of being deleted. Every device
-- keeps a full local copy and uploads it again after signing in; drop the legacy table once all
-- devices have synced.
--
-- The whole change is one DO block: it applies atomically and a second run does nothing.

do $migration$
begin
  if exists (
    select 1 from information_schema.columns
    where table_schema = 'public' and table_name = 'subscriptions' and column_name = 'user_id'
  ) then
    return;
  end if;

  create table public.subscriptions_legacy_0002 (like public.subscriptions including all);
  insert into public.subscriptions_legacy_0002 select * from public.subscriptions;
  alter table public.subscriptions_legacy_0002 enable row level security;
  revoke all on public.subscriptions_legacy_0002 from anon, authenticated;

  delete from public.subscriptions;

  drop policy if exists "anon read" on public.subscriptions;
  drop policy if exists "anon insert" on public.subscriptions;
  drop policy if exists "anon update" on public.subscriptions;
  revoke all on public.subscriptions from anon;
  revoke all on public.subscriptions from authenticated;
  grant select, insert, update on public.subscriptions to authenticated;

  alter table public.subscriptions
    add column user_id uuid not null default auth.uid() references auth.users (id) on delete cascade;
  alter table public.subscriptions drop constraint subscriptions_pkey;
  alter table public.subscriptions add constraint subscriptions_pkey primary key (user_id, id);

  create policy "own rows: read" on public.subscriptions
    for select to authenticated
    using ((select auth.uid()) = user_id);
  create policy "own rows: insert" on public.subscriptions
    for insert to authenticated
    with check ((select auth.uid()) = user_id);
  create policy "own rows: update" on public.subscriptions
    for update to authenticated
    using ((select auth.uid()) = user_id)
    with check ((select auth.uid()) = user_id);
end
$migration$;
