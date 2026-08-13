$ErrorActionPreference = 'Stop'

if (-not (Test-Path 'android\app\src\main\res')) {
    throw 'Android scaffold is missing. Run tool/bootstrap_android.ps1 first.'
}

$icons = @{
    'mipmap-mdpi'    = '48'
    'mipmap-hdpi'    = '72'
    'mipmap-xhdpi'   = '96'
    'mipmap-xxhdpi'  = '144'
    'mipmap-xxxhdpi' = '192'
}

foreach ($entry in $icons.GetEnumerator()) {
    $targetDirectory = Join-Path 'android\app\src\main\res' $entry.Key
    New-Item -ItemType Directory -Force -Path $targetDirectory | Out-Null
    Copy-Item `
        -LiteralPath "assets\branding\icons\afareet_app_icon_$($entry.Value).png" `
        -Destination (Join-Path $targetDirectory 'ic_launcher.png') `
        -Force
}

$manifestPath = 'android\app\src\main\AndroidManifest.xml'
$manifest = Get-Content -Raw $manifestPath
$manifest = $manifest -replace 'android:label="[^"]*"', 'android:label="Afareet Asphalt Flutter"'
Set-Content -LiteralPath $manifestPath -Value $manifest -Encoding utf8

Write-Host 'Flutter Android branding applied: Afareet Asphalt Flutter.'
