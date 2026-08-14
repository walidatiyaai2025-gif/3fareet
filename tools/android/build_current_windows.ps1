param(
    [string]$UnityPath = "",
    [string]$RepoRoot = "",
    [switch]$AllowDirty
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ExpectedUnityVersion = "6000.5.8f1"
$ExpectedPackage = "com.fiftysolutions.afareetunity3d"
$ExpectedAbi = "arm64-v8a"

function Fail([string]$Message) {
    throw "AFAREET_LOCAL_BUILD_ERROR: $Message"
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
} else {
    $RepoRoot = (Resolve-Path $RepoRoot).Path
}

if ([string]::IsNullOrWhiteSpace($UnityPath)) {
    $defaultUnity = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$ExpectedUnityVersion\Editor\Unity.exe"
    if (Test-Path $defaultUnity) {
        $UnityPath = $defaultUnity
    }
}

if ([string]::IsNullOrWhiteSpace($UnityPath) -or -not (Test-Path $UnityPath)) {
    Fail "Unity $ExpectedUnityVersion was not found. Pass -UnityPath 'C:\path\to\Unity.exe'."
}
$UnityPath = (Resolve-Path $UnityPath).Path

if ($UnityPath -notmatch [regex]::Escape($ExpectedUnityVersion)) {
    Fail "Expected Unity $ExpectedUnityVersion, but path is: $UnityPath"
}

$ProjectPath = Join-Path $RepoRoot "unity_game"
if (-not (Test-Path $ProjectPath)) {
    Fail "Unity project is missing: $ProjectPath"
}

$git = Get-Command git -ErrorAction SilentlyContinue
if ($null -eq $git) {
    Fail "git is required for SHA-pinned build evidence. Install Git and retry."
}

$gitShaOutput = & git -C $RepoRoot rev-parse HEAD 2>$null
if ($LASTEXITCODE -ne 0 -or $null -eq $gitShaOutput) {
    Fail "Unable to resolve Git HEAD for repository: $RepoRoot"
}
$GitSha = ($gitShaOutput | Select-Object -First 1).Trim()
if ($GitSha -notmatch '^[0-9a-fA-F]{40}$') {
    Fail "Git HEAD is not a full 40-character commit SHA: $GitSha"
}

$gitBranchOutput = & git -C $RepoRoot rev-parse --abbrev-ref HEAD 2>$null
if ($LASTEXITCODE -ne 0 -or $null -eq $gitBranchOutput) {
    Fail "Unable to resolve Git branch for repository: $RepoRoot"
}
$GitBranch = ($gitBranchOutput | Select-Object -First 1).Trim()

$dirty = @(& git -C $RepoRoot status --porcelain 2>$null)
if ($LASTEXITCODE -ne 0) {
    Fail "Unable to inspect Git working-tree state for repository: $RepoRoot"
}
$IsDirty = $dirty.Count -gt 0
if (-not $AllowDirty -and $IsDirty) {
    Fail "Repository has uncommitted changes. Commit/stash them or use -AllowDirty for non-release debugging only."
}
if ($AllowDirty -and $IsDirty) {
    Write-Warning "Dirty-tree override is active. The resulting artifact is debug evidence only and is not release-evidence eligible."
}

$androidPlayer = Join-Path (Split-Path $UnityPath -Parent) "Data\PlaybackEngines\AndroidPlayer"
if (-not (Test-Path $androidPlayer)) {
    Fail "Unity Android Build Support is not installed under: $androidPlayer"
}

$ArtifactDir = Join-Path $RepoRoot "artifacts\android-local"
$LogDir = Join-Path $RepoRoot "artifacts\logs"
New-Item -ItemType Directory -Force -Path $ArtifactDir, $LogDir | Out-Null
$LogPath = Join-Path $LogDir "unity-android-local.log"
$ApkPath = Join-Path $ProjectPath "Builds\Android\afareet-unity3d-debug.apk"

Write-Host "AFAREET_LOCAL_BUILD_START unity=$UnityPath project=$ProjectPath gitSha=$GitSha branch=$GitBranch dirty=$IsDirty"

$unityArgs = @(
    "-batchmode",
    "-quit",
    "-projectPath", $ProjectPath,
    "-executeMethod", "Afareet.Editor.AfareetBuild.BuildAndroid",
    "-logFile", $LogPath
)
& $UnityPath @unityArgs

if ($LASTEXITCODE -ne 0) {
    Get-Content $LogPath -Tail 120 -ErrorAction SilentlyContinue | Write-Host
    Fail "Unity exited with code $LASTEXITCODE. See $LogPath"
}

if (-not (Test-Path $ApkPath)) {
    Fail "Unity returned success but APK is missing: $ApkPath"
}
if ((Get-Item $ApkPath).Length -le 0) {
    Fail "APK exists but is empty: $ApkPath"
}

$sdkCandidates = @()
foreach ($value in @($env:AFAREET_ANDROID_SDK_ROOT, $env:ANDROID_SDK_ROOT, $env:ANDROID_HOME)) {
    if (-not [string]::IsNullOrWhiteSpace($value)) { $sdkCandidates += $value }
}
if (-not [string]::IsNullOrWhiteSpace($env:LOCALAPPDATA)) {
    $sdkCandidates += (Join-Path $env:LOCALAPPDATA "Android\Sdk")
}
$sdkCandidates += (Join-Path $androidPlayer "SDK")

$Aapt = $null
foreach ($sdk in $sdkCandidates | Select-Object -Unique) {
    if (-not (Test-Path $sdk)) { continue }
    $buildTools = Join-Path $sdk "build-tools"
    if (-not (Test-Path $buildTools)) { continue }
    $candidate = Get-ChildItem $buildTools -Directory -ErrorAction SilentlyContinue |
        Sort-Object Name -Descending |
        ForEach-Object { Join-Path $_.FullName "aapt.exe" } |
        Where-Object { Test-Path $_ } |
        Select-Object -First 1
    if ($candidate) {
        $Aapt = $candidate
        break
    }
}
if (-not $Aapt) {
    Fail "aapt.exe was not found in Android SDK build-tools."
}

$BadgingPath = Join-Path $ArtifactDir "aapt-badging.txt"
$badging = & $Aapt dump badging $ApkPath
if ($LASTEXITCODE -ne 0) {
    Fail "aapt dump badging failed for $ApkPath"
}
$badging | Set-Content -Encoding UTF8 $BadgingPath
$badgingText = $badging -join "`n"

if ($badgingText -notmatch "package: name='$([regex]::Escape($ExpectedPackage))'") {
    Fail "APK package id is not $ExpectedPackage"
}
if ($badgingText -notmatch "sdkVersion:'26'") {
    Fail "APK minSdk is not Android API 26"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($ApkPath)
try {
    $abis = @($zip.Entries |
        Where-Object { $_.FullName -match '^lib/([^/]+)/' } |
        ForEach-Object { [regex]::Match($_.FullName, '^lib/([^/]+)/').Groups[1].Value } |
        Sort-Object -Unique)
    if ($abis.Count -ne 1 -or $abis[0] -ne $ExpectedAbi) {
        Fail "Expected only ABI $ExpectedAbi; found: $($abis -join ', ')"
    }
    $hasUnity = $zip.Entries | Where-Object { $_.FullName -eq "lib/$ExpectedAbi/libunity.so" } | Select-Object -First 1
    if (-not $hasUnity) {
        Fail "APK does not contain lib/$ExpectedAbi/libunity.so"
    }
} finally {
    $zip.Dispose()
}

$hash = (Get-FileHash -Algorithm SHA256 $ApkPath).Hash.ToLowerInvariant()
$size = (Get-Item $ApkPath).Length
$shaPath = Join-Path $ArtifactDir "afareet-unity3d-debug.apk.sha256"
"$hash  afareet-unity3d-debug.apk" | Set-Content -Encoding ASCII $shaPath

$metadata = [ordered]@{
    schemaVersion = 1
    artifact = "afareet-unity3d-debug.apk"
    source = if ($IsDirty) { "local-windows-licensed-unity-dirty-debug" } else { "local-windows-licensed-unity" }
    unityVersion = $ExpectedUnityVersion
    packageId = $ExpectedPackage
    minSdk = 26
    abi = $ExpectedAbi
    sha256 = $hash
    sizeBytes = $size
    gitSha = $GitSha
    gitBranch = $GitBranch
    gitDirty = $IsDirty
    releaseEvidenceEligible = (-not $IsDirty)
    generatedAtUtc = [DateTime]::UtcNow.ToString("o")
    unityLog = $LogPath
}
$metadata | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 (Join-Path $ArtifactDir "artifact-metadata.json")

Copy-Item -Force $ApkPath (Join-Path $ArtifactDir "afareet-unity3d-debug.apk")

Write-Host "AFAREET_LOCAL_ANDROID_BUILD_OK package=$ExpectedPackage abi=$ExpectedAbi sha256=$hash size=$size gitSha=$GitSha releaseEligible=$(-not $IsDirty)"
Write-Host "APK: $ApkPath"
Write-Host "Evidence: $ArtifactDir"
