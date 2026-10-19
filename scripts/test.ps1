$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $projectRoot 'dist/Catcon-1.6.1/Catcon.exe'
$report = Join-Path $projectRoot 'artifacts/test-results.json'
$process = Start-Process -FilePath $exe -ArgumentList @('--self-test', ('"' + $report + '"')) -WindowStyle Hidden -PassThru -Wait
$result = Get-Content -LiteralPath $report -Encoding UTF8 -Raw | ConvertFrom-Json
$result | Select-Object passed,checks,failures | ConvertTo-Json -Compress
if ($process.ExitCode -ne 0) { throw 'Self tests failed' }
$smoke = Join-Path $projectRoot 'artifacts/smoke-results.json'
$process = Start-Process -FilePath $exe -ArgumentList @('--smoke-test', ('"' + $smoke + '"')) -WindowStyle Hidden -PassThru -Wait
Get-Content -LiteralPath $smoke
if ($process.ExitCode -ne 0) { throw 'Native window smoke test failed' }
