$ErrorActionPreference = 'Stop'

if (-not (Get-Command flutter -ErrorAction SilentlyContinue)) {
    throw 'Flutter is required on PATH.'
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("afareet-" + [guid]::NewGuid())
$scaffold = Join-Path $tempRoot 'afareet_scaffold'

try {
    flutter create --empty --no-pub --platforms=android --org com.fiftysolutions --project-name afareet_asphalt $scaffold
    if (Test-Path 'android') { Remove-Item 'android' -Recurse -Force }
    Copy-Item (Join-Path $scaffold 'android') 'android' -Recurse
    Write-Host 'Android scaffold generated. Run: flutter pub get; flutter build apk --debug'
}
finally {
    if (Test-Path $tempRoot) { Remove-Item $tempRoot -Recurse -Force }
}
