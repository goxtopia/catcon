$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$testFolder = 'artifacts/' + [string][char]0x53D1 + [char]0x5E03 + ' ' + [char]0x9A8C + [char]0x8BC1
$unpacked = [IO.Path]::GetFullPath((Join-Path $projectRoot $testFolder))
if (-not $unpacked.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Package verification directory must stay in this workspace' }
Expand-Archive -LiteralPath (Join-Path $projectRoot 'dist/Catcon-1.2.3-win.zip') -DestinationPath $unpacked -Force
$report = Join-Path $projectRoot 'artifacts/package-tests.json'
$process = Start-Process -FilePath (Join-Path $unpacked 'Catcon.exe') -ArgumentList @('--self-test', ('"' + $report + '"')) -WindowStyle Hidden -PassThru -Wait
if ($process.ExitCode -ne 0) { throw 'Extracted application failed its regression tests' }
$result = Get-Content -LiteralPath $report -Encoding UTF8 -Raw | ConvertFrom-Json
Add-Type -AssemblyName System.Drawing
$framesRoot = Join-Path $unpacked 'assets/skins/mikan/frames'
$manifest = Get-Content -LiteralPath (Join-Path $framesRoot 'manifest.json') -Encoding UTF8 -Raw | ConvertFrom-Json
foreach ($clip in $manifest.clips) {
    if ($clip.durations.Count -ne $clip.frames -or @($clip.durations | Where-Object { $_ -lt 10 }).Count -gt 0) { throw ('Invalid exported timing: ' + $clip.id) }
    $image = [Drawing.Image]::FromFile((Join-Path $framesRoot ($clip.id + '.png')))
    try { if ($image.Height -ne 32 -or $image.Width -ne 32 * $clip.frames) { throw ('Invalid exported strip: ' + $clip.id) } } finally { $image.Dispose() }
}
if (-not (Test-Path -LiteralPath (Join-Path $unpacked 'docs/gallery.html'))) { throw 'Animation gallery is missing' }
$sourceHash = (Get-FileHash -LiteralPath (Join-Path $projectRoot 'dist/Catcon-1.2.3/Catcon.exe')).Hash
$packageHash = (Get-FileHash -LiteralPath (Join-Path $unpacked 'Catcon.exe')).Hash
if ($sourceHash -ne $packageHash) { throw 'Packaged executable differs from release executable' }
Write-Output ('Package verified: ' + $result.checks + ' checks; ' + $manifest.clips.Count + ' native 32px animation strips; Chinese and space-containing extraction path works.')
