param([string]$BaselineVersion='1.2.2',[string]$CurrentVersion='1.2.3')
$ErrorActionPreference = 'Stop'
foreach ($version in @($BaselineVersion,$CurrentVersion)) { if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Installer test versions must use x.y.z format.' } }
$projectRoot = Split-Path -Parent $PSScriptRoot
$identity = 'Catcon.InstallerTest.' + [Guid]::NewGuid().ToString('N')
$testRoot = Join-Path $projectRoot ('artifacts/installer-validation/' + $identity)
$installDir = Join-Path $testRoot (([string][char]0x5B89 + [char]0x88C5) + ' path/Catcon')
$keyPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\' + $identity + '_is1'
$checks = New-Object 'Collections.Generic.List[string]'
$oldRelease = Join-Path $projectRoot ('dist/Catcon-' + $BaselineVersion)
$newRelease = Join-Path $projectRoot ('dist/Catcon-' + $CurrentVersion)
function Check($condition,$message) { if (-not $condition) { throw $message }; $checks.Add($message) }
function Hash($file) { (Get-FileHash -LiteralPath $file).Hash }
function Run-Installer($file,$log,$directory) {
    $arguments = @('/VERYSILENT','/SUPPRESSMSGBOXES','/SP-','/NORESTART',('/LOG="' + (Join-Path $testRoot $log) + '"'))
    if ($directory) { $arguments += '/DIR="' + $directory + '"' }
    $process = Start-Process -FilePath $file -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
    return $process.ExitCode
}
New-Item -ItemType Directory -Force -Path $testRoot | Out-Null
$success = $false
try {
    & (Join-Path $PSScriptRoot 'build-installer.ps1') -ReleaseDirectory $oldRelease -OutputDirectory $testRoot -TestIdentity $identity
    & (Join-Path $PSScriptRoot 'build-installer.ps1') -ReleaseDirectory $newRelease -OutputDirectory $testRoot -TestIdentity $identity
    $oldSetup = Join-Path $testRoot ('Catcon-' + $BaselineVersion + '-Setup-Test.exe')
    $newSetup = Join-Path $testRoot ('Catcon-' + $CurrentVersion + '-Setup-Test.exe')
    Check ((Run-Installer $oldSetup '01-install.log' $installDir) -eq 0) 'First install succeeds in a Chinese and space-containing path'
    $installedExe = Join-Path $installDir 'Catcon.exe'
    Check ((Hash $installedExe) -eq (Hash (Join-Path $oldRelease 'Catcon.exe'))) 'Initial installed executable matches release payload'
    Check ((Get-ItemProperty -LiteralPath $keyPath).DisplayVersion -eq $BaselineVersion) 'Per-user uninstall entry has the correct version'
    $shortcuts = @(Get-ChildItem -LiteralPath (Join-Path $installDir 'Test Shortcuts') -Filter '*.lnk')
    Check ($shortcuts.Count -eq 2) 'Start and uninstall shortcuts are created'
    $shell = New-Object -ComObject WScript.Shell
    Check (@($shortcuts | Where-Object { $shell.CreateShortcut($_.FullName).TargetPath -eq $installedExe }).Count -eq 1) 'Start shortcut targets the installed executable'
    $dataRoot = Join-Path $installDir 'Test User Data'
    $customSkin = Join-Path $installDir 'assets/skins/my-custom-cat/skin.json'
    New-Item -ItemType Directory -Force -Path $dataRoot,(Split-Path -Parent $customSkin) | Out-Null
    $testSettings = Join-Path $dataRoot 'settings.json'
    [IO.File]::WriteAllText($testSettings,'{"Scale":3,"X":123,"Y":456,"SkinId":"my-custom-cat"}')
    Copy-Item -LiteralPath (Join-Path $installDir 'assets/skins/mikan/skin.json') -Destination $customSkin
    $settingsHash = Hash $testSettings
    $skinHash = Hash $customSkin
    $modified = @{}
    foreach ($relative in @('config/behaviors.json','config/classifiers.json','assets/skins/mikan/skin.json')) {
        $path = Join-Path $installDir $relative
        [IO.File]::AppendAllText($path,"`n")
        $modified[$relative] = Hash $path
    }
    Check ((Run-Installer $newSetup '02-upgrade.log' $null) -eq 0) 'Upgrade automatically reuses the existing installation directory'
    Check ((Hash $installedExe) -eq (Hash (Join-Path $newRelease 'Catcon.exe'))) 'Upgrade replaces the executable with the new version'
    Check ((Get-ItemProperty -LiteralPath $keyPath).DisplayVersion -eq $CurrentVersion) 'Upgrade updates the same uninstall entry'
    Check ((Hash $testSettings) -eq $settingsHash -and (Hash $customSkin) -eq $skinHash) 'Upgrade preserves user preferences and custom skins'
    $backups = @(Get-ChildItem -LiteralPath (Join-Path $dataRoot 'Backups') -Directory)
    Check ($backups.Count -eq 1) 'Upgrade creates one backup of the old editable data'
    foreach ($relative in $modified.Keys) {
        Check ((Hash (Join-Path $backups[0].FullName $relative)) -eq $modified[$relative]) ('Backup preserves the previous file exactly: ' + $relative)
        Check ((Hash (Join-Path $installDir $relative)) -eq (Hash (Join-Path $newRelease $relative))) ('Upgrade deploys the new default file: ' + $relative)
    }
    $report = Join-Path $testRoot 'installed-app-tests.json'
    $process = Start-Process -FilePath $installedExe -ArgumentList @('--self-test',('"' + $report + '"')) -WindowStyle Hidden -PassThru -Wait
    $appTests = Get-Content -LiteralPath $report -Encoding UTF8 -Raw | ConvertFrom-Json
    Check ($process.ExitCode -eq 0 -and $appTests.passed) ('Installed application passes ' + $appTests.checks + ' regression checks')
    Check ((Run-Installer $newSetup '03-repair.log' $null) -eq 0) 'Reinstalling the same version repairs the installation'
    Check ((Hash $testSettings) -eq $settingsHash -and (Hash $customSkin) -eq $skinHash) 'Repair preserves preferences and custom skins'
    Check (@(Get-ChildItem -LiteralPath (Join-Path $dataRoot 'Backups') -Directory).Count -eq 2) 'Repair retains the earlier backup and creates a separate backup'
    Check ((Run-Installer $oldSetup '04-downgrade-blocked.log' $null) -ne 0) 'An older installer cannot downgrade the current installation'
    Check ((Hash $installedExe) -eq (Hash (Join-Path $newRelease 'Catcon.exe'))) 'Rejected downgrade leaves the installed executable unchanged'
    Check ((Run-Installer (Join-Path $installDir 'unins000.exe') '05-uninstall.log' $null) -eq 0) 'Uninstallation succeeds'
    for ($i=0;$i -lt 50 -and (Test-Path -LiteralPath $installedExe);$i++) { Start-Sleep -Milliseconds 100 }
    Check (-not (Test-Path -LiteralPath $installedExe) -and -not (Test-Path -LiteralPath $keyPath)) 'Uninstall removes managed application files and its registry entry'
    Check ((Hash $testSettings) -eq $settingsHash -and (Hash $customSkin) -eq $skinHash) 'Uninstall preserves preferences and user-added skins'
    Check (@(Get-ChildItem -LiteralPath (Join-Path $dataRoot 'Backups') -Directory).Count -eq 2) 'Uninstall preserves upgrade backups'
    $upgradeLog = [IO.File]::ReadAllText((Join-Path $testRoot '02-upgrade.log'))
    Check ($upgradeLog.Contains((Join-Path $dataRoot 'Backups')) -and [IO.Path]::GetFullPath($dataRoot).StartsWith([IO.Path]::GetFullPath($testRoot) + '\',[StringComparison]::OrdinalIgnoreCase)) 'Installer test data and upgrade backups are confined to the isolated workspace directory'
    $success = $true
} finally {
    if (Test-Path -LiteralPath $keyPath) {
        $registeredPath = (Get-ItemProperty -LiteralPath $keyPath).InstallLocation
        if ([IO.Path]::GetFullPath($registeredPath).TrimEnd('\') -ne [IO.Path]::GetFullPath($installDir).TrimEnd('\')) { throw 'Unexpected installer test path; cleanup stopped.' }
        $uninstaller = Join-Path $installDir 'unins000.exe'
        if (Test-Path -LiteralPath $uninstaller) { [void](Run-Installer $uninstaller 'cleanup-uninstall.log' $null) }
    }
    $result = [PSCustomObject]@{passed=$success;checks=$checks.Count;verified=$checks.ToArray();artifacts=$testRoot}
    $result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $projectRoot 'artifacts/installer-test-results.json') -Encoding UTF8
    $result | Select-Object passed,checks,artifacts | ConvertTo-Json -Compress
}
