# SubTrackr — Shared Specification (v0.1)

This document is the **human-readable source of truth** for both apps:

- **Desktop** — WPF / C# / .NET 10 (`desktop/`)
- **Android** — Kotlin / Jetpack Compose (`android/`, later phase)

The two apps share **no code**. They stay in lockstep through three contracts:

1. **Data shape** — [`proto/subtrackr.proto`](proto/subtrackr.proto). Both apps
   code-generate their models from it. Nobody hand-writes the model twice.
2. **Behavior** — the golden vectors in [`vectors/`](vectors/). Both apps run
   them as tests; drift turns one side's suite red.
3. **This document** — prose the code and the vectors both trace back to.

> Rule: if behavior changes, the vectors change **first**, then both apps.

---

## 1. Domain model

See `subtrackr.proto` for exact fields. Key decisions:

- **Money** is integer `minor_units` + `exponent` (never a float). `5.12 EUR`
  is `{currency:"EUR", minor_units:512, exponent:2}`.
- **IDs** are UUID v4, generated once, stable across devices (sync depends on this).
- **Soft delete**: `deleted_at` non-empty means the record is a tombstone —
  hidden in the UI, kept for sync convergence.
- `updated_at` is bumped on every edit (last-writer-wins baseline for sync).

## 2. Monthly-equivalent normalization

Every subscription reduces to a **monthly-equivalent** amount in its *own*
currency. Compute in exact decimal (no binary float). Constant:
`AVG_DAYS_PER_MONTH = 30.436875` (= 365.2425 / 12).

| Billing cycle | Monthly equivalent |
|---|---|
| `WEEKLY`      | `cost * 52 / 12` |
| `MONTHLY`     | `cost` |
| `QUARTERLY`   | `cost / 3` |
| `SEMIANNUAL`  | `cost / 6` |
| `ANNUAL`      | `cost / 12` |
| `CUSTOM_DAYS(n)` | `cost * AVG_DAYS_PER_MONTH / n` |

**Yearly equivalent** = `monthly_equivalent * 12`.

`PAUSED` subscriptions are excluded from active-spend totals (but still listed).

## 3. Currency conversion

- Settings hold a **base currency**. Totals roll up into it.
- Conversion uses a rate table `rate(FROM -> base)`:
  `amount_base = amount_source * rate(source -> base)`.
- Rates come from the **Frankfurter API** (ECB data), cached locally with an
  offline fallback to the last-known table. Rate provenance/date is shown in UI.
- **Rounding**: keep full precision through every intermediate step. Round
  **only** the final displayed figure — half-up — to the currency's `exponent`
  (2 for most). Sum first, then round; never round per-item then sum.

## 4. Totals (dashboard)

- **Monthly active spend (base)** = Σ over `ACTIVE`, non-deleted subs of
  `convert(monthly_equivalent, base)`, rounded at the end.
- **Yearly active spend (base)** = monthly × 12.
- Also expose per-currency subtotals (before conversion) so the user sees the
  raw picture in each currency they actually pay in.

## 5. Worth-it / not-worth-it helper

For a sub with `uses_per_month > 0`:

- `cost_per_use = monthly_equivalent_base / uses_per_month` (round display to 4 dp).
- Verdict vs a threshold `T` (global default, overridable per sub):
  - `cost_per_use <= T`  → **WORTH**
  - `cost_per_use >  T`  → **NOT_WORTH**
  - `uses_per_month == 0` → **UNKNOWN** (not enough data)

Default `T` is configurable in settings; v0.1 default = `2.00` in base currency.

## 6. "What-if" calculator

A non-persisted, in-memory scenario: clone current active subs, add/remove/edit
hypothetical entries, and recompute §4 totals + the delta vs the real total.
Uses the exact same normalization + conversion functions — no separate math.

## 7. Versioning

- `schema_version` in `Database`/`Settings` gates migrations.
- App releases follow **SemVer**; see repo README for the release pipeline.

---

*v0.1 — foundation. Expect fields to be added (never renumbered) as we go.*
