#Requires -Version 5.1
<#
  Milk Hisab — premium UI polish APPLY
  Run on Windows machine against the live local tree (do NOT commit).
  Usage:
    powershell -NoProfile -ExecutionPolicy Bypass -File .\APPLY.ps1
    powershell -NoProfile -ExecutionPolicy Bypass -File .\APPLY.ps1 -UpdateHtml
    powershell -NoProfile -ExecutionPolicy Bypass -File .\APPLY.ps1 -SkipBuild
#>
param(
  [switch]$UpdateHtml,
  [switch]$SkipBuild,
  [string]$Root = 'D:\Other Project\Milk-Hisab'
)

$ErrorActionPreference = 'Stop'
$Pack = $PSScriptRoot
$Fe = Join-Path $Root 'frontend\src'
$App = Join-Path $Fe 'app'

if (-not (Test-Path $Fe)) { throw "Frontend not found: $Fe" }

Write-Host 'Milk Hisab premium UI APPLY' -ForegroundColor Cyan
Write-Host ("Pack: " + $Pack)
Write-Host ("Target: " + $Fe)

function CopyTo([string]$relPack, [string]$relDest) {
  $src = Join-Path $Pack $relPack
  if (-not (Test-Path $src)) { throw ("Missing pack file: " + $relPack) }
  $dest = Join-Path $Fe $relDest
  New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
  Copy-Item $src $dest -Force
  Write-Host ("Wrote " + $relDest) -ForegroundColor Green
}

function SoftenTeal([string]$path) {
  if (-not (Test-Path $path)) { return }
  $t = Get-Content $path -Raw
  $orig = $t
  # Map leftover teal brand colors → purple/navy tokens / neutrals
  $t = $t -replace '#0f766e', 'var(--mh-purple)'
  $t = $t -replace '#134e4a', 'var(--mh-purple-hover)'
  $t = $t -replace '#99f6e4', 'var(--mh-border)'
  $t = $t -replace '#ccfbf1', 'var(--mh-border)'
  $t = $t -replace '#f0fdfa', 'var(--mh-surface)'
  $t = $t -replace '#0f172a', 'var(--mh-navy)'
  if ($t -ne $orig) {
    Set-Content -Path $path -Value $t -NoNewline
    Write-Host ("Softened teal overrides: " + $path) -ForegroundColor Yellow
  }
}

# 1) Global design system (single source of truth for shell + tokens)
CopyTo 'styles.scss' 'styles.scss'

# 2) Reconcile shell: app.css becomes host-only (no duplicated sidebar/topbar)
CopyTo 'app\app.css' 'app\app.css'

# 3) HTML shell — update if requested OR if current shell still looks like topbar-only
$htmlPath = Join-Path $App 'app.html'
$shouldHtml = $UpdateHtml.IsPresent
if ((Test-Path $htmlPath) -and -not $shouldHtml) {
  $html = Get-Content $htmlPath -Raw
  if ($html -notmatch 'sidebar' -or $html -notmatch 'brand-mark') {
    $shouldHtml = $true
    Write-Host 'app.html lacks sidebar/brand-mark — will refresh shell HTML' -ForegroundColor Yellow
  } else {
    Write-Host 'Keeping existing app.html (already has sidebar + brand-mark)' -ForegroundColor DarkGray
  }
}
if ($shouldHtml -or -not (Test-Path $htmlPath)) {
  CopyTo 'app\app.html' 'app\app.html'
}

# 4) Feature page SCSS (create or overwrite polish stubs / token-aligned overrides)
# Force-overwrite files that commonly fight the purple brand (teal leftovers)
$forceOverwrite = @(
  @{ Pack = 'features\customers\customer-form.page.scss'; Dest = 'app\features\customers\customer-form.page.scss' },
  @{ Pack = 'features\receipts\receipt.page.scss'; Dest = 'app\features\receipts\receipt.page.scss' }
)
foreach ($item in $forceOverwrite) {
  $dir = Split-Path (Join-Path $Fe $item.Dest)
  if (Test-Path $dir) { CopyTo $item.Pack $item.Dest }
  else { Write-Host ("Skip (no feature dir): " + $item.Dest) -ForegroundColor DarkGray }
}

# Stub SCSS only when missing (do not wipe local layout work)
$stubIfMissing = @(
  @{ Pack = 'features\dashboard\dashboard.page.scss'; Dest = 'app\features\dashboard\dashboard.page.scss' },
  @{ Pack = 'features\customers\customers-list.page.scss'; Dest = 'app\features\customers\customers-list.page.scss' },
  @{ Pack = 'features\daily\daily.page.scss'; Dest = 'app\features\daily\daily.page.scss' },
  @{ Pack = 'features\payments\payments.page.scss'; Dest = 'app\features\payments\payments.page.scss' },
  @{ Pack = 'features\hisab\hisab.page.scss'; Dest = 'app\features\hisab\hisab.page.scss' },
  @{ Pack = 'features\settings\settings.page.scss'; Dest = 'app\features\settings\settings.page.scss' },
  @{ Pack = 'features\personal\personal.page.scss'; Dest = 'app\features\personal\personal.page.scss' }
)
foreach ($item in $stubIfMissing) {
  $destFull = Join-Path $Fe $item.Dest
  $dir = Split-Path $destFull
  if (-not (Test-Path $dir)) {
    if ($item.Dest -like '*personal*') {
      New-Item -ItemType Directory -Force -Path $dir | Out-Null
    } else {
      Write-Host ("Skip (no feature dir): " + $item.Dest) -ForegroundColor DarkGray
      continue
    }
  }
  if (Test-Path $destFull) {
    Write-Host ("Keep existing: " + $item.Dest) -ForegroundColor DarkGray
  } else {
    CopyTo $item.Pack $item.Dest
  }
}

# 5) Sweep any remaining page SCSS for teal leftovers (style only)
Get-ChildItem -Path (Join-Path $App 'features') -Recurse -Include *.scss,*.css -ErrorAction SilentlyContinue |
  ForEach-Object { SoftenTeal $_.FullName }

# 6) If personal had a separate calendar.scss with hard-coded colors, soften it
$cal = Join-Path $App 'features\personal\calendar.scss'
if (Test-Path $cal) { SoftenTeal $cal }
# Also check component-level styles next to personal page
Get-ChildItem (Join-Path $App 'features\personal') -Filter '*.scss' -ErrorAction SilentlyContinue |
  ForEach-Object { SoftenTeal $_.FullName }

# 7) Optional production build
$feRoot = Join-Path $Root 'frontend'
$nm = Join-Path $feRoot 'node_modules'
$buildResult = 'skipped'
if (-not $SkipBuild -and (Test-Path $nm)) {
  Write-Host 'Running npm run build...' -ForegroundColor Cyan
  Push-Location $feRoot
  try {
    npm run build 2>&1 | Tee-Object -Variable buildOut | Out-Host
    if ($LASTEXITCODE -eq 0) { $buildResult = 'ok' } else { $buildResult = "failed exit=$LASTEXITCODE" }
  } catch {
    $buildResult = "error: $($_.Exception.Message)"
  } finally {
    Pop-Location
  }
} elseif (-not (Test-Path $nm)) {
  $buildResult = 'skipped (no node_modules)'
}

Write-Host ''
Write-Host 'APPLY_PREMIUM_UI_OK' -ForegroundColor Green
Write-Host ("Build: " + $buildResult)
Write-Host 'Preview: cd frontend; ng serve   (or npm start)'
