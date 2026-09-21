#Requires -Version 5.1
<#
.SYNOPSIS
  Milk-Hisab Day-1 setup on WBLSLAP32:
  - Ensure PLAN + BRD in project root
  - Run dev-assistant install.ps1 (once)
  - Connect skills into Milk-Hisab (piped answers)
  - Scaffold .NET Web API (backend) + Angular PWA (frontend)
  - Write root README.md

Run from elevated or normal PowerShell:
  powershell -ExecutionPolicy Bypass -File setup-milk-hisab.ps1
#>
$ErrorActionPreference = 'Continue'
$ProgressPreference = 'SilentlyContinue'

$MilkHisab = 'D:\Other Project\Milk-Hisab'
$DevAssistant = 'D:\Other Project\dev-assistant'
$TempPlan = Join-Path $env:LOCALAPPDATA 'Temp\milk-hisab-plan'
$Report = [ordered]@{}
$created = New-Object System.Collections.Generic.List[string]

function Write-Step($m) { Write-Host "`n==== $m ====" -ForegroundColor Cyan }
function Ensure-Dir($p) {
  if (-not (Test-Path -LiteralPath $p)) {
    New-Item -ItemType Directory -Path $p -Force | Out-Null
    $script:created.Add($p)
  }
}

Write-Step '0) Preconditions'
if (-not (Test-Path -LiteralPath $MilkHisab)) {
  Ensure-Dir $MilkHisab
}
$Report['milkHisab'] = $MilkHisab
$Report['devAssistantExists'] = (Test-Path -LiteralPath $DevAssistant)
$Report['machine'] = $env:COMPUTERNAME

# ---------- 1) PLAN + BRD ----------
Write-Step '1) Ensure PLAN + BRD HTML'
Ensure-Dir $TempPlan
$planSrcCandidates = @(
  (Join-Path $TempPlan 'PLAN-5Day-Milk-Hisab.html'),
  (Join-Path $MilkHisab 'PLAN-5Day-Milk-Hisab.html')
)
$brdSrcCandidates = @(
  (Join-Path $TempPlan 'BRD-Milk-Hisab-PWA.html'),
  (Join-Path $MilkHisab 'BRD-Milk-Hisab-PWA.html')
)

$planDest = Join-Path $MilkHisab 'PLAN-5Day-Milk-Hisab.html'
$brdDest = Join-Path $MilkHisab 'BRD-Milk-Hisab-PWA.html'

$planCopied = $false
foreach ($c in $planSrcCandidates) {
  if ((Test-Path -LiteralPath $c) -and ($c -ne $planDest)) {
    Copy-Item -LiteralPath $c -Destination $planDest -Force
    $planCopied = $true
    Write-Host "Copied PLAN from $c"
    break
  }
}
if (-not (Test-Path -LiteralPath $planDest)) {
  Write-Warning "PLAN missing at $planDest — place via CopyFromBox to $TempPlan first"
  $Report['plan'] = 'MISSING'
} else {
  $Report['plan'] = if ($planCopied) { 'COPIED' } else { 'PRESENT' }
  $created.Add($planDest)
}

$brdCopied = $false
foreach ($c in $brdSrcCandidates) {
  if ((Test-Path -LiteralPath $c) -and ($c -ne $brdDest)) {
    Copy-Item -LiteralPath $c -Destination $brdDest -Force
    $brdCopied = $true
    Write-Host "Copied BRD from $c"
    break
  }
}
if (-not (Test-Path -LiteralPath $brdDest)) {
  Write-Warning "BRD missing at $brdDest"
  $Report['brd'] = 'MISSING'
} else {
  $Report['brd'] = if ($brdCopied) { 'COPIED' } else { 'PRESENT' }
  $created.Add($brdDest)
}

# ---------- 2) install.ps1 + connect.ps1 ----------
Write-Step '2a) Toolchain probe'
function Has-Cmd($name) { [bool](Get-Command $name -ErrorAction SilentlyContinue) }
$Report['dotnet'] = if (Has-Cmd dotnet) { (dotnet --version) } else { 'MISSING' }
$Report['node'] = if (Has-Cmd node) { (node --version) } else { 'MISSING' }
$Report['npm'] = if (Has-Cmd npm) { (npm --version) } else { 'MISSING' }
$Report['ng'] = if (Has-Cmd ng) { (ng version 2>$null | Select-String 'Angular CLI:' | ForEach-Object { $_.ToString().Trim() }) } else { 'MISSING' }
$Report['python'] = if (Has-Cmd python) { (python --version 2>&1 | Out-String).Trim() } elseif (Has-Cmd py) { (py --version 2>&1 | Out-String).Trim() } else { 'MISSING' }
$Report['uv'] = if (Has-Cmd uv) { (uv --version) } else { 'MISSING' }
$Report | ConvertTo-Json -Compress | Write-Host

Write-Step '2b) install.ps1 (dev-assistant)'
$installPs1 = Join-Path $DevAssistant 'install.ps1'
$connectPs1 = Join-Path $DevAssistant 'connect.ps1'
if (-not (Test-Path -LiteralPath $installPs1)) {
  $Report['install.ps1'] = 'NOT_FOUND'
  Write-Warning "install.ps1 not found at $installPs1"
} else {
  try {
    Push-Location $DevAssistant
    # Non-interactive-ish: suppress prompts where possible
    $env:CI = '1'
    $env:UV_NO_PROGRESS = '1'
    $out = & powershell -NoProfile -ExecutionPolicy Bypass -File $installPs1 2>&1 | Out-String
    $Report['install.ps1'] = if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { "PARTIAL exit=$LASTEXITCODE" } else { 'OK' }
    $Report['install.ps1_tail'] = ($out -split "`n" | Select-Object -Last 30) -join "`n"
    Write-Host $Report['install.ps1_tail']
  } catch {
    $Report['install.ps1'] = "ERROR: $($_.Exception.Message)"
  } finally {
    Pop-Location
  }
}

Write-Step '2c) connect.ps1 into Milk-Hisab (piped answers)'
# Preferred: Angular + .NET + PostgreSQL / 8765 / 6 (AST only)
if (-not (Test-Path -LiteralPath $connectPs1)) {
  $Report['connect.ps1'] = 'NOT_FOUND'
  Write-Warning "connect.ps1 not found"
} else {
  try {
    Push-Location $MilkHisab
    $answers = "Angular + .NET + PostgreSQL`n8765`n6`n"
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = 'powershell'
    $psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$connectPs1`""
    $psi.WorkingDirectory = $MilkHisab
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $p = [System.Diagnostics.Process]::Start($psi)
    $p.StandardInput.Write($answers)
    $p.StandardInput.Close()
    $stdout = $p.StandardOutput.ReadToEnd()
    $stderr = $p.StandardError.ReadToEnd()
    $p.WaitForExit(180000) | Out-Null
    if (-not $p.HasExited) { $p.Kill(); $Report['connect.ps1'] = 'TIMEOUT' }
    else {
      $Report['connect.ps1'] = if ($p.ExitCode -eq 0) { 'OK' } else { "PARTIAL exit=$($p.ExitCode)" }
    }
    $Report['connect.stdout_tail'] = (($stdout -split "`n") | Select-Object -Last 40) -join "`n"
    $Report['connect.stderr_tail'] = (($stderr -split "`n") | Select-Object -Last 20) -join "`n"
    Write-Host $Report['connect.stdout_tail']
    if ($stderr) { Write-Host $Report['connect.stderr_tail'] -ForegroundColor Yellow }
  } catch {
    $Report['connect.ps1'] = "ERROR: $($_.Exception.Message)"
  } finally {
    Pop-Location
  }

  # Verify connect artifacts
  $skillMarkers = @('skills', '.agents', '.cursor', 'config.json', 'graphify-out')
  $found = @()
  foreach ($m in $skillMarkers) {
    $p = Join-Path $MilkHisab $m
    if (Test-Path -LiteralPath $p) { $found += $m }
  }
  # also common nested
  if (Test-Path (Join-Path $MilkHisab '.cursor\rules')) { $found += '.cursor/rules' }
  $Report['connect_artifacts'] = ($found -join ', ')
  if (-not $found) {
    Write-Warning 'connect produced no obvious skills/.agents/.cursor — attempting manual mirror'
    $Report['connect_manual'] = 'ATTEMPTING'
    # Best-effort: copy known skill folders from lims-style or from plugin package if present
    $candidates = @(
      (Join-Path $DevAssistant 'skills'),
      (Join-Path $DevAssistant '.agents'),
      (Join-Path $DevAssistant '.cursor'),
      (Join-Path $DevAssistant 'omni-plugin\skills'),
      (Join-Path $DevAssistant 'plugin\skills')
    )
    foreach ($c in $candidates) {
      if (Test-Path -LiteralPath $c) {
        $name = Split-Path $c -Leaf
        $dest = Join-Path $MilkHisab $name
        if ($name -eq 'skills' -or $name -eq '.agents' -or $name -eq '.cursor') {
          Copy-Item -LiteralPath $c -Destination $dest -Recurse -Force
          $created.Add($dest)
          Write-Host "Mirrored $c -> $dest"
        }
      }
    }
    # Write a minimal config.json if connect would have
    $cfgPath = Join-Path $MilkHisab 'config.json'
    if (-not (Test-Path $cfgPath)) {
      # Search for plugin config template
      $tpl = Get-ChildItem -Path $DevAssistant -Filter 'config.json' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
      if ($tpl) {
        Copy-Item $tpl.FullName $cfgPath -Force
      } else {
        @{
          techStack = 'Angular + .NET + PostgreSQL'
          port = 8765
          graphifyBackend = 6
          pluginPath = $DevAssistant
          projectRoot = $MilkHisab
        } | ConvertTo-Json | Set-Content -LiteralPath $cfgPath -Encoding UTF8
      }
      $created.Add($cfgPath)
    }
    $Report['connect_manual'] = 'DONE_BEST_EFFORT'
  }
}

# ---------- 3) Scaffold backend + frontend ----------
Write-Step '3a) .NET Web API backend'
$backendDir = Join-Path $MilkHisab 'backend'
$slnPath = Join-Path $MilkHisab 'MilkHisab.sln'
Push-Location $MilkHisab
if (Has-Cmd dotnet) {
  try {
    if (-not (Test-Path $slnPath)) {
      dotnet new sln -n MilkHisab -o . --force 2>&1 | Out-Host
    }
    if (-not (Test-Path (Join-Path $backendDir 'MilkHisab.Api.csproj')) -and -not (Test-Path (Join-Path $backendDir 'MilkHisab.Api'))) {
      # Prefer net8.0; fall back to SDK default
      $tfm = 'net8.0'
      $sdks = (dotnet --list-sdks) -join ' '
      if ($sdks -match '9\.') { $tfm = 'net9.0' }
      if ($sdks -match '10\.') { $tfm = 'net10.0' }
      $Report['dotnet_tfm'] = $tfm
      dotnet new webapi -n MilkHisab.Api -o backend --framework $tfm --use-controllers true 2>&1 | Out-Host
      if ($LASTEXITCODE -ne 0) {
        # retry without framework pin
        dotnet new webapi -n MilkHisab.Api -o backend --use-controllers true 2>&1 | Out-Host
      }
      dotnet sln MilkHisab.sln add (Join-Path $backendDir 'MilkHisab.Api.csproj') 2>&1 | Out-Host
    }
    # Placeholder folders + AppDbContext stub
    foreach ($d in @('Controllers','Services','Data','Models')) {
      Ensure-Dir (Join-Path $backendDir $d)
    }
    $dbCtx = Join-Path $backendDir 'Data\AppDbContext.cs'
    if (-not (Test-Path $dbCtx)) {
      @'
namespace MilkHisab.Api.Data;

// Placeholder — wire Npgsql + EF Core in Day 1 PM
public class AppDbContext // : DbContext
{
    // public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}
'@ | Set-Content -LiteralPath $dbCtx -Encoding UTF8
      $created.Add($dbCtx)
    }
    $isvc = Join-Path $backendDir 'Services\IHealthService.cs'
    if (-not (Test-Path $isvc)) {
      @'
namespace MilkHisab.Api.Services;

public interface IHealthService
{
    string Status();
}

public class HealthService : IHealthService
{
    public string Status() => "ok";
}
'@ | Set-Content -LiteralPath $isvc -Encoding UTF8
      $created.Add($isvc)
    }
    # launchSettings port 8080
    $launch = Join-Path $backendDir 'Properties\launchSettings.json'
    Ensure-Dir (Join-Path $backendDir 'Properties')
    @'
{
  "profiles": {
    "MilkHisab.Api": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:8080",
      "environmentVariables": { "ASPNETCORE_ENVIRONMENT": "Development" }
    }
  }
}
'@ | Set-Content -LiteralPath $launch -Encoding UTF8
    $Report['backend'] = 'DOTNET_SCAFFOLDED'
    $created.Add($backendDir)
  } catch {
    $Report['backend'] = "ERROR: $($_.Exception.Message)"
  }
} else {
  Write-Warning 'dotnet missing — writing minimal backend structure'
  Ensure-Dir $backendDir
  foreach ($d in @('Controllers','Services','Data','Models','Properties')) {
    Ensure-Dir (Join-Path $backendDir $d)
  }
  @'
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>MilkHisab.Api</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.11" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.9.0" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.11" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.11">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
</Project>
'@ | Set-Content (Join-Path $backendDir 'MilkHisab.Api.csproj') -Encoding UTF8
  @'
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors();
app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.Run();
'@ | Set-Content (Join-Path $backendDir 'Program.cs') -Encoding UTF8
  @'
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=milk_hisab;Username=postgres;Password=postgres"
  }
}
'@ | Set-Content (Join-Path $backendDir 'appsettings.json') -Encoding UTF8
  @'
Microsoft Visual Studio Solution File, Format Version 12.00
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MilkHisab.Api", "backend\MilkHisab.Api.csproj", "{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}"
EndProject
Global
        GlobalSection(SolutionConfigurationPlatforms) = preSolution
                Debug|Any CPU = Debug|Any CPU
                Release|Any CPU = Release|Any CPU
        EndGlobalSection
EndGlobal
'@ | Set-Content $slnPath -Encoding UTF8
  $Report['backend'] = 'MINIMAL_NO_DOTNET'
  $created.Add($backendDir)
}
Pop-Location

Write-Step '3b) Angular PWA frontend'
$frontendDir = Join-Path $MilkHisab 'frontend'
Push-Location $MilkHisab
$ngOk = $false
if ((Has-Cmd npx) -or (Has-Cmd ng)) {
  try {
    if (-not (Test-Path (Join-Path $frontendDir 'package.json'))) {
      $ngArgs = @('--yes','@angular/cli@20','new','milk-hisab','--directory=frontend','--routing','--style=scss','--ssr=false','--skip-git','--defaults')
      Write-Host "Running: npx $($ngArgs -join ' ')"
      & npx @ngArgs 2>&1 | Out-Host
      if ($LASTEXITCODE -eq 0 -and (Test-Path (Join-Path $frontendDir 'package.json'))) { $ngOk = $true }
    } else { $ngOk = $true }

    if ($ngOk) {
      Push-Location $frontendDir
      try {
        & npx --yes ng add @angular/pwa --project milk-hisab --skip-confirmation 2>&1 | Out-Host
        if ($LASTEXITCODE -ne 0) {
          & npx --yes ng add @angular/pwa --skip-confirmation 2>&1 | Out-Host
        }
      } finally { Pop-Location }
      $Report['frontend'] = 'ANGULAR_SCAFFOLDED'
    }
  } catch {
    $Report['frontend'] = "ERROR: $($_.Exception.Message)"
  }
}

if (-not $ngOk -and -not (Test-Path (Join-Path $frontendDir 'package.json'))) {
  Write-Warning 'ng/npx scaffold failed or missing — writing minimal Angular+PWA structure'
  Ensure-Dir $frontendDir
  Ensure-Dir (Join-Path $frontendDir 'src\app')
  Ensure-Dir (Join-Path $frontendDir 'src\assets\icons')
  Ensure-Dir (Join-Path $frontendDir 'public')
  @'
{
  "name": "milk-hisab",
  "version": "0.0.1",
  "scripts": {
    "start": "ng serve --port 4200",
    "build": "ng build",
    "test": "ng test"
  },
  "private": true,
  "dependencies": {
    "@angular/animations": "^20.0.0",
    "@angular/common": "^20.0.0",
    "@angular/compiler": "^20.0.0",
    "@angular/core": "^20.0.0",
    "@angular/forms": "^20.0.0",
    "@angular/platform-browser": "^20.0.0",
    "@angular/platform-browser-dynamic": "^20.0.0",
    "@angular/router": "^20.0.0",
    "@angular/service-worker": "^20.0.0",
    "rxjs": "~7.8.0",
    "tslib": "^2.3.0",
    "zone.js": "~0.15.0"
  },
  "devDependencies": {
    "@angular/build": "^20.0.0",
    "@angular/cli": "^20.0.0",
    "@angular/compiler-cli": "^20.0.0",
    "typescript": "~5.8.0"
  }
}
'@ | Set-Content (Join-Path $frontendDir 'package.json') -Encoding UTF8
  @'
{
  "$schema": "./node_modules/@angular/cli/lib/config/schema.json",
  "version": 1,
  "projects": {
    "milk-hisab": {
      "projectType": "application",
      "root": "",
      "sourceRoot": "src",
      "prefix": "app",
      "architect": {
        "build": {
          "builder": "@angular/build:application",
          "options": {
            "outputPath": "dist/milk-hisab",
            "index": "src/index.html",
            "browser": "src/main.ts",
            "polyfills": ["zone.js"],
            "tsConfig": "tsconfig.app.json",
            "inlineStyleLanguage": "scss",
            "assets": ["src/assets", "src/manifest.webmanifest"],
            "styles": ["src/styles.scss"],
            "serviceWorker": "ngsw-config.json"
          }
        },
        "serve": {
          "builder": "@angular/build:dev-server",
          "options": { "buildTarget": "milk-hisab:build", "port": 4200 }
        }
      }
    }
  }
}
'@ | Set-Content (Join-Path $frontendDir 'angular.json') -Encoding UTF8
  @'
{
  "name": "Milk Hisab",
  "short_name": "MilkHisab",
  "theme_color": "#1565c0",
  "background_color": "#ffffff",
  "display": "standalone",
  "scope": "./",
  "start_url": "./",
  "icons": [
    { "src": "assets/icons/icon-192.png", "sizes": "192x192", "type": "image/png" },
    { "src": "assets/icons/icon-512.png", "sizes": "512x512", "type": "image/png" }
  ]
}
'@ | Set-Content (Join-Path $frontendDir 'src\manifest.webmanifest') -Encoding UTF8
  @'
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <title>Milk Hisab</title>
  <base href="/">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <link rel="manifest" href="manifest.webmanifest">
  <meta name="theme-color" content="#1565c0">
</head>
<body>
  <app-root></app-root>
</body>
</html>
'@ | Set-Content (Join-Path $frontendDir 'src\index.html') -Encoding UTF8
  @'
import { bootstrapApplication } from "@angular/platform-browser";
import { provideRouter } from "@angular/router";
import { provideServiceWorker } from "@angular/service-worker";
import { isDevMode } from "@angular/core";
import { AppComponent } from "./app/app.component";
import { routes } from "./app/app.routes";

bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(routes),
    provideServiceWorker("ngsw-worker.js", { enabled: !isDevMode() })
  ]
}).catch(console.error);
'@ | Set-Content (Join-Path $frontendDir 'src\main.ts') -Encoding UTF8
  @'
import { Component } from "@angular/core";
import { RouterOutlet, RouterLink } from "@angular/router";

@Component({
  selector: "app-root",
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  template: `
    <header style="padding:12px;background:#1565c0;color:#fff;display:flex;gap:12px;align-items:center">
      <strong>Milk Hisab</strong>
      <a routerLink="/" style="color:#fff">Dashboard</a>
      <a routerLink="/customers" style="color:#fff">Customers</a>
      <a routerLink="/daily" style="color:#fff">Daily</a>
      <a routerLink="/hisab" style="color:#fff">Hisab</a>
      <a routerLink="/payments" style="color:#fff">Payments</a>
      <a routerLink="/settings" style="color:#fff">Settings</a>
    </header>
    <main style="padding:16px"><router-outlet /></main>
  `
})
export class AppComponent {}
'@ | Set-Content (Join-Path $frontendDir 'src\app\app.component.ts') -Encoding UTF8
  @'
import { Routes } from "@angular/router";
import { Component } from "@angular/core";

@Component({ standalone: true, template: "<h1>Dashboard</h1><p>Milk Hisab shell — wire API next.</p>" })
class DashboardPage {}
@Component({ standalone: true, template: "<h1>Customers</h1>" })
class CustomersPage {}
@Component({ standalone: true, template: "<h1>Daily</h1>" })
class DailyPage {}
@Component({ standalone: true, template: "<h1>Hisab</h1>" })
class HisabPage {}
@Component({ standalone: true, template: "<h1>Payments</h1>" })
class PaymentsPage {}
@Component({ standalone: true, template: "<h1>Settings</h1>" })
class SettingsPage {}

export const routes: Routes = [
  { path: "", component: DashboardPage },
  { path: "customers", component: CustomersPage },
  { path: "daily", component: DailyPage },
  { path: "hisab", component: HisabPage },
  { path: "payments", component: PaymentsPage },
  { path: "settings", component: SettingsPage }
];
'@ | Set-Content (Join-Path $frontendDir 'src\app\app.routes.ts') -Encoding UTF8
  'body { margin: 0; font-family: system-ui, sans-serif; }' | Set-Content (Join-Path $frontendDir 'src\styles.scss') -Encoding UTF8
  @'
{
  "compileOnSave": false,
  "compilerOptions": {
    "outDir": "./dist/out-tsc",
    "strict": true,
    "experimentalDecorators": true,
    "moduleResolution": "bundler",
    "target": "ES2022",
    "module": "ES2022"
  }
}
'@ | Set-Content (Join-Path $frontendDir 'tsconfig.json') -Encoding UTF8
  @'
{ "extends": "./tsconfig.json", "compilerOptions": { "outDir": "./out-tsc/app" }, "files": ["src/main.ts"] }
'@ | Set-Content (Join-Path $frontendDir 'tsconfig.app.json') -Encoding UTF8
  @'
{
  "$schema": "./node_modules/@angular/service-worker/config/schema.json",
  "index": "/index.html",
  "assetGroups": [
    { "name": "app", "installMode": "prefetch", "resources": { "files": ["/favicon.ico","/index.html","/*.css","/*.js","/manifest.webmanifest"] } },
    { "name": "assets", "installMode": "lazy", "resources": { "files": ["/assets/**"] } }
  ]
}
'@ | Set-Content (Join-Path $frontendDir 'ngsw-config.json') -Encoding UTF8
  @'
# Install Node 20+ then:
#   cd frontend && npm install && npm start
# UI: http://localhost:4200
'@ | Set-Content (Join-Path $frontendDir 'README.md') -Encoding UTF8
  $Report['frontend'] = 'MINIMAL_ANGULAR_PWA'
  $created.Add($frontendDir)
}
Pop-Location

# ---------- 4) Root README ----------
Write-Step '4) Root README.md'
$readme = Join-Path $MilkHisab 'README.md'
@'
# Milk Hisab

Daily milk delivery tracker & monthly hisab (PWA).

## Stack
- **Frontend:** Angular PWA — `frontend/` (port **4200**)
- **Backend:** .NET Web API — `backend/` / `MilkHisab.Api` (port **8080**)
- **DB:** PostgreSQL
- **Docs:** `BRD-Milk-Hisab-PWA.html`, `PLAN-5Day-Milk-Hisab.html`

## Run API
```powershell
cd "D:\Other Project\Milk-Hisab"
dotnet run --project backend --urls http://localhost:8080
```

## Run UI
```powershell
cd "D:\Other Project\Milk-Hisab\frontend"
npm install
npm start
# or: npx ng serve --port 4200
```

## Skills (omni-plugin)
Installed from `D:\Other Project\dev-assistant` via `install.ps1` + `connect.ps1`
(Tech stack: Angular + .NET + PostgreSQL, port 8765, graphify backend 6 / AST only).
'@ | Set-Content -LiteralPath $readme -Encoding UTF8
$created.Add($readme)

# ---------- Report ----------
Write-Step 'REPORT'
$Report['created_paths'] = $created | Select-Object -Unique
$Report['tree_top'] = (Get-ChildItem -LiteralPath $MilkHisab | Select-Object Name, Mode | Format-Table | Out-String)
$jsonPath = Join-Path $MilkHisab 'setup-report.json'
$Report | ConvertTo-Json -Depth 6 | Set-Content $jsonPath -Encoding UTF8
Write-Host ($Report | ConvertTo-Json -Depth 6)
Write-Host "Wrote $jsonPath"
exit 0
