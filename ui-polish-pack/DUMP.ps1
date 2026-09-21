#Requires -Version 5.1
# Dump current style surfaces to Temp for parent CopyToBox
$ErrorActionPreference = 'Continue'
$root = 'D:\Other Project\Milk-Hisab\frontend\src'
$out = 'C:\Users\raj.parmar\AppData\Local\Temp\milk-hisab-plan\ui-polish-dump'
New-Item -ItemType Directory -Force -Path $out | Out-Null

$files = @(
  'styles.scss',
  'index.html',
  'app\app.html', 'app\app.css', 'app\app.scss',
  'app\features\personal\personal.page.html',
  'app\features\personal\personal.page.scss',
  'app\features\personal\calendar.scss',
  'app\features\dashboard\dashboard.page.scss',
  'app\features\customers\customers-list.page.scss',
  'app\features\customers\customer-form.page.scss',
  'app\features\customers\customer-form.page.html',
  'app\features\daily\daily.page.scss',
  'app\features\payments\payments.page.scss',
  'app\features\hisab\hisab.page.scss',
  'app\features\receipts\receipt.page.scss',
  'app\features\settings\settings.page.scss'
)

foreach ($rel in $files) {
  $src = Join-Path $root $rel
  $dest = Join-Path $out ($rel -replace '[\\/]', '__')
  if (Test-Path $src) {
    Copy-Item $src $dest -Force
    Write-Host ("OK " + $rel)
  } else {
    Write-Host ("MISS " + $rel)
  }
}
Write-Host ("OUT " + $out)
Write-Host 'DUMP_UI_POLISH_OK'
