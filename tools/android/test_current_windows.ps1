param(
    [string]$UnityPath = "",
    [string]$RepoRoot = "",
    [switch]$AllowDirty
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ExpectedUnityVersion = "6000.5.8f1"

function Fail([string]$Message) {
    throw "AFAREET_LOCAL_TEST_ERROR: $Message"
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
    Fail "git is required for SHA-pinned test evidence. Install Git and retry."
}

$GitSha = (& git -C $RepoRoot rev-parse HEAD 2>$null).Trim()
if ($LASTEXITCODE -ne 0 -or $GitSha -notmatch '^[0-9a-fA-F]{40}$') {
    Fail "Could not resolve a full 40-character Git commit SHA for $RepoRoot"
}
$GitSha = $GitSha.ToLowerInvariant()

$GitBranch = (& git -C $RepoRoot rev-parse --abbrev-ref HEAD 2>$null).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($GitBranch)) {
    Fail "Could not resolve the active Git branch."
}

$dirty = @(& git -C $RepoRoot status --porcelain 2>$null)
if ($LASTEXITCODE -ne 0) {
    Fail "Could not inspect Git working-tree state."
}
$IsDirty = $dirty.Count -gt 0
if (-not $AllowDirty -and $IsDirty) {
    Fail "Repository has uncommitted changes. Commit/stash them or use -AllowDirty for non-release debugging only."
}
$ReleaseEvidenceEligible = -not $IsDirty

$ArtifactDir = Join-Path $RepoRoot "artifacts\unity-local-tests"
$LogDir = Join-Path $RepoRoot "artifacts\logs"
New-Item -ItemType Directory -Force -Path $ArtifactDir, $LogDir | Out-Null

function Invoke-UnityTests([string]$Mode) {
    $modeKey = $Mode.ToLowerInvariant()
    $ResultPath = Join-Path $ArtifactDir "$modeKey-results.xml"
    $LogPath = Join-Path $LogDir "unity-$modeKey-tests.log"

    Remove-Item -Force $ResultPath -ErrorAction SilentlyContinue
    Remove-Item -Force $LogPath -ErrorAction SilentlyContinue

    $unityArgs = @(
        '-batchmode',
        '-quit',
        '-projectPath', ('"{0}"' -f $ProjectPath),
        '-runTests',
        '-testPlatform', $Mode,
        '-testResults', ('"{0}"' -f $ResultPath),
        '-logFile', ('"{0}"' -f $LogPath)
    )

    Write-Host "AFAREET_LOCAL_TEST_START mode=$Mode gitSha=$GitSha branch=$GitBranch"
    $unityProcess = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -Wait -PassThru
    $unityExitCode = $unityProcess.ExitCode

    if ($unityExitCode -ne 0) {
        Get-Content $LogPath -Tail 160 -ErrorAction SilentlyContinue | Write-Host
        Fail "Unity $Mode tests exited with code $unityExitCode. See $LogPath"
    }

    if (-not (Test-Path $ResultPath)) {
        Get-Content $LogPath -Tail 160 -ErrorAction SilentlyContinue | Write-Host
        Fail "Unity $Mode tests returned success but did not create $ResultPath"
    }
    if ((Get-Item $ResultPath).Length -le 0) {
        Fail "Unity $Mode test result exists but is empty: $ResultPath"
    }

    try {
        [xml]$resultXml = Get-Content -Raw $ResultPath
    } catch {
        Fail "Unity $Mode result is not valid XML: $($_.Exception.Message)"
    }

    $testRun = $resultXml.'test-run'
    if ($null -eq $testRun) {
        Fail "Unity $Mode result is missing the NUnit test-run root."
    }

    $total = 0
    $passed = 0
    $failed = 0
    $skipped = 0
    if ($null -ne $testRun.total) { $total = [int]$testRun.total }
    if ($null -ne $testRun.passed) { $passed = [int]$testRun.passed }
    if ($null -ne $testRun.failed) { $failed = [int]$testRun.failed }
    if ($null -ne $testRun.skipped) { $skipped = [int]$testRun.skipped }
    $result = [string]$testRun.result

    if ($total -le 0) {
        Fail "Unity $Mode executed zero tests; refusing empty test evidence."
    }
    if ($passed -le 0) {
        Fail "Unity $Mode produced no passing tests; all-skipped/non-executed evidence is not release eligible. total=$total passed=$passed failed=$failed skipped=$skipped"
    }
    if ($failed -gt 0 -or $result -notin @('Passed', 'Success')) {
        Fail "Unity $Mode tests did not pass. result=$result total=$total passed=$passed failed=$failed skipped=$skipped"
    }

    Write-Host "AFAREET_LOCAL_TEST_OK mode=$Mode total=$total passed=$passed failed=$failed skipped=$skipped"

    return [ordered]@{
        mode = $Mode
        result = $result
        total = $total
        passed = $passed
        failed = $failed
        skipped = $skipped
        resultXml = $ResultPath
        unityLog = $LogPath
    }
}

$editMode = Invoke-UnityTests 'EditMode'
$playMode = Invoke-UnityTests 'PlayMode'

$metadata = [ordered]@{
    schemaVersion = 1
    source = "local-windows-licensed-unity-tests"
    unityVersion = $ExpectedUnityVersion
    gitSha = $GitSha
    gitBranch = $GitBranch
    dirtyTree = $IsDirty
    releaseEvidenceEligible = $ReleaseEvidenceEligible
    generatedAtUtc = [DateTime]::UtcNow.ToString("o")
    editMode = $editMode
    playMode = $playMode
}
$metadataPath = Join-Path $ArtifactDir "test-metadata.json"
$metadata | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 $metadataPath

Write-Host "AFAREET_LOCAL_UNITY_TESTS_OK gitSha=$GitSha releaseEvidenceEligible=$ReleaseEvidenceEligible"
Write-Host "Evidence: $ArtifactDir"
