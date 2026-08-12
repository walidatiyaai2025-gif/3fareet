$ErrorActionPreference = 'Stop'
flutter pub get
flutter build apk --release --dart-define=BUILD_CHANNEL=prototype
Write-Host 'Release skeleton built. Do not place it in Last verified APK released until device smoke verification and release signing metadata are complete.'
