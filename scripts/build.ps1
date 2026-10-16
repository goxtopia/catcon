param([switch]$SkipArt)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $projectRoot
if (-not $SkipArt) { & node (Join-Path $PSScriptRoot 'draw-skin.js'); if ($LASTEXITCODE -ne 0) { throw 'Art generation failed' } }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.8 compiler is required.' }
$out = Join-Path $projectRoot 'dist/Catcon-1.4.0'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$sources = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
$iconOptions = @()
if (Test-Path -LiteralPath (Join-Path $projectRoot 'assets/catcon.ico')) { $iconOptions = @("/win32icon:$projectRoot/assets/catcon.ico") }
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /utf8output /codepage:65001 /warn:4 "/out:$out/Catcon.exe" "/win32manifest:$projectRoot/app.manifest" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll @iconOptions @sources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
Copy-Item -LiteralPath (Join-Path $projectRoot 'assets') -Destination $out -Recurse -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'config') -Destination $out -Recurse -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination $out -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs') -Destination $out -Recurse -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination $out -Force
Compress-Archive -Path "$out/*" -DestinationPath (Join-Path $projectRoot 'dist/Catcon-1.4.0-win.zip') -Force
$checksumLines = @(
    ((Get-FileHash -LiteralPath "$out/Catcon.exe" -Algorithm SHA256).Hash.ToLower() + '  Catcon-1.4.0/Catcon.exe'),
    ((Get-FileHash -LiteralPath (Join-Path $projectRoot 'dist/Catcon-1.4.0-win.zip') -Algorithm SHA256).Hash.ToLower() + '  Catcon-1.4.0-win.zip')
)
Set-Content -LiteralPath (Join-Path $projectRoot 'dist/SHA256SUMS.txt') -Value $checksumLines -Encoding ASCII
Write-Output "Built $out/Catcon.exe"
