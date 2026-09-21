# Milk Hisab P1 Pack

Drop-in backend + frontend + docs for **Milk Hisab** (manual payments, receipt download, no gateway).

## Contents

- `backend/` — ASP.NET Core API (`MilkHisab.Api`, SQLite)
- `frontend/src/app/` — Angular standalone features + routes + teal shell
- `docs/` — REQUIREMENTS-UPDATE.md, BRD-REQUIREMENT-DELTA.md
- `APPLY.ps1` — copy into `D:\Other Project\Milk-Hisab`, add EF Sqlite packages, build

## Parent / machine apply

On Windows machine `8710bde2-c4c4-4ebf-aa4d-99f99dfca21f`, after CopyFromBox of this pack:

```powershell
cd "D:\Other Project\Milk-Hisab"
# If pack landed as p1-pack subfolder:
.\p1-pack\APPLY.ps1
# Or if APPLY.ps1 is already at repo root:
.\APPLY.ps1
```

Then run API (port 8080) and `ng serve` (4200).
