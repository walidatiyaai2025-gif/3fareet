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
$verifyScript = Join-Path $RepoRoot "tools\android\verify_local_candidate_windows.ps1"
$packageVerifyScript = Join-Path $RepoRoot "tools\android\verify_unity_package_lock_windows.ps1"
$textNormalizeScript = Join-Path $RepoRoot "tools\android\verify_unity_text_normalization_windows.ps1"
foreach ($required in @($testScript, $buildScript, $verifyScript, $packageVerifyScript, $textNormalizeScript)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        Fail "Required candidate step is missing: $required"
    }
}

$git = Get-Command git -ErrorAction SilentlyContinue
if ($null -eq $git) {
    Fail "git is required for exact-SHA candidate evidence."
}

$gitSha = (& $git.Source -C $RepoRoot rev-parse HEAD 2>$null).Trim()
if ($LASTEXITCODE -ne 0 -or $gitSha -notmatch '^[0-9a-fA-F]{40}$') {
    Fail "Unable to resolve a full 40-character Git SHA."
}
$gitSha = $gitSha.ToLowerInvariant()

function Preserve-DirtyTreeEvidence([string]$Phase, [string[]]$Changes) {
    $phaseKey = ($Phase.ToLowerInvariant() -replace '[^a-z0-9_-]', '-')
    $evidenceDir = Join-Path $RepoRoot "artifacts\logs"
    New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null

    $statusPath = Join-Path $evidenceDir "git-dirty-$phaseKey.status.txt"
    $patchPath = Join-Path $evidenceDir "git-dirty-$phaseKey.patch"
    $stderrPath = Join-Path $evidenceDir "git-dirty-$phaseKey.stderr.txt"

    $Changes | Set-Content -Encoding UTF8 $statusPath
    Remove-Item -Force $patchPath, $stderrPath -ErrorAction SilentlyContinue

    $gitArgs = @(
        '-C', ('"{0}"' -f $RepoRoot),
        'diff', '--binary', 'HEAD', '--'
    )
    $gitProcess = Start-Process `
        -FilePath $git.Source `
        -ArgumentList $gitArgs `
        -Wait `
        -PassThru `
        -RedirectStandardOutput $patchPath `
        -RedirectStandardError $stderrPath
    $patchExitCode = $gitProcess.ExitCode

    if ($patchExitCode -ne 0) {
        $stderr = ""
        if (Test-Path $stderrPath) {
            $stderr = (Get-Content -Raw $stderrPath -ErrorAction SilentlyContinue).Trim()
        }
        "Unable to capture git diff. exitCode=$patchExitCode stderr=$stderr" | Set-Content -Encoding UTF8 $patchPath
        Write-Warning "Unable to capture tracked dirty-tree patch for phase $Phase. exitCode=$patchExitCode stderr=$stderrPath"
    } elseif ((Test-Path $stderrPath) -and (Get-Item $stderrPath).Length -gt 0) {
        Write-Warning "Git emitted non-fatal stderr while preserving dirty-tree evidence for phase $Phase. See $stderrPath"
    }

    Write-Host "AFAREET_DIRTY_TREE_EVIDENCE phase=$Phase status=$statusPath patch=$patchPath stderr=$stderrPath"
}

function Assert-CleanTree([string]$Phase) {
    $changes = @(& $git.Source -C $RepoRoot status --porcelain 2>$null)
    if ($LASTEXITCODE -ne 0) {
        Fail "Unable to inspect Git state after $Phase."
    }
    if ($changes.Count -gt 0) {
        Preserve-DirtyTreeEvidence -Phase $Phase -Changes $changes
        $changes | ForEach-Object { Write-Warning "${Phase}_DIRTY $_" }
        Fail "Unity/UPM changed repository content during $Phase. Exact status/patch evidence was preserved under artifacts/logs. Reconcile and commit package/source changes, then restart from a clean exact head."
    }
}

function Clear-StaleCandidateEvidence {
    $stalePaths = @(
        (Join-Path $RepoRoot "artifacts\local-candidate-manifest.json"),
        (Join-Path $RepoRoot "artifacts\unity-local-tests\test-metadata.json"),
        (Join-Path $RepoRoot "artifacts\android-local\artifact-metadata.json"),
        (Join-Path $RepoRoot "artifacts\android-local\afareet-unity3d-debug.apk"),
        (Join-Path $RepoRoot "artifacts\android-local\afareet-unity3d-debug.apk.sha256"),
        (Join-Path $RepoRoot "artifacts\android-local\aapt-badging.txt")
    )

    $removed = 0
    foreach ($stalePath in $stalePaths) {
        if (Test-Path $stalePath) {
            Remove-Item -Force $stalePath
            $removed++
        }
    }

    Write-Host "AFAREET_STALE_CANDIDATE_EVIDENCE_CLEARED count=$removed"
}

# Preserve any prior dirty state before deleting stale ignored evidence. This
# keeps the fail-closed initial-tree behavior introduced by the hardened path.
$initialDirty = @(& $git.Source -C $RepoRoot status --porcelain 2>$null)
if ($LASTEXITCODE -ne 0) {
    Fail "Unable to inspect the initial Git working tree."
}
if ($initialDirty.Count -gt 0) {
    Preserve-DirtyTreeEvidence -Phase "INITIAL_TREE" -Changes $initialDirty
    $initialDirty | ForEach-Object { Write-Warning "INITIAL_TREE_DIRTY $_" }
    Fail "Candidate orchestration requires a clean Git working tree before Unity starts. Initial dirty-tree status/patch/stderr evidence was preserved under artifacts/logs before any cleanup."
}

Clear-StaleCandidateEvidence
Assert-CleanTree "STALE_EVIDENCE_PURGE"

# The licensed Windows candidate path is intentionally self-contained. Python
# verifiers remain available for hosted CI/Linux parity, but the Windows
# workstation uses native PowerShell equivalents and needs no Python install.
Write-Host "AFAREET_WINDOWS_NATIVE_VERIFIERS_OK pythonRequired=False"

Write-Host "AFAREET_TEXT_NORMALIZATION_PREFLIGHT_START gitSha=$gitSha"
& $textNormalizeScript -RepoRoot $RepoRoot
Assert-CleanTree "TEXT_NORMALIZATION_PREFLIGHT"
Write-Host "AFAREET_TEXT_NORMALIZATION_PREFLIGHT_OK gitSha=$gitSha"

$packageManifest = Join-Path $RepoRoot "unity_game\Packages\manifest.json"
$packageLock = Join-Path $RepoRoot "unity_game\Packages\packages-lock.json"
Write-Host "AFAREET_PACKAGE_PREFLIGHT_START gitSha=$gitSha"
& $packageVerifyScript -RepoRoot $RepoRoot -ManifestPath $packageManifest -LockPath $packageLock
Assert-CleanTree "PACKAGE_PREFLIGHT"
Write-Host "AFAREET_PACKAGE_PREFLIGHT_OK gitSha=$gitSha"

$sharedParams = @{
    RepoRoot = $RepoRoot
}
if (-not [string]::IsNullOrWhiteSpace($UnityPath)) {
    if (-not (Test-Path $UnityPath)) {
        Fail "The supplied UnityPath does not exist: $UnityPath"
    }
    $UnityPath = (Resolve-Path $UnityPath).Path
    $sharedParams.UnityPath = $UnityPath
}

Write-Host "AFAREET_LOCAL_CANDIDATE_START gitSha=$gitSha unity=$UnityPath"

& $testScript @sharedParams
Assert-CleanTree "UNITY_TESTS"

& $buildScript @sharedParams
Assert-CleanTree "UNITY_BUILD"

$testMetadata = Join-Path $RepoRoot "artifacts\unity-local-tests\test-metadata.json"
$buildMetadata = Join-Path $RepoRoot "artifacts\android-local\artifact-metadata.json"
$apk = Join-Path $RepoRoot "artifacts\android-local\afareet-unity3d-debug.apk"
$manifest = Join-Path $RepoRoot "artifacts\local-candidate-manifest.json"

& $verifyScript `
    -TestMetadata $testMetadata `
    -BuildMetadata $buildMetadata `
    -Apk $apk `
    -Output $manifest

Assert-CleanTree "CANDIDATE_VERIFY"

if (-not (Test-Path $manifest) -or (Get-Item $manifest).Length -le 0) {
    Fail "Candidate verifier did not produce a non-empty manifest: $manifest"
}

Write-Host "AFAREET_LOCAL_CANDIDATE_OK gitSha=$gitSha manifest=$manifest"
Write-Host "Next: candidate-bound physical Android device evidence. Python is not required for this Windows candidate chain."
