-- Row security on public.subscriptions after 0002: each user sees and changes only their rows.
begin;
create extension if not exists pgtap with schema extensions;
select plan(14);

insert into auth.users (id, instance_id, aud, role, email)
values
  ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', '00000000-0000-0000-0000-000000000000',
   'authenticated', 'authenticated', 'first@example.test'),
  ('bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb', '00000000-0000-0000-0000-000000000000',
   'authenticated', 'authenticated', 'second@example.test');

-- The first user, pushing a first-run sample row with its fixed ID.
set local role authenticated;
select set_config('request.jwt.claims',
  '{"sub": "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa", "role": "authenticated"}', true);

select lives_ok(
  $$ insert into public.subscriptions (user_id, id, name, updated_at)
     values ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', 'eb90294c-78d8-40d1-83a3-0596708b1797',
             'Netflix', '2026-09-01T10:00:00Z') $$,
  'a user can insert their own row');

select lives_ok(
  $$ insert into public.subscriptions (id, name, updated_at)
     values ('55555555-5555-4555-8555-555555555555', 'Default owner', '2026-09-01T10:00:00Z') $$,
  'user_id defaults to the signed-in user');

select lives_ok(
  $$ insert into public.subscriptions (user_id, id, name, updated_at)
     values ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', 'eb90294c-78d8-40d1-83a3-0596708b1797',
             'Netflix renamed', '2026-09-02T10:00:00Z')
     on conflict (user_id, id) do update
       set name = excluded.name, updated_at = excluded.updated_at $$,
  'the sync upsert on (user_id, id) updates an existing row');

select is(
  (select name from public.subscriptions where id = 'eb90294c-78d8-40d1-83a3-0596708b1797'),
  'Netflix renamed',
  'the upsert changed the row');

select throws_ok(
  $$ insert into public.subscriptions (user_id, id, name, updated_at)
     values ('bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb', '66666666-6666-4666-8666-666666666666',
             'Planted', '2026-09-01T10:00:00Z') $$,
  '42501', null, 'a user cannot write a row for someone else');

select throws_ok(
  $$ delete from public.subscriptions where id = 'eb90294c-78d8-40d1-83a3-0596708b1797' $$,
  '42501', null, 'clients cannot delete rows');

-- The second user, with the same fixed sample ID.
select set_config('request.jwt.claims',
  '{"sub": "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb", "role": "authenticated"}', true);

select is((select count(*)::int from public.subscriptions), 0, 'a user sees none of another user''s rows');

select lives_ok(
  $$ insert into public.subscriptions (user_id, id, name, updated_at)
     values ('bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb', 'eb90294c-78d8-40d1-83a3-0596708b1797',
             'Netflix', '2026-09-01T10:00:00Z') $$,
  'two users can hold the same sample ID');

update public.subscriptions set name = 'Hijacked'
where user_id = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa';

select is((select count(*)::int from public.subscriptions), 1, 'the second user sees exactly their row');

select throws_ok(
  $$ select * from public.subscriptions_legacy_0002 $$,
  '42501', null, 'signed-in users cannot read the legacy rows');

-- Back to the test runner to inspect what really happened.
reset role;

select is(
  (select name from public.subscriptions
   where user_id = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa'
     and id = 'eb90294c-78d8-40d1-83a3-0596708b1797'),
  'Netflix renamed',
  'the second user could not change the first user''s row');

select is(
  (select user_id::text from public.subscriptions where id = '55555555-5555-4555-8555-555555555555'),
  'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
  'the defaulted user_id is the caller');

-- An anonymous caller holding only the publishable key.
set local role anon;
select set_config('request.jwt.claims', '{"role": "anon"}', true);

select throws_ok(
  $$ select * from public.subscriptions $$,
  '42501', null, 'the anonymous role cannot read subscriptions');

select throws_ok(
  $$ insert into public.subscriptions (user_id, id, name, updated_at)
     values ('aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', '77777777-7777-4777-8777-777777777777',
             'Anon', '2026-09-01T10:00:00Z') $$,
  '42501', null, 'the anonymous role cannot write subscriptions');

select * from finish();
rollback;
