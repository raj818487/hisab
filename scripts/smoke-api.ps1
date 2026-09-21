<#
.SYNOPSIS
  Smoke-test Milk Hisab API on http://localhost:8080
.DESCRIPTION
  Hits /health (and /api/v1/health) plus /api/v1/customers.
  Exit 0 on success; non-zero if API is down or returns error.
#>
$ErrorActionPreference = 'Stop'
$Base = if ($env:MILK_HISAB_API) { $env:MILK_HISAB_API.TrimEnd('/') } else { 'http://localhost:8080' }

Write-Host "Milk Hisab API smoke — $Base" -ForegroundColor Cyan

function Invoke-Check([string]$Path, [string]$Label) {
  $url = "$Base$Path"
  Write-Host "GET $url ..." -NoNewline
  try {
    $resp = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10
    if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 300) {
      Write-Host " $($resp.StatusCode) OK" -ForegroundColor Green
      return $true
    }
    Write-Host " $($resp.StatusCode) FAIL" -ForegroundColor Red
    return $false
  } catch {
    Write-Host " ERROR: $($_.Exception.Message)" -ForegroundColor Red
    return $false
  }
}

$ok = $true
# Prefer root /health; also try api/v1/health
$h1 = Invoke-Check '/health' 'health'
$h2 = Invoke-Check '/api/v1/health' 'api-health'
if (-not ($h1 -or $h2)) { $ok = $false }

$c = Invoke-Check '/api/v1/customers' 'customers'
if (-not $c) { $ok = $false }

# Optional hisab sanity (current month)
$now = Get-Date
$hisabPath = "/api/v1/hisab/month?year=$($now.Year)&month=$($now.Month)"
$h = Invoke-Check $hisabPath 'hisab-month'
# hisab optional for pass — warn only
if (-not $h) { Write-Host "WARN: hisab endpoint not reachable (non-fatal)" -ForegroundColor Yellow }

if ($ok) {
  Write-Host "`nSMOKE PASS" -ForegroundColor Green
  exit 0
}

Write-Host "`nSMOKE FAIL — is the API running?  dotnet run --urls http://localhost:8080" -ForegroundColor Red
exit 1
