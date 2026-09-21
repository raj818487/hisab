#Requires -Version 5.1
$ErrorActionPreference = 'Continue'
$ProgressPreference = 'SilentlyContinue'
$MilkHisab = 'D:\Other Project\Milk-Hisab'
$DevAssistant = 'D:\Other Project\dev-assistant'
$Report = [ordered]@{}

function Write-Step($m) { Write-Host "`n==== $m ====" -ForegroundColor Cyan }
function Ensure-Dir($p) { if (-not (Test-Path -LiteralPath $p)) { New-Item -ItemType Directory -Path $p -Force | Out-Null } }
function Has-Cmd($name) { [bool](Get-Command $name -ErrorAction SilentlyContinue) }
function Write-Utf8($path, $content) {
  $dir = Split-Path -Parent $path
  if ($dir) { Ensure-Dir $dir }
  [System.IO.File]::WriteAllText($path, $content, [System.Text.UTF8Encoding]::new($false))
}

Ensure-Dir $MilkHisab
$Report['machine'] = $env:COMPUTERNAME
$Report['devAssistantExists'] = (Test-Path -LiteralPath $DevAssistant)

Write-Step '1) PLAN+BRD already expected in Milk-Hisab'
$Report['plan'] = if (Test-Path (Join-Path $MilkHisab 'PLAN-5Day-Milk-Hisab.html')) { 'PRESENT' } else { 'MISSING' }
$Report['brd'] = if (Test-Path (Join-Path $MilkHisab 'BRD-Milk-Hisab-PWA.html')) { 'PRESENT' } else { 'MISSING' }

Write-Step '2a) Toolchain'
$Report['dotnet'] = if (Has-Cmd dotnet) { (dotnet --version) } else { 'MISSING' }
$Report['node'] = if (Has-Cmd node) { (node --version) } else { 'MISSING' }
$Report['npm'] = if (Has-Cmd npm) { (npm --version) } else { 'MISSING' }
$Report['python'] = if (Has-Cmd python) { (python --version 2>&1 | Out-String).Trim() } else { 'MISSING' }
$Report | ConvertTo-Json -Compress | Write-Host

Write-Step '2b) install.ps1'
$installPs1 = Join-Path $DevAssistant 'install.ps1'
if (Test-Path -LiteralPath $installPs1) {
  Push-Location $DevAssistant
  try {
    $env:CI = '1'
    $out = & powershell -NoProfile -ExecutionPolicy Bypass -File $installPs1 2>&1 | Out-String
    $Report['install.ps1'] = if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { "PARTIAL exit=$LASTEXITCODE" } else { 'OK' }
    $Report['install.ps1_tail'] = (($out -split "`n") | Select-Object -Last 25) -join "`n"
    Write-Host $Report['install.ps1_tail']
  } catch {
    $Report['install.ps1'] = "ERROR: $($_.Exception.Message)"
  } finally { Pop-Location }
} else { $Report['install.ps1'] = 'NOT_FOUND' }

Write-Step '2c) connect.ps1 piped'
$connectPs1 = Join-Path $DevAssistant 'connect.ps1'
if (Test-Path -LiteralPath $connectPs1) {
  Push-Location $MilkHisab
  try {
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
    if (-not $p.WaitForExit(300000)) { $p.Kill(); $Report['connect.ps1'] = 'TIMEOUT' }
    else { $Report['connect.ps1'] = if ($p.ExitCode -eq 0) { 'OK' } else { "PARTIAL exit=$($p.ExitCode)" } }
    $Report['connect.stdout_tail'] = (($stdout -split "`n") | Select-Object -Last 40) -join "`n"
    Write-Host $Report['connect.stdout_tail']
    if ($stderr) { Write-Host ((($stderr -split "`n") | Select-Object -Last 15) -join "`n") -ForegroundColor Yellow }
  } catch {
    $Report['connect.ps1'] = "ERROR: $($_.Exception.Message)"
  } finally { Pop-Location }

  # Manual mirror if empty
  $found = @()
  foreach ($m in @('skills','.agents','.cursor','dev-assistant.json','graphify-out','.graphifyignore')) {
    if (Test-Path (Join-Path $MilkHisab $m)) { $found += $m }
  }
  $Report['connect_artifacts'] = ($found -join ', ')
  if (-not $found) {
    foreach ($name in @('skills','.agents','.cursor','templates','workflows')) {
      $src = Join-Path $DevAssistant $name
      if (Test-Path $src) {
        Copy-Item $src (Join-Path $MilkHisab $name) -Recurse -Force
        Write-Host "Mirrored $name"
      }
    }
    # Also copy agent rule files
    foreach ($f in @('AGENTS.md','CLAUDE.md','GEMINI.md')) {
      $src = Join-Path $DevAssistant $f
      if (Test-Path $src) { Copy-Item $src (Join-Path $MilkHisab $f) -Force }
    }
    $cfg = @{
      projectName = 'Milk-Hisab'
      projectRoot = $MilkHisab
      techStack = 'Angular + .NET + PostgreSQL'
      port = 8765
      graphifyBackend = 'ast-only'
      apiPattern = '/api/v1/<resource>'
      backendPattern = 'Controller -> IService -> Service(AppDbContext); hybrid, no MediatR'
      frontendPattern = 'Angular standalone + PWA manifest'
    }
    $cfg | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $MilkHisab 'dev-assistant.json') -Encoding UTF8
    $Report['connect_manual'] = 'MIRRORED'
  }
} else { $Report['connect.ps1'] = 'NOT_FOUND' }

Write-Step '3a) backend'
$backendDir = Join-Path $MilkHisab 'backend'
Push-Location $MilkHisab
if (Has-Cmd dotnet) {
  if (-not (Test-Path (Join-Path $MilkHisab 'MilkHisab.sln'))) {
    dotnet new sln -n MilkHisab -o . --force 2>&1 | Out-Host
  }
  if (-not (Test-Path (Join-Path $backendDir 'MilkHisab.Api.csproj'))) {
    $tfm = 'net8.0'
    $sdks = (dotnet --list-sdks) -join ' '
    if ($sdks -match '9\.') { $tfm = 'net9.0' }
    if ($sdks -match '10\.') { $tfm = 'net10.0' }
    $Report['dotnet_tfm'] = $tfm
    dotnet new webapi -n MilkHisab.Api -o backend --framework $tfm --use-controllers 2>&1 | Out-Host
    if ($LASTEXITCODE -ne 0) { dotnet new webapi -n MilkHisab.Api -o backend --use-controllers 2>&1 | Out-Host }
    if (Test-Path (Join-Path $backendDir 'MilkHisab.Api.csproj')) {
      dotnet sln MilkHisab.sln add (Join-Path $backendDir 'MilkHisab.Api.csproj') 2>&1 | Out-Host
    }
  }
  foreach ($d in @('Controllers','Services','Data','Models')) { Ensure-Dir (Join-Path $backendDir $d) }
  $dbCtx = Join-Path $backendDir 'Data\AppDbContext.cs'
  if (-not (Test-Path $dbCtx)) {
    Write-Utf8 $dbCtx @"
namespace MilkHisab.Api.Data;

// Placeholder - wire Npgsql + EF Core next
public class AppDbContext
{
}
"@
  }
  $svc = Join-Path $backendDir 'Services\IHealthService.cs'
  if (-not (Test-Path $svc)) {
    Write-Utf8 $svc @"
namespace MilkHisab.Api.Services;

public interface IHealthService
{
    string Status();
}

public class HealthService : IHealthService
{
    public string Status() => "ok";
}
"@
  }
  Ensure-Dir (Join-Path $backendDir 'Properties')
  Write-Utf8 (Join-Path $backendDir 'Properties\launchSettings.json') @"
{
  "profiles": {
    "MilkHisab.Api": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:8080",
      "environmentVariables": { "ASPNETCORE_ENVIRONMENT": "Development" }
    }
  }
}
"@
  $Report['backend'] = 'DOTNET_SCAFFOLDED'
} else {
  Ensure-Dir $backendDir
  foreach ($d in @('Controllers','Services','Data','Models','Properties')) { Ensure-Dir (Join-Path $backendDir $d) }
  Write-Utf8 (Join-Path $backendDir 'MilkHisab.Api.csproj') @"
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>MilkHisab.Api</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.9.0" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.11" />
  </ItemGroup>
</Project>
"@
  Write-Utf8 (Join-Path $backendDir 'Program.cs') @"
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
"@
  Write-Utf8 (Join-Path $backendDir 'appsettings.json') @"
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=milk_hisab;Username=postgres;Password=postgres"
  }
}
"@
  $Report['backend'] = 'MINIMAL_NO_DOTNET'
}
Pop-Location

Write-Step '3b) frontend Angular PWA'
$frontendDir = Join-Path $MilkHisab 'frontend'
$ngOk = $false
Push-Location $MilkHisab
if ((Has-Cmd npx) -or (Has-Cmd npm)) {
  try {
    if (-not (Test-Path (Join-Path $frontendDir 'package.json'))) {
      & npx --yes @angular/cli@20 new milk-hisab --directory=frontend --routing --style=scss --ssr=false --skip-git --defaults 2>&1 | Out-Host
      if ($LASTEXITCODE -eq 0 -and (Test-Path (Join-Path $frontendDir 'package.json'))) { $ngOk = $true }
    } else { $ngOk = $true }
    if ($ngOk) {
      Push-Location $frontendDir
      try {
        & npx --yes ng add @angular/pwa --skip-confirmation 2>&1 | Out-Host
      } finally { Pop-Location }
      $Report['frontend'] = 'ANGULAR_SCAFFOLDED'
    }
  } catch {
    $Report['frontend'] = "ERROR: $($_.Exception.Message)"
  }
}
if (-not $ngOk -and -not (Test-Path (Join-Path $frontendDir 'package.json'))) {
  Ensure-Dir (Join-Path $frontendDir 'src\app')
  Ensure-Dir (Join-Path $frontendDir 'public')
  Write-Utf8 (Join-Path $frontendDir 'package.json') '{"name":"milk-hisab","version":"0.0.1","private":true,"scripts":{"start":"ng serve --port 4200","build":"ng build"},"dependencies":{"@angular/core":"^20.0.0","@angular/common":"^20.0.0","@angular/platform-browser":"^20.0.0","@angular/router":"^20.0.0","@angular/service-worker":"^20.0.0","rxjs":"~7.8.0","zone.js":"~0.15.0"},"devDependencies":{"@angular/cli":"^20.0.0","@angular/compiler-cli":"^20.0.0","typescript":"~5.8.0"}}'
  Write-Utf8 (Join-Path $frontendDir 'public\manifest.webmanifest') '{"name":"Milk Hisab","short_name":"MilkHisab","start_url":"/","display":"standalone","background_color":"#ffffff","theme_color":"#0f766e","icons":[{"src":"icons/icon-192.png","sizes":"192x192","type":"image/png"},{"src":"icons/icon-512.png","sizes":"512x512","type":"image/png"}]}'
  Write-Utf8 (Join-Path $frontendDir 'src\index.html') '<!doctype html><html lang="en"><head><meta charset="utf-8"><title>Milk Hisab</title><base href="/"><meta name="viewport" content="width=device-width, initial-scale=1"><link rel="manifest" href="manifest.webmanifest"><meta name="theme-color" content="#0f766e"></head><body><app-root></app-root></body></html>'
  Write-Utf8 (Join-Path $frontendDir 'src\main.ts') "import { bootstrapApplication } from '@angular/platform-browser';\nimport { AppComponent } from './app/app.component';\nbootstrapApplication(AppComponent).catch(console.error);\n"
  Write-Utf8 (Join-Path $frontendDir 'src\app\app.component.ts') "import { Component } from '@angular/core';\n@Component({ selector: 'app-root', standalone: true, template: '<h1>Milk Hisab</h1><p>Day 1 scaffold ready.</p>' })\nexport class AppComponent {}\n"
  Write-Utf8 (Join-Path $frontendDir 'README.md') "# Milk Hisab Frontend`n`nnpm install`nnpm start  # http://localhost:4200`n"
  $Report['frontend'] = 'MINIMAL_ANGULAR_TREE'
}
Pop-Location

Write-Step '4) README'
$readme = Join-Path $MilkHisab 'README.md'
Write-Utf8 $readme @"
# Milk Hisab PWA

Daily milk take/skip + monthly hisab. Target: 5-day full product (see PLAN-5Day-Milk-Hisab.html).

## Stack
- Frontend: Angular 20 + PWA (`frontend/`) — http://localhost:4200
- Backend: .NET Web API (`backend/`) — http://localhost:8080
- DB: PostgreSQL

## Run
```bash
# API
cd backend
dotnet run

# UI
cd frontend
npm install
npm start
```

## Dev-assistant
Installed from `D:\Other Project\dev-assistant` (install.ps1 + connect.ps1).
Config: `dev-assistant.json`. Chat port default 8765.
"@

$Report['created_paths'] = @(
  (Join-Path $MilkHisab 'PLAN-5Day-Milk-Hisab.html'),
  (Join-Path $MilkHisab 'BRD-Milk-Hisab-PWA.html'),
  $backendDir,
  $frontendDir,
  $readme
)
$Report['doneAt'] = (Get-Date).ToString('s')
$reportPath = Join-Path $MilkHisab 'setup-report.json'
($Report | ConvertTo-Json -Depth 6) | Set-Content $reportPath -Encoding UTF8
Write-Step 'DONE'
Get-Content $reportPath
Write-Host "`nTree:"
Get-ChildItem $MilkHisab | Format-Table Name, Mode
