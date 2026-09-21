# Parent: apply premium UI polish on Windows

**Executor subagent cannot target `Shell.machineId=8710bde2-c4c4-4ebf-aa4d-99f99dfca21f`**
(all Shell calls run on the box). Please apply with machineId yourself.

## Pack location (box)
`/workspace/milk-hisab/ui-polish-pack/`

## Steps

### 1) CopyFromBox → Windows Temp
Copy the whole pack folder to:
`C:\Users\raj.parmar\AppData\Local\Temp\milk-hisab-plan\ui-polish-pack\`

(Or CopyFromBox each file preserving structure.)

### 2) Shell(machineId=8710bde2-c4c4-4ebf-aa4d-99f99dfca21f)
```powershell
$src = 'C:\Users\raj.parmar\AppData\Local\Temp\milk-hisab-plan\ui-polish-pack'
$dst = 'D:\Other Project\Milk-Hisab\ui-polish-pack'
if (Test-Path $src) {
  New-Item -ItemType Directory -Force -Path $dst | Out-Null
  Copy-Item "$src\*" $dst -Recurse -Force
}
powershell -NoProfile -ExecutionPolicy Bypass -File 'D:\Other Project\Milk-Hisab\ui-polish-pack\APPLY.ps1'
```

Notes:
- Does **not** commit / push / discard git changes.
- Keeps existing `app.html` if it already has `sidebar` + `brand-mark`; pass `-UpdateHtml` to force pack shell.
- Softens leftover teal hex in feature SCSS toward `--mh-*`.
- Runs `npm run build` if `frontend\node_modules` exists (`-SkipBuild` to skip).

### 3) Preview
```powershell
cd 'D:\Other Project\Milk-Hisab\frontend'
npx ng serve
```

### Optional dump (before/after)
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File 'D:\Other Project\Milk-Hisab\ui-polish-pack\DUMP.ps1'
```
Then CopyToBox from `C:\Users\raj.parmar\AppData\Local\Temp\milk-hisab-plan\ui-polish-dump\`.
