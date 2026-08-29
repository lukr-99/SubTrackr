-- 001_init.sql — initial SubTrackr sync schema (relational subscriptions table).
-- Idempotent: safe to re-run (IF NOT EXISTS / DROP POLICY IF EXISTS).

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

drop policy if exists "anon read"   on subscriptions;
drop policy if exists "anon insert" on subscriptions;
drop policy if exists "anon update" on subscriptions;
create policy "anon read"   on subscriptions for select to anon using (true);
create policy "anon insert" on subscriptions for insert to anon with check (true);
create policy "anon update" on subscriptions for update to anon using (true) with check (true);

-- Retire the single-JSON-blob store from the pre-relational version.
drop table if exists subtrackr_docs;
