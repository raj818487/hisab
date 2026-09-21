# Milk Hisab — Runbook (API + UI)

## Prerequisites

- .NET SDK 8+ / 9+ / 10 (project targets .NET 10 API)
- Node.js 20+ and Angular CLI (`npm i -g @angular/cli` if needed)
- Windows path used in production work: `D:\Other Project\Milk-Hisab`

## 1. Apply P5 pack (once)

```powershell
cd <path-to-p5-pack>   # e.g. copied from box /workspace/milk-hisab/p5-pack
.\APPLY.ps1
# Optional: .\APPLY.ps1 -PatchProgram
```

`-PatchProgram` idempotently inserts `DbSeeder.SeedAsync` after `EnsureCreated`/`Migrate`.

## 2. Run API

```powershell
cd "D:\Other Project\Milk-Hisab\backend"   # or wherever MilkHisab.Api.csproj lives
dotnet restore
dotnet run --urls http://localhost:8080
```

- Swagger (Development): `http://localhost:8080/swagger`
- Health: `http://localhost:8080/health` and `http://localhost:8080/api/v1/health`
- Customers: `http://localhost:8080/api/v1/customers`

SQLite file is created next to the API (`milk_hisab.db` by default). First run seeds demo data if the customers table is empty.

## 3. Run UI

```powershell
cd "D:\Other Project\Milk-Hisab\frontend"   # or client / MilkHisab.Web
npm install
ng serve
# open http://localhost:4200
```

Ensure `environment*.ts` has:

```ts
apiBaseUrl: 'http://localhost:8080/api/v1'
```

CORS policy `AngularDev` allows `http://localhost:4200`.

## 4. Smoke test

```powershell
cd <p5-pack>
.\scripts\smoke-api.ps1
# or after apply:
.\scripts\smoke-api.ps1   # from target if copied
```

## 5. Happy-path demo (manual)

1. Dashboard → confirm today counts + month due  
2. Daily → mark Taken / Skip for a customer  
3. Hisab → verify delivered / paid / due  
4. Payments → record cash/UPI (label only) → lands on Receipt  
5. Receipt → **Download HTML** + **Print**

## 6. Backup (SQLite)

Stop the API, copy `milk_hisab.db` (and `-wal`/`-shm` if present) to a safe folder. Restore by replacing the file before starting the API.

## Troubleshooting

| Symptom | Fix |
|--------|-----|
| CORS errors in browser | Confirm API CORS origin `http://localhost:4200` and UI uses that port |
| Empty dashboard / API errors | API not running or wrong `apiBaseUrl` |
| No seed data | Delete DB and restart, or ensure seeder hook is present |
| Duplicate receipt numbers | Unique index on `ReceiptNumber`; use `ReceiptService.NextReceiptNumberAsync` |
| Build fails on PaymentService ctor | Register both services; PaymentService needs `IReceiptService` |

## Payments policy reminder

**No payment gateway.** Mode `upi` means “received via UPI offline and recorded here,” not collect-via-SDK.
