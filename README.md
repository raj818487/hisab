# Milk Hisab PWA

Daily milk take/skip + monthly hisab + **manual** payments with **receipt download**.  
No payment gateway.

## Run

```powershell
# API (SQLite, seeds demo data on first run)
cd "D:\Other Project\Milk-Hisab\backend"
dotnet run --urls http://localhost:8080

# UI
cd "D:\Other Project\Milk-Hisab\frontend"
npm start
```

- UI: http://localhost:4200  
- API/Swagger: http://localhost:8080/swagger  
- Health: http://localhost:8080/api/v1/health  

## Features
- Customers CRUD
- Daily board (qty/rate/note)
- Monthly hisab (delivered / paid / due)
- Record payment (cash / UPI label / other) → receipt view + HTML download / print
- PWA installable shell

## Docs
- `docs/REQUIREMENTS-UPDATE.md` — no gateway; receipts required
- `docs/BRD-REQUIREMENT-DELTA.md`
- `docs/LAUNCH-CHECKLIST.md`
- `docs/RUNBOOK.md`
- `PLAN-5Day-Milk-Hisab.html` / `BRD-Milk-Hisab-PWA.html`

## Smoke
```powershell
.\scripts\smoke-api.ps1
```

## Personal expenses and product hisab

The default home is now **My expenses**. A person can maintain their own daily expenses and product accounts without creating a customer or requiring a supplier to join. Existing milk business screens remain accessible.

See [the personal hisab guide](docs/PERSONAL-HISAB.md) for usage, data upgrade behavior, verification commands and the distinction between private personal accounts and the legacy shared business ledger.
