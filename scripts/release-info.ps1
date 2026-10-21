# Release identity helper. src/AssemblyInfo.cs is the single source of truth for the
# version, so directory / zip / installer names and checksum lists cannot drift apart.
# Keep this file ASCII-only: Windows PowerShell 5.1 reads BOM-less UTF-8 as ANSI.
function Get-CatconRelease {
    param([Parameter(Mandatory=$true)][string]$ProjectRoot)
    $info = [IO.File]::ReadAllText((Join-Path $ProjectRoot 'src/AssemblyInfo.cs'))
    $match = [regex]::Match($info, 'AssemblyVersion\("(\d+)\.(\d+)\.(\d+)')
    if (-not $match.Success) { throw 'AssemblyVersion is missing from src/AssemblyInfo.cs.' }
    $version = '{0}.{1}.{2}' -f $match.Groups[1].Value,$match.Groups[2].Value,$match.Groups[3].Value
    $name = 'Catcon-' + $version
    [pscustomobject]@{
        Version   = $version
        Name      = $name
        Directory = Join-Path $ProjectRoot ('dist/' + $name)
        Zip       = Join-Path $ProjectRoot ('dist/' + $name + '-win.zip')
        Setup     = Join-Path $ProjectRoot ('dist/' + $name + '-Setup.exe')
        Exe       = Join-Path $ProjectRoot ('dist/' + $name + '/Catcon.exe')
    }
}

function Get-CatconLatestRelease {
    param([Parameter(Mandatory=$true)][string]$ProjectRoot)
    Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'dist') -Directory |
        Where-Object { $_.Name -match '^Catcon-\d+\.\d+\.\d+$' } |
        Sort-Object { [version]($_.Name.Substring(7)) } -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}
