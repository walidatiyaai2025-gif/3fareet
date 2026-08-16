param(
    [string]$UnityPath = "",
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ExpectedUnityVersion = "6000.5.8f1"
$ExpectedPackage = "com.fiftysolutions.afareetunity3d"
$ExpectedAbi = "arm64-v8a"

function Fail([string]$Message) {
    throw "AFAREET_EXPERIMENTAL_BUILD_ERROR: $Message"
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
} else {
    $RepoRoot = (Resolve-Path $RepoRoot).Path
}

if ([string]::IsNullOrWhiteSpace($UnityPath)) {
    $defaultUnity = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$ExpectedUnityVersion\Editor\Unity.exe"
    if (Test-Path $defaultUnity) { $UnityPath = $defaultUnity }
}
if ([string]::IsNullOrWhiteSpace($UnityPath) -or -not (Test-Path $UnityPath -PathType Leaf)) {
    Fail "Unity $ExpectedUnityVersion was not found. Pass -UnityPath explicitly."
}
$UnityPath = (Resolve-Path $UnityPath).Path
if ($UnityPath -notmatch [regex]::Escape($ExpectedUnityVersion)) {
    Fail "Expected Unity $ExpectedUnityVersion, but path is: $UnityPath"
}

$ProjectPath = Join-Path $RepoRoot "unity_game"
if (-not (Test-Path $ProjectPath -PathType Container)) {
    Fail "Unity project is missing: $ProjectPath"
}

$git = Get-Command git -ErrorAction SilentlyContinue
if ($null -eq $git) { Fail "git is required for exact-SHA experimental evidence." }
$GitSha = (& $git.Source -C $RepoRoot rev-parse HEAD 2>$null).Trim().ToLowerInvariant()
if ($LASTEXITCODE -ne 0 -or $GitSha -notmatch '^[0-9a-f]{40}$') {
    Fail "Unable to resolve a full 40-character Git SHA."
}
$dirtyBefore = @(& $git.Source -C $RepoRoot status --porcelain 2>$null)
if ($LASTEXITCODE -ne 0) { Fail "Unable to inspect the initial Git worktree." }
if ($dirtyBefore.Count -gt 0) {
    $dirtyBefore | ForEach-Object { Write-Warning "INITIAL_TREE_DIRTY $_" }
    Fail "Experimental candidate must start from a clean exact checkout."
}

$androidPlayer = Join-Path (Split-Path $UnityPath -Parent) "Data\PlaybackEngines\AndroidPlayer"
if (-not (Test-Path $androidPlayer -PathType Container)) {
    Fail "Unity Android Build Support is not installed under: $androidPlayer"
}

$ArtifactDir = Join-Path $RepoRoot "artifacts\android-experimental"
$LogDir = Join-Path $RepoRoot "artifacts\logs"
New-Item -ItemType Directory -Force -Path $ArtifactDir, $LogDir | Out-Null
$LogPath = Join-Path $LogDir "unity-android-experimental.log"
$ApkPath = Join-Path $ProjectPath "Builds\Android\afareet-unity3d-experimental.apk"
$ArtifactApkPath = Join-Path $ArtifactDir "afareet-unity3d-experimental.apk"
$ArtifactMetadataPath = Join-Path $ArtifactDir "artifact-metadata.json"
$ShaPath = Join-Path $ArtifactDir "afareet-unity3d-experimental.apk.sha256"
$BadgingPath = Join-Path $ArtifactDir "aapt-badging.txt"

foreach ($stalePath in @($ApkPath, $ArtifactApkPath, $ArtifactMetadataPath, $ShaPath, $BadgingPath, $LogPath)) {
    Remove-Item -Force $stalePath -ErrorAction SilentlyContinue
}

Write-Host "AFAREET_EXPERIMENTAL_LOCAL_BUILD_START unity=$UnityPath gitSha=$GitSha"
$unityArgs = @(
    '-batchmode',
    '-quit',
    '-projectPath', ('"{0}"' -f $ProjectPath),
    '-executeMethod', 'Afareet.Editor.AfareetBuild.BuildAndroidExperimental',
    '-logFile', ('"{0}"' -f $LogPath)
)
$unityProcess = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -Wait -PassThru
if ($unityProcess.ExitCode -ne 0) {
    Get-Content $LogPath -Tail 180 -ErrorAction SilentlyContinue | Write-Host
    Fail "Unity exited with code $($unityProcess.ExitCode). See $LogPath"
}
if (-not (Test-Path $LogPath -PathType Leaf)) {
    Fail "Unity exited successfully but did not create the requested log: $LogPath"
}
if (-not (Select-String -Path $LogPath -SimpleMatch 'AFAREET_EXPERIMENTAL_APK_READY productionEvidence=false' -Quiet)) {
    Get-Content $LogPath -Tail 180 -ErrorAction SilentlyContinue | Write-Host
    Fail "Experimental APK success marker is missing."
}
if (-not (Test-Path $ApkPath -PathType Leaf) -or (Get-Item $ApkPath).Length -le 0) {
    Fail "Experimental APK is missing or empty: $ApkPath"
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
    if ($candidate) { $Aapt = $candidate; break }
}
if (-not $Aapt) { Fail "aapt.exe was not found in Android SDK build-tools." }

$badging = & $Aapt dump badging $ApkPath
if ($LASTEXITCODE -ne 0) { Fail "aapt dump badging failed for $ApkPath" }
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
    if (-not ($zip.Entries | Where-Object { $_.FullName -eq "lib/$ExpectedAbi/libunity.so" } | Select-Object -First 1)) {
        Fail "APK does not contain lib/$ExpectedAbi/libunity.so"
    }
} finally {
    $zip.Dispose()
}

$dirtyAfter = @(& $git.Source -C $RepoRoot status --porcelain 2>$null)
if ($LASTEXITCODE -ne 0) { Fail "Unable to inspect the post-build Git worktree." }
if ($dirtyAfter.Count -gt 0) {
    $dirtyAfter | ForEach-Object { Write-Warning "POST_BUILD_DIRTY $_" }
    Fail "Unity changed tracked/untracked non-ignored repository content during the experimental build."
}

$hash = (Get-FileHash -Algorithm SHA256 $ApkPath).Hash.ToLowerInvariant()
$size = (Get-Item $ApkPath).Length
"$hash  afareet-unity3d-experimental.apk" | Set-Content -Encoding ASCII $ShaPath
$metadata = [ordered]@{
    schemaVersion = 1
    source = "local-windows-licensed-unity-experimental"
    artifact = "afareet-unity3d-experimental.apk"
    artifactClass = "experimental"
    unityVersion = $ExpectedUnityVersion
    packageId = $ExpectedPackage
    minSdk = 26
    abi = $ExpectedAbi
    sha256 = $hash
    sizeBytes = $size
    gitSha = $GitSha
    releaseEvidenceEligible = $false
    physicalDeviceVerified = $false
    generatedAtUtc = [DateTime]::UtcNow.ToString("o")
    unityLog = $LogPath
}
$metadata | ConvertTo-Json -Depth 5 | Set-Content -Encoding UTF8 $ArtifactMetadataPath
Copy-Item -Force $ApkPath $ArtifactApkPath

Write-Host "AFAREET_EXPERIMENTAL_LOCAL_BUILD_OK package=$ExpectedPackage abi=$ExpectedAbi sha256=$hash size=$size releaseEvidenceEligible=false"
Write-Host "APK: $ArtifactApkPath"
