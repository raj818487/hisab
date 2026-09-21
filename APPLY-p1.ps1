<#
.SYNOPSIS
  Applies Milk Hisab p1-pack into D:\Other Project\Milk-Hisab
.NOTES
  Manual payments only — no Razorpay/UPI gateway packages are added.
#>
$ErrorActionPreference = 'Stop'
$TargetRoot = 'D:\Other Project\Milk-Hisab'
$PackRoot = $PSScriptRoot

Write-Host "Milk Hisab P1 APPLY" -ForegroundColor Cyan
Write-Host "Pack:    $PackRoot"
Write-Host "Target:  $TargetRoot"

if (-not (Test-Path $TargetRoot)) {
  New-Item -ItemType Directory -Path $TargetRoot -Force | Out-Null
  Write-Host "Created target root."
}

function Copy-Tree($Rel) {
  $src = Join-Path $PackRoot $Rel
  $dst = Join-Path $TargetRoot $Rel
  if (-not (Test-Path $src)) {
    Write-Warning "Skip missing: $Rel"
    return
  }
  $dstParent = Split-Path $dst -Parent
  if (-not (Test-Path $dstParent)) {
    New-Item -ItemType Directory -Path $dstParent -Force | Out-Null
  }
  Copy-Item -Path $src -Destination $dst -Recurse -Force
  Write-Host "Copied $Rel"
}

# Backend
$backendFiles = @(
  'backend\Models\Customer.cs',
  'backend\Models\DailyEntry.cs',
  'backend\Models\Payment.cs',
  'backend\Data\AppDbContext.cs',
  'backend\DTOs\CustomerDtos.cs',
  'backend\DTOs\DailyEntryDtos.cs',
  'backend\DTOs\HisabDtos.cs',
  'backend\DTOs\PaymentDtos.cs',
  'backend\DTOs\ReceiptDtos.cs',
  'backend\Services\ICustomerService.cs',
  'backend\Services\CustomerService.cs',
  'backend\Services\IDailyEntryService.cs',
  'backend\Services\DailyEntryService.cs',
  'backend\Services\IHisabService.cs',
  'backend\Services\HisabService.cs',
  'backend\Services\IPaymentService.cs',
  'backend\Services\PaymentService.cs',
  'backend\Services\IReceiptService.cs',
  'backend\Services\ReceiptService.cs',
  'backend\Controllers\CustomersController.cs',
  'backend\Controllers\DailyEntriesController.cs',
  'backend\Controllers\HisabController.cs',
  'backend\Controllers\PaymentsController.cs',
  'backend\Controllers\ReceiptsController.cs',
  'backend\Program.cs',
  'backend\appsettings.json',
  'backend\appsettings.Development.json',
  'backend\MilkHisab.Api.csproj',
  'backend\Properties\launchSettings.json'
)

foreach ($f in $backendFiles) { Copy-Tree $f }

# Frontend — map pack frontend/src/app → common Angular layouts
$feCandidates = @(
  (Join-Path $TargetRoot 'frontend\src\app'),
  (Join-Path $TargetRoot 'src\app'),
  (Join-Path $TargetRoot 'client\src\app'),
  (Join-Path $TargetRoot 'MilkHisab.Web\src\app')
)
$FeApp = $null
foreach ($c in $feCandidates) {
  if (Test-Path $c) { $FeApp = $c; break }
}
if (-not $FeApp) {
  $FeApp = Join-Path $TargetRoot 'frontend\src\app'
  New-Item -ItemType Directory -Path $FeApp -Force | Out-Null
  Write-Host "Created frontend app at $FeApp"
}

$feSrc = Join-Path $PackRoot 'frontend\src\app'
Copy-Item -Path (Join-Path $feSrc '*') -Destination $FeApp -Recurse -Force
Write-Host "Copied frontend features into $FeApp"

$envSrc = Join-Path $PackRoot 'frontend\src\environments'
$envDstParent = Split-Path $FeApp -Parent
$envDst = Join-Path $envDstParent 'environments'
if (Test-Path $envSrc) {
  New-Item -ItemType Directory -Path $envDst -Force | Out-Null
  Copy-Item -Path (Join-Path $envSrc '*') -Destination $envDst -Force
  Write-Host "Copied environments"
}

# Docs
Copy-Tree 'docs\REQUIREMENTS-UPDATE.md'
Copy-Tree 'docs\BRD-REQUIREMENT-DELTA.md'
Copy-Tree 'README.md'

# Locate or create API project
$csproj = Get-ChildItem -Path $TargetRoot -Filter 'MilkHisab.Api.csproj' -Recurse -ErrorAction SilentlyContinue |
  Select-Object -First 1

if (-not $csproj) {
  $apiDir = Join-Path $TargetRoot 'backend'
  if (-not (Test-Path (Join-Path $apiDir 'MilkHisab.Api.csproj'))) {
    Write-Host "Creating new web API project..." -ForegroundColor Yellow
    Push-Location $TargetRoot
    try {
      if (-not (Test-Path $apiDir)) { New-Item -ItemType Directory -Path $apiDir | Out-Null }
      Push-Location $apiDir
      dotnet new webapi -n MilkHisab.Api -o . --force
      Pop-Location
    } finally {
      Pop-Location
    }
    # Re-copy pack files over template
    foreach ($f in $backendFiles) { Copy-Tree $f }
    $csproj = Get-Item (Join-Path $apiDir 'MilkHisab.Api.csproj')
  } else {
    $csproj = Get-Item (Join-Path $apiDir 'MilkHisab.Api.csproj')
  }
}

$projDir = $csproj.Directory.FullName
Write-Host "API project: $($csproj.FullName)"

Push-Location $projDir
try {
  Write-Host "Ensuring EF Core Sqlite + Design packages..." -ForegroundColor Cyan
  dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 9.0.0
  dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.0
  dotnet add package Swashbuckle.AspNetCore --version 7.2.0

  Write-Host "Building..." -ForegroundColor Cyan
  dotnet build
  if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit $LASTEXITCODE"
  }
  Write-Host "Build succeeded." -ForegroundColor Green
} finally {
  Pop-Location
}

Write-Host ""
Write-Host "DONE. Next:" -ForegroundColor Green
Write-Host "  cd `"$projDir`""
Write-Host "  dotnet run --urls http://localhost:8080"
Write-Host "  (frontend) ng serve  # CORS allows localhost:4200"
Write-Host "Payments: manual only. Receipts: /receipts/:paymentId + Download HTML."
