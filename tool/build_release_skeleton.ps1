$ErrorActionPreference = 'Stop'
flutter pub get
if ($LASTEXITCODE -ne 0) { throw 'flutter pub get failed.' }
flutter build apk --release --dart-define=BUILD_CHANNEL=prototype
if ($LASTEXITCODE -ne 0) { throw 'Flutter release skeleton build failed.' }
Copy-Item `
    -LiteralPath 'build\app\outputs\flutter-apk\app-release.apk' `
    -Destination 'build\app\outputs\flutter-apk\afareet-flutter-release-skeleton.apk' `
    -Force
Write-Host 'Release skeleton built. Do not place it in Last verified APK released until device smoke verification and release signing metadata are complete.'
