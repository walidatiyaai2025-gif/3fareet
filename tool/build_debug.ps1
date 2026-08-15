$ErrorActionPreference = 'Stop'
flutter pub get
if ($LASTEXITCODE -ne 0) { throw 'flutter pub get failed.' }
flutter build apk --debug
if ($LASTEXITCODE -ne 0) { throw 'Flutter debug APK build failed.' }
Copy-Item `
    -LiteralPath 'build\app\outputs\flutter-apk\app-debug.apk' `
    -Destination 'build\app\outputs\flutter-apk\afareet-flutter-debug.apk' `
    -Force
Write-Host 'Flutter APK: build\app\outputs\flutter-apk\afareet-flutter-debug.apk'
