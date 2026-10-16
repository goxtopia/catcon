param(
    [string]$ReleaseDirectory,
    [string]$Compiler,
    [string]$OutputDirectory,
    [string]$TestIdentity
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $ReleaseDirectory) {
    $ReleaseDirectory = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'dist') -Directory |
        Where-Object { $_.Name -match '^Catcon-\d+\.\d+\.\d+$' } |
        Sort-Object { [version]($_.Name.Substring(7)) } -Descending | Select-Object -First 1 -ExpandProperty FullName
}
if (-not $ReleaseDirectory) { throw 'Build a portable release first.' }
$ReleaseDirectory = (Resolve-Path -LiteralPath $ReleaseDirectory).Path
$app = Join-Path $ReleaseDirectory 'Catcon.exe'
$fileVersion = [version](Get-Item -LiteralPath $app).VersionInfo.FileVersion
$version = '{0}.{1}.{2}' -f $fileVersion.Major,$fileVersion.Minor,$fileVersion.Build
if (-not $Compiler) { $Compiler = Join-Path $projectRoot 'tools/inno-6.7.3/ISCC.exe' }
if (-not (Test-Path -LiteralPath $Compiler)) { throw 'Run scripts/prepare-installer-tools.ps1 first, or pass -Compiler.' }
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $projectRoot 'dist' }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$runtime = Join-Path $projectRoot 'tools/downloads/ndp48-web.exe'
$expectedRuntimeHash = '0BBA3094588C4BFEC301939985222A20B340BF03431563DEC8B2B4478B06FFFA'
if (-not (Test-Path -LiteralPath $runtime) -or (Get-FileHash -LiteralPath $runtime).Hash -ne $expectedRuntimeHash) { throw 'Microsoft .NET runtime bootstrapper is missing or differs from the pinned download.' }
$name = 'Catcon-' + $version + '-Setup'
$options = @('/Qp',('/DAppVersion=' + $version),('/DReleaseDir=' + $ReleaseDirectory),('/DOutputFolder=' + $OutputDirectory),('/DRuntimeInstaller=' + $runtime))
if ($TestIdentity) {
    if ($TestIdentity -notmatch '^Catcon\.InstallerTest\.[a-zA-Z0-9-]+$') { throw 'Invalid isolated test identity.' }
    $options += @('/DInstallerTest=1',('/DAppIdentity=' + $TestIdentity))
    $name += '-Test'
}
$options += '/DOutputName=' + $name
& $Compiler @options (Join-Path $projectRoot 'installer/Catcon.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
$setup = Join-Path $OutputDirectory ($name + '.exe')
if (-not $TestIdentity) {
    $hashFile = Join-Path $projectRoot 'dist/SHA256SUMS.txt'
    $existing = @(Get-Content -LiteralPath $hashFile -ErrorAction SilentlyContinue | Where-Object { $_ -notmatch ([regex]::Escape($name + '.exe') + '$') })
    $existing += (Get-FileHash -LiteralPath $setup).Hash.ToLower() + '  ' + $name + '.exe'
    [IO.File]::WriteAllLines($hashFile,$existing,[Text.Encoding]::ASCII)
}
Write-Output ('Built installer: ' + $setup)
