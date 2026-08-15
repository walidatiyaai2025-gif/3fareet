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
$MetadataPath = Join-Path $ArtifactDir "test-metadata.json"
$hadStaleMetadata = Test-Path $MetadataPath
Remove-Item -Force $MetadataPath -ErrorAction SilentlyContinue
Write-Host "AFAREET_STALE_TEST_METADATA_CLEARED present=$hadStaleMetadata path=$MetadataPath"

function Write-TestCaseDiagnostics([xml]$ResultXml, [string]$Mode, [string]$ResultPath) {
    $problemCases = @($ResultXml.SelectNodes("//test-case[@result='Failed' or @result='Inconclusive']"))
    if ($problemCases.Count -eq 0) {
        return
    }

    Write-Host "AFAREET_LOCAL_TEST_DIAGNOSTICS mode=$Mode count=$($problemCases.Count)"
    $limit = [Math]::Min($problemCases.Count, 25)
    for ($i = 0; $i -lt $limit; $i++) {
        $case = $problemCases[$i]
        $name = $case.GetAttribute('fullname')
        if ([string]::IsNullOrWhiteSpace($name)) { $name = $case.GetAttribute('name') }
        $result = $case.GetAttribute('result')

        $message = ""
        $stack = ""
        $messageNode = $case.SelectSingleNode('failure/message')
        $stackNode = $case.SelectSingleNode('failure/stack-trace')
        if ($null -ne $messageNode) { $message = [string]$messageNode.InnerText }
        if ($null -ne $stackNode) { $stack = [string]$stackNode.InnerText }

        $message = ($message -replace "\r?\n", " | ").Trim()
        $stackFirst = (($stack -split "\r?\n") | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1)
        Write-Host "AFAREET_TEST_CASE result=$result name=$name"
        if (-not [string]::IsNullOrWhiteSpace($message)) { Write-Host "  message=$message" }
        if (-not [string]::IsNullOrWhiteSpace($stackFirst)) { Write-Host "  stack=$stackFirst" }
    }
    if ($problemCases.Count -gt $limit) {
        Write-Host "AFAREET_TEST_CASE_MORE remaining=$($problemCases.Count - $limit) xml=$ResultPath"
    }
}

function Invoke-UnityTests([string]$Mode) {
    $modeKey = $Mode.ToLowerInvariant()
    $ResultPath = Join-Path $ArtifactDir "$modeKey-results.xml"
    $LogPath = Join-Path $LogDir "unity-$modeKey-tests.log"

    Remove-Item -Force $ResultPath -ErrorAction SilentlyContinue
    Remove-Item -Force $LogPath -ErrorAction SilentlyContinue

    # Do not pass -quit here. Unity Test Framework owns the lifecycle for
    # -runTests and exits the Editor after the run. With Unity 6000.5.8f1,
    # combining -quit with -runTests can make batchmode exit after project
    # initialization before the test run starts or writes the NUnit XML.
    $unityArgs = @(
        '-batchmode',
        '-projectPath', ('"{0}"' -f $ProjectPath),
        '-runTests',
        '-testPlatform', $Mode,
        '-testResults', ('"{0}"' -f $ResultPath),
        '-logFile', ('"{0}"' -f $LogPath)
    )

    Write-Host "AFAREET_LOCAL_TEST_START mode=$Mode gitSha=$GitSha branch=$GitBranch"
    $unityProcess = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -Wait -PassThru
    $unityExitCode = $unityProcess.ExitCode

    # Unity Test Framework commonly uses non-zero exit codes for a completed
    # test run that contains failed/inconclusive tests. If NUnit XML exists,
    # parse it first so the operator sees the exact failing test names/messages
    # instead of only a generic process exit code.
    if (-not (Test-Path $ResultPath)) {
        Get-Content $LogPath -Tail 160 -ErrorAction SilentlyContinue | Write-Host
        Fail "Unity $Mode tests exited with code $unityExitCode and did not create $ResultPath"
    }
    if ((Get-Item $ResultPath).Length -le 0) {
        Get-Content $LogPath -Tail 160 -ErrorAction SilentlyContinue | Write-Host
        Fail "Unity $Mode test result exists but is empty. exitCode=$unityExitCode path=$ResultPath"
    }

    try {
        [xml]$resultXml = Get-Content -Raw $ResultPath
    } catch {
        Get-Content $LogPath -Tail 160 -ErrorAction SilentlyContinue | Write-Host
        Fail "Unity $Mode result is not valid XML. exitCode=$unityExitCode error=$($_.Exception.Message)"
    }

    $testRun = $resultXml.'test-run'
    if ($null -eq $testRun) {
        Fail "Unity $Mode result is missing the NUnit test-run root. exitCode=$unityExitCode"
    }

    $total = 0
    $passed = 0
    $failed = 0
    $skipped = 0
    $inconclusive = 0
    if ($null -ne $testRun.total) { $total = [int]$testRun.total }
    if ($null -ne $testRun.passed) { $passed = [int]$testRun.passed }
    if ($null -ne $testRun.failed) { $failed = [int]$testRun.failed }
    if ($null -ne $testRun.skipped) { $skipped = [int]$testRun.skipped }
    if ($null -ne $testRun.inconclusive) { $inconclusive = [int]$testRun.inconclusive }
    $result = [string]$testRun.result
    $accounted = $passed + $failed + $skipped + $inconclusive

    Write-TestCaseDiagnostics -ResultXml $resultXml -Mode $Mode -ResultPath $ResultPath

    if ($total -le 0) {
        Fail "Unity $Mode executed zero tests; refusing empty test evidence. exitCode=$unityExitCode"
    }
    if ($passed -le 0) {
        Fail "Unity $Mode produced no passing tests; all-skipped/non-executed evidence is not release eligible. exitCode=$unityExitCode total=$total passed=$passed failed=$failed skipped=$skipped inconclusive=$inconclusive"
    }
    if ($failed -gt 0) {
        Fail "Unity $Mode tests contain failures. exitCode=$unityExitCode total=$total passed=$passed failed=$failed skipped=$skipped inconclusive=$inconclusive xml=$ResultPath"
    }
    if ($inconclusive -gt 0) {
        Fail "Unity $Mode tests contain inconclusive results. exitCode=$unityExitCode total=$total passed=$passed failed=$failed skipped=$skipped inconclusive=$inconclusive xml=$ResultPath"
    }
    if ($accounted -ne $total) {
        Fail "Unity $Mode test counters do not account for every test. exitCode=$unityExitCode total=$total accounted=$accounted passed=$passed failed=$failed skipped=$skipped inconclusive=$inconclusive"
    }
    if ($result -notin @('Passed', 'Success')) {
        Fail "Unity $Mode tests did not pass. exitCode=$unityExitCode result=$result total=$total passed=$passed failed=$failed skipped=$skipped inconclusive=$inconclusive"
    }
    if ($unityExitCode -ne 0) {
        Fail "Unity $Mode NUnit XML reports a passing run but Unity exited non-zero. exitCode=$unityExitCode total=$total passed=$passed failed=$failed skipped=$skipped inconclusive=$inconclusive"
    }

    Write-Host "AFAREET_LOCAL_TEST_OK mode=$Mode total=$total passed=$passed failed=$failed skipped=$skipped inconclusive=$inconclusive"

    return [ordered]@{
        mode = $Mode
        result = $result
        total = $total
        passed = $passed
        failed = $failed
        skipped = $skipped
        inconclusive = $inconclusive
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
$metadata | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 $MetadataPath

Write-Host "AFAREET_LOCAL_UNITY_TESTS_OK gitSha=$GitSha releaseEvidenceEligible=$ReleaseEvidenceEligible"
Write-Host "Evidence: $ArtifactDir"