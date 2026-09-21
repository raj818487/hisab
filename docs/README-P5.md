# Milk Hisab — P5 Finishing Pack

Hardening + demo seed + UI polish + launch docs for the Angular 20 PWA + .NET API + SQLite product.

**Hard rule:** No payment gateway. Manual payments with receipt view / HTML download / Print only.

## What’s in this pack

| Path | Purpose |
|------|---------|
| `backend/Data/DbSeeder.cs` | Idempotent seed: 3 customers, ~10 daily take/skip entries, 2 manual payments |
| `backend/Program.SeedSnippet.cs.txt` | Exact hook after EnsureCreated/Migrate |
| `backend/Controllers/HealthController.cs` | `/health` + `/api/v1/health` for smoke |
| `backend/Services/ReceiptService.cs` | Stable `MH-yyyyMMdd-####` receipt numbers + HTML |
| `backend/Services/PaymentService.cs` | Uses ReceiptService for numbers |
| `backend/Services/HisabService.cs` | Due = Delivered − Paid |
| `frontend/...` | Dashboard (today + month due), receipt Download/Print, empty states, `provideHttpClient` |
| `scripts/smoke-api.ps1` | Health + customers against localhost:8080 |
| `docs/LAUNCH-CHECKLIST.md` | DoD checklist |
| `docs/RUNBOOK.md` | How to run API + UI |
| `APPLY.ps1` | Copy into `D:\Other Project\Milk-Hisab` (+ optional Program.cs patch) |
| `FULL-GAP-FILL.md` | Critical files that must exist for DoD |

## Apply (Windows)

```powershell
# Copy this pack onto the Windows machine, then:
cd <p5-pack>
.\APPLY.ps1 -PatchProgram
```

Then follow `docs/RUNBOOK.md`.

## Verify

```powershell
.\scripts\smoke-api.ps1
```

Walk `docs/LAUNCH-CHECKLIST.md`.

## Parent agent notes

- Box path: `/workspace/milk-hisab/p5-pack/`
- Target Windows root: `D:\Other Project\Milk-Hisab`
- MachineId (parent): `8710bde2-c4c4-4ebf-aa4d-99f99dfca21f`
