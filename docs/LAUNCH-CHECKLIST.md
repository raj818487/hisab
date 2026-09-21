# Milk Hisab — Launch Checklist (P5 DoD)

**Product:** Milk Hisab PWA (Angular 20 + .NET API + SQLite)  
**Constraint:** **No payment gateway** — manual payments + receipt view / HTML download only.  
**Timezone note:** Operator times reported in Asia/Calcutta (IST).

## Definition of Done

A fresh machine can:

1. Run API + UI
2. Install PWA (manifest)
3. Complete one full month hisab flow (customers → daily take/skip → payment → receipt → due updates)

---

## Pre-flight

- [ ] P1–P4 applied on `D:\Other Project\Milk-Hisab`
- [ ] P5 pack applied via `APPLY.ps1`
- [ ] `DbSeeder` hooked after `EnsureCreated` / `Migrate` (see `backend/Program.SeedSnippet.cs.txt`)
- [ ] No Razorpay / Stripe / UPI-collect NuGet or npm packages in solution
- [ ] `provideHttpClient()` present in `app.config.ts`

## Database & seed

- [ ] Fresh DB creates cleanly (`EnsureCreated` or migrations)
- [ ] Seed creates **3 sample customers** (idempotent — skip if customers exist)
- [ ] Seed creates **~10 daily entries** this month (mix taken + skip)
- [ ] Seed creates **1–2 manual payments** with receipt numbers `MH-yyyyMMdd-####`

## Feature acceptance

- [ ] Customer create / edit works
- [ ] Daily **Taken** (qty &gt; 0) and **Skip** (qty 0) persist
- [ ] Month hisab: **Due = Delivered − Paid**
- [ ] Recording a payment reduces due and opens receipt
- [ ] Receipt page shows **Download HTML** and **Print** buttons
- [ ] Receipt HTML downloads and prints / Save as PDF via browser
- [ ] Dashboard shows **today** taken/skip/pending counts + **month due**
- [ ] Empty states appear when no customers / payments

## Smoke & run

- [ ] API listens on `http://localhost:8080`
- [ ] `scripts/smoke-api.ps1` PASS (health + customers)
- [ ] UI on `http://localhost:4200` with CORS OK
- [ ] README / RUNBOOK steps verified on a clean shell

## PWA

- [ ] `manifest.webmanifest` present and linked
- [ ] Installable on desktop Chrome (and phone over HTTPS / localhost)
- [ ] App survives refresh; SQLite data persists

## Docs

- [ ] `docs/RUNBOOK.md` followed once end-to-end
- [ ] Demo data ready for showcase
- [ ] BRD delta honored: manual payments only

## Explicit non-goals (do not block launch)

- Online payment collection / gateways
- Multi-user auth (stretch)
- Deep offline sync queue (stretch)
