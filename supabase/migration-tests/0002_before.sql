-- State before 0002: rows written by the anonymous role under 0001, including a fixed seed ID,
-- a paused custom-cycle record, and a tombstone.
insert into public.subscriptions (
  id, name, cost_currency, cost_minor, cost_exponent, billing_cycle, custom_days, next_renewal,
  category, icon_ref, auto_pay, status, uses_per_month, worth_mode, trial_end, website, notes,
  created_at, updated_at, deleted_at
) values
  ('eb90294c-78d8-40d1-83a3-0596708b1797', 'Netflix', 'CZK', 19900, 2, 'MONTHLY', 0, '2026-10-04',
   'Entertainment', '', true, 'ACTIVE', 8, 'AUTO', '', 'netflix.com', '',
   '2026-08-28T10:00:00Z', '2026-08-29T11:30:00Z', ''),
  ('33333333-3333-4333-8333-333333333333', 'Transit pass', 'CZK', 240000, 2, 'CUSTOM_DAYS', 45,
   '2026-11-10', 'Transport', '', false, 'PAUSED', 12.5, 'NOT_WORTH', '', '', 'yearly',
   '2026-08-28T10:00:00Z', '2026-09-02T09:00:00Z', ''),
  ('44444444-4444-4444-8444-444444444444', 'Old gym', 'EUR', 2999, 2, 'MONTHLY', 0, '',
   'Health', '', false, 'ACTIVE', 0, 'AUTO', '', '', '',
   '2026-08-28T10:00:00Z', '2026-09-03T09:00:00Z', '2026-09-03T09:00:00Z');
