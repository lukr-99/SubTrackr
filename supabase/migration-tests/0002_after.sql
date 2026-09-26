-- Checks after 0002 ran on the 0002_before.sql state.
do $checks$
declare
  legacy_count int;
  kept_notes text;
  kept_deleted text;
begin
  select count(*) into legacy_count from public.subscriptions_legacy_0002;
  if legacy_count <> 3 then
    raise exception 'expected 3 legacy rows, found %', legacy_count;
  end if;

  select notes into kept_notes from public.subscriptions_legacy_0002
  where id = '33333333-3333-4333-8333-333333333333';
  if kept_notes is distinct from 'yearly' then
    raise exception 'legacy row lost its data: notes = %', kept_notes;
  end if;

  select deleted_at into kept_deleted from public.subscriptions_legacy_0002
  where id = '44444444-4444-4444-8444-444444444444';
  if kept_deleted is distinct from '2026-09-03T09:00:00Z' then
    raise exception 'legacy tombstone lost deleted_at: %', kept_deleted;
  end if;

  if exists (select 1 from public.subscriptions) then
    raise exception 'ownerless rows must leave public.subscriptions';
  end if;

  if not exists (
    select 1 from information_schema.columns
    where table_schema = 'public' and table_name = 'subscriptions'
      and column_name = 'user_id' and is_nullable = 'NO'
  ) then
    raise exception 'subscriptions.user_id must exist and be not null';
  end if;

  if (
    select array_agg(a.attname::text order by k.ordinality)
    from pg_constraint c
    cross join lateral unnest(c.conkey) with ordinality as k(attnum, ordinality)
    join pg_attribute a on a.attrelid = c.conrelid and a.attnum = k.attnum
    where c.conrelid = 'public.subscriptions'::regclass and c.contype = 'p'
  ) is distinct from array['user_id', 'id'] then
    raise exception 'primary key must be (user_id, id)';
  end if;

  if exists (
    select 1 from pg_policies
    where schemaname = 'public' and tablename = 'subscriptions' and 'anon' = any (roles)
  ) then
    raise exception 'no policy may target anon';
  end if;

  if has_table_privilege('anon', 'public.subscriptions', 'select')
     or has_table_privilege('anon', 'public.subscriptions', 'insert')
     or has_table_privilege('anon', 'public.subscriptions', 'update') then
    raise exception 'anon must have no privileges on subscriptions';
  end if;

  if has_table_privilege('authenticated', 'public.subscriptions', 'delete') then
    raise exception 'clients never delete rows; sync uses tombstones';
  end if;

  if has_table_privilege('anon', 'public.subscriptions_legacy_0002', 'select')
     or has_table_privilege('authenticated', 'public.subscriptions_legacy_0002', 'select') then
    raise exception 'the legacy table must be unreadable through the API';
  end if;

  if not (select relrowsecurity from pg_class where oid = 'public.subscriptions_legacy_0002'::regclass) then
    raise exception 'row security must be on for the legacy table';
  end if;
end
$checks$;
