$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$downloads = Join-Path $projectRoot 'tools/downloads'
New-Item -ItemType Directory -Force -Path $downloads | Out-Null
function Get-VerifiedDownload($url,$name,$hash,$publisher) {
    $file = Join-Path $downloads $name
    if (-not (Test-Path -LiteralPath $file)) { Invoke-WebRequest -Uri $url -OutFile $file -UseBasicParsing }
    if ((Get-FileHash -LiteralPath $file).Hash -ne $hash) { throw ('Pinned download hash mismatch: ' + $name) }
    $signature = Get-AuthenticodeSignature -LiteralPath $file
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch [regex]::Escape($publisher)) { throw ('Publisher verification failed: ' + $name) }
    return $file
}
$inno = Get-VerifiedDownload 'https://github.com/jrsoftware/issrc/releases/download/is-6_7_3/innosetup-6.7.3.exe' 'innosetup-6.7.3.exe' '9C73C3BAE7ED48D44112A0F48E66742C00090BDB5BEF71D9D3C056C66E97B732' 'Pyrsys B.V.'
$runtime = Get-VerifiedDownload 'https://go.microsoft.com/fwlink/?LinkId=2085155' 'ndp48-web.exe' '0BBA3094588C4BFEC301939985222A20B340BF03431563DEC8B2B4478B06FFFA' 'Microsoft Corporation'
$compilerRoot = Join-Path $projectRoot 'tools/inno-6.7.3'
if (-not (Test-Path -LiteralPath (Join-Path $compilerRoot 'ISCC.exe'))) {
    $process = Start-Process -FilePath $inno -ArgumentList @('/PORTABLE=1','/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/CURRENTUSER','/NOICONS',('/DIR="' + $compilerRoot + '"')) -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw 'Inno Setup portable preparation failed.' }
}
Write-Output 'Installer compiler and Microsoft runtime bootstrapper are ready.'
