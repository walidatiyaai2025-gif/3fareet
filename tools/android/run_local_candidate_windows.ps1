param(
    [string]$UnityPath = "",
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Fail([string]$Message) {
    throw "AFAREET_LOCAL_CANDIDATE_ERROR: $Message"
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
} else {
    $RepoRoot = (Resolve-Path $RepoRoot).Path
}

$testScript = Join-Path $RepoRoot "tools\android\test_current_windows.ps1"
$buildScript = Join-Path $RepoRoot "tools\android\build_current_windows.ps1"
$verifyScript = Join-Path $RepoRoot "tools\android\verify_local_candidate.py"
foreach ($required in @($testScript, $buildScript, $verifyScript)) {
    if (-not (Test-Path $required)) {
        Fail "Required candidate step is missing: $required"
    }
}

$git = Get-Command git -ErrorAction SilentlyContinue
if ($null -eq $git) {
    Fail "git is required for exact-SHA candidate evidence."
}

$gitSha = (& git -C $RepoRoot rev-parse HEAD 2>$null).Trim()
if ($LASTEXITCODE -ne 0 -or $gitSha -notmatch '^[0-9a-fA-F]{40}$') {
    Fail "Unable to resolve a full 40-character Git SHA."
}
$gitSha = $gitSha.ToLowerInvariant()

$initialDirty = @(& git -C $RepoRoot status --porcelain 2>$null)
if ($LASTEXITCODE -ne 0) {
    Fail "Unable to inspect the initial Git working tree."
}
if ($initialDirty.Count -gt 0) {
    Fail "Candidate orchestration requires a clean Git working tree before Unity starts."
}

function Assert-CleanTree([string]$Phase) {
    $changes = @(& git -C $RepoRoot status --porcelain 2>$null)
    if ($LASTEXITCODE -ne 0) {
        Fail "Unable to inspect Git state after $Phase."
    }
    if ($changes.Count -gt 0) {
        $changes | ForEach-Object { Write-Warning "${Phase}_DIRTY $_" }
        Fail "Unity/UPM changed repository content during $Phase. Reconcile and commit package/source changes, then restart from a clean exact head."
    }
}

$sharedArgs = @()
if (-not [string]::IsNullOrWhiteSpace($RepoRoot)) {
    $sharedArgs += @('-RepoRoot', $RepoRoot)
}
if (-not [string]::IsNullOrWhiteSpace($UnityPath)) {
    $sharedArgs += @('-UnityPath', $UnityPath)
}

Write-Host "AFAREET_LOCAL_CANDIDATE_START gitSha=$gitSha"

& $testScript @sharedArgs
Assert-CleanTree "UNITY_TESTS"

& $buildScript @sharedArgs
Assert-CleanTree "UNITY_BUILD"

$testMetadata = Join-Path $RepoRoot "artifacts\unity-local-tests\test-metadata.json"
$buildMetadata = Join-Path $RepoRoot "artifacts\android-local\artifact-metadata.json"
$apk = Join-Path $RepoRoot "artifacts\android-local\afareet-unity3d-debug.apk"
$manifest = Join-Path $RepoRoot "artifacts\local-candidate-manifest.json"

$python = Get-Command python -ErrorAction SilentlyContinue
$pythonArgs = @()
if ($null -eq $python) {
    $python = Get-Command py -ErrorAction SilentlyContinue
    if ($null -ne $python) {
        $pythonArgs += '-3'
    }
}
if ($null -eq $python) {
    Fail "Python 3 is required to run verify_local_candidate.py."
}

$verifyArgs = @(
    $verifyScript,
    '--test-metadata', $testMetadata,
    '--build-metadata', $buildMetadata,
    '--apk', $apk,
    '--output', $manifest
)
& $python.Source @pythonArgs @verifyArgs
if ($LASTEXITCODE -ne 0) {
    Fail "Local candidate integrity verification failed with exit code $LASTEXITCODE."
}

Assert-CleanTree "CANDIDATE_VERIFY"

if (-not (Test-Path $manifest) -or (Get-Item $manifest).Length -le 0) {
    Fail "Candidate verifier did not produce a non-empty manifest: $manifest"
}

Write-Host "AFAREET_LOCAL_CANDIDATE_OK gitSha=$gitSha manifest=$manifest"
Write-Host "Next: tools/android/prepare_candidate_device.py with this exact manifest/APK on a physical Android device."
