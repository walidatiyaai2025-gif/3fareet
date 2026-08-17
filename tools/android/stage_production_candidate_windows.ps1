param(
    [Parameter(Mandatory = $true)]
    [string]$HeroSource,
    [string]$UnityPath = "",
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ExpectedUnityVersion = "6000.5.8f1"

function Fail([string]$Message) {
    throw "AFAREET_P1_STAGING_HANDOFF_ERROR: $Message"
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
} else {
    $RepoRoot = (Resolve-Path $RepoRoot).Path
}

$git = Get-Command git -ErrorAction SilentlyContinue
if ($null -eq $git) {
    Fail "git is required for exact-SHA staging handoff."
}

$gitTop = (& $git.Source -C $RepoRoot rev-parse --show-toplevel 2>$null | Select-Object -First 1)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($gitTop)) {
    Fail "Unable to resolve Git worktree root: $RepoRoot"
}
$gitTop = (Resolve-Path $gitTop.Trim()).Path
if (-not [string]::Equals($gitTop, $RepoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    Fail "RepoRoot must be the exact Git worktree root. resolved=$gitTop requested=$RepoRoot"
}

$gitSha = (& $git.Source -C $RepoRoot rev-parse HEAD 2>$null | Select-Object -First 1).Trim()
if ($LASTEXITCODE -ne 0 -or $gitSha -notmatch '^[0-9a-fA-F]{40}$') {
    Fail "Unable to resolve a full 40-character Git SHA."
}
$gitSha = $gitSha.ToLowerInvariant()

$initialDirty = @(& $git.Source -C $RepoRoot status --porcelain --untracked-files=all 2>$null)
if ($LASTEXITCODE -ne 0) {
    Fail "Unable to inspect the initial Git working tree."
}
if ($initialDirty.Count -gt 0) {
    $initialDirty | ForEach-Object { Write-Warning "INITIAL_TREE_DIRTY $_" }
    Fail "Staging handoff requires a clean Git tree. Commit the real Hero source package first, then rerun."
}

$HeroSource = ($HeroSource.Trim().Trim('"') -replace '\\', '/')
if (-not $HeroSource.StartsWith('Assets/', [System.StringComparison]::Ordinal)) {
    Fail "HeroSource must be a Unity Assets/ path, for example Assets/Afareet/ArtSource/Vehicles/HeroCar/AfareetKing.fbx"
}
if ($HeroSource -match '(?i)/(Generated|Preview|Blockout|Rivals)/') {
    Fail "HeroSource cannot be under Generated, Preview, Blockout or Rivals: $HeroSource"
}
$extension = [System.IO.Path]::GetExtension($HeroSource).ToLowerInvariant()
if ($extension -notin @('.fbx', '.obj', '.blend', '.glb', '.gltf')) {
    Fail "Unsupported HeroSource extension: $extension"
}

$heroRepoRelative = ('unity_game/' + $HeroSource).Replace('//', '/')
$heroAbsolute = Join-Path $RepoRoot ($heroRepoRelative -replace '/', [System.IO.Path]::DirectorySeparatorChar)
if (-not (Test-Path -LiteralPath $heroAbsolute -PathType Leaf)) {
    Fail "Tracked Hero source file is missing: $heroRepoRelative"
}

& $git.Source -C $RepoRoot ls-files --error-unmatch -- $heroRepoRelative *> $null
if ($LASTEXITCODE -ne 0) {
    Fail "Hero source must already be tracked in the clean starting commit before licensed staging: $heroRepoRelative"
}

$ReportDir = Join-Path $RepoRoot "artifacts\production-staging"
New-Item -ItemType Directory -Force -Path $ReportDir | Out-Null
$NativeIntakeScript = Join-Path $RepoRoot "tools\android\validate_hero_asset_intake_windows.ps1"
$NativeIntakeReportPath = Join-Path $ReportDir "uart003-native-intake.json"
if (-not (Test-Path -LiteralPath $NativeIntakeScript -PathType Leaf)) {
    Fail "Mandatory UART-003 native intake script is missing: $NativeIntakeScript"
}
Remove-Item -Force $NativeIntakeReportPath -ErrorAction SilentlyContinue
Write-Host "AFAREET_P1_NATIVE_HERO_PREFLIGHT_START gitSha=$gitSha heroSource=$HeroSource"
& $NativeIntakeScript -Source $HeroSource -RepoRoot $RepoRoot -Output $NativeIntakeReportPath | ForEach-Object { Write-Host $_ }
if (-not (Test-Path -LiteralPath $NativeIntakeReportPath -PathType Leaf) -or (Get-Item $NativeIntakeReportPath).Length -le 0) {
    Fail "Mandatory UART-003 native intake did not produce a report: $NativeIntakeReportPath"
}
try {
    $nativeIntake = Get-Content -Raw -LiteralPath $NativeIntakeReportPath | ConvertFrom-Json
} catch {
    Fail "Mandatory UART-003 native intake report is not valid JSON: $($_.Exception.Message)"
}
if ($nativeIntake.schemaVersion -ne 1 -or $nativeIntake.task -ne 'UART-003') {
    Fail "Mandatory UART-003 native intake report has an unsupported schema/task."
}
if ($nativeIntake.source -ne $heroRepoRelative) {
    Fail "Mandatory UART-003 native intake source mismatch. expected=$heroRepoRelative actual=$($nativeIntake.source)"
}
if ($nativeIntake.verified -ne $false -or $nativeIntake.productionArtApproved -ne $false) {
    Fail "Native intake must never self-assert verified or production-art approval."
}
$expectedVerdict = if ($extension -eq '.obj') { 'READY_FOR_LICENSED_UNITY_IMPORT' } else { 'UNITY_INSPECTION_REQUIRED' }
if ($nativeIntake.verdict -ne $expectedVerdict) {
    Fail "Mandatory UART-003 native intake verdict mismatch. expected=$expectedVerdict actual=$($nativeIntake.verdict)"
}
Write-Host "AFAREET_P1_NATIVE_HERO_PREFLIGHT_OK gitSha=$gitSha verdict=$($nativeIntake.verdict) verified=false"

if ([string]::IsNullOrWhiteSpace($UnityPath)) {
    $defaultUnity = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$ExpectedUnityVersion\Editor\Unity.exe"
    if (Test-Path $defaultUnity) {
        $UnityPath = $defaultUnity
    }
}
if ([string]::IsNullOrWhiteSpace($UnityPath) -or -not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    Fail "Unity $ExpectedUnityVersion was not found. Pass -UnityPath 'C:\path\to\Unity.exe'."
}
$UnityPath = (Resolve-Path $UnityPath).Path
if ($UnityPath -notmatch [regex]::Escape($ExpectedUnityVersion)) {
    Fail "Expected Unity $ExpectedUnityVersion, but path is: $UnityPath"
}

$ProjectPath = Join-Path $RepoRoot "unity_game"
if (-not (Test-Path -LiteralPath $ProjectPath -PathType Container)) {
    Fail "Unity project is missing: $ProjectPath"
}

$LogDir = Join-Path $RepoRoot "artifacts\logs"
New-Item -ItemType Directory -Force -Path $LogDir, $ReportDir | Out-Null
$LogPath = Join-Path $LogDir "unity-production-staging-handoff.log"
$StatusPath = Join-Path $ReportDir "p1-staging-handoff.git-status.txt"
$ReportPath = Join-Path $ReportDir "p1-staging-handoff.json"
Remove-Item -Force $LogPath, $StatusPath, $ReportPath -ErrorAction SilentlyContinue

Write-Host "AFAREET_P1_STAGING_HANDOFF_START gitSha=$gitSha heroSource=$HeroSource unity=$UnityPath"

$unityArgs = @(
    '-batchmode',
    '-quit',
    '-projectPath', ('"{0}"' -f $ProjectPath),
    '-executeMethod', 'Afareet.Editor.P1ProductionCandidateStagingHandoff.StageForCommit',
    '-afareetHeroSource', ('"{0}"' -f $HeroSource),
    '-afareetGitSha', $gitSha,
    '-logFile', ('"{0}"' -f $LogPath)
)
$unityProcess = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -Wait -PassThru
$unityExitCode = $unityProcess.ExitCode
if ($unityExitCode -ne 0) {
    Get-Content $LogPath -Tail 200 -ErrorAction SilentlyContinue | Write-Host
    Fail "Unity staging handoff exited with code $unityExitCode. Review $LogPath and Git changes before retrying."
}

if (-not (Test-Path -LiteralPath $LogPath -PathType Leaf)) {
    Fail "Unity exited successfully but did not create the staging log: $LogPath"
}
if (-not (Select-String -Path $LogPath -SimpleMatch 'AFAREET_P1_STAGING_HANDOFF_OK' -Quiet -ErrorAction SilentlyContinue)) {
    Get-Content $LogPath -Tail 200 -ErrorAction SilentlyContinue | Write-Host
    Fail "Unity exited successfully but the staging success marker is missing."
}
if (-not (Test-Path -LiteralPath $ReportPath -PathType Leaf) -or (Get-Item $ReportPath).Length -le 0) {
    Fail "Unity staging handoff did not produce a non-empty report: $ReportPath"
}

try {
    $handoffReport = Get-Content -Raw -LiteralPath $ReportPath | ConvertFrom-Json
} catch {
    Fail "Unity staging handoff report is not valid JSON: $($_.Exception.Message)"
}
if ($handoffReport.schemaVersion -ne 2) {
    Fail "Unity staging handoff report schema mismatch. expected=2 actual=$($handoffReport.schemaVersion)"
}
if ($handoffReport.gitSha -ne $gitSha) {
    Fail "Unity staging handoff report Git SHA mismatch. expected=$gitSha actual=$($handoffReport.gitSha)"
}
if ($handoffReport.heroSource -ne $HeroSource) {
    Fail "Unity staging handoff report Hero source mismatch. expected=$HeroSource actual=$($handoffReport.heroSource)"
}
if ($handoffReport.state -ne 'STAGED_FOR_COMMIT_NOT_CANDIDATE' -or
    $handoffReport.verified -ne $false -or
    $handoffReport.runtimeVerified -ne $false -or
    $handoffReport.ownerAccepted -ne $false -or
    $handoffReport.publicationEligible -ne $false -or
    $handoffReport.candidateBuildStarted -ne $false) {
    Fail "Unity staging handoff report crossed the staging-only verification/publication boundary."
}
if ([string]::IsNullOrWhiteSpace([string]$handoffReport.heroSourceGuid) -or
    [string]::IsNullOrWhiteSpace([string]$handoffReport.heroPrefabGuid)) {
    Fail "Unity staging handoff report is missing UART-003 source/prefab GUID provenance."
}

$expectedTasks = @('UART-003', 'UART-004', 'UART-005', 'UART-006', 'UART-007', 'URAC-011')
$coveredTasks = @($handoffReport.coveredTasks)
if ($coveredTasks.Count -ne $expectedTasks.Count) {
    Fail "Unity staging handoff report must cover exactly six visual/runtime tasks. actual=$($coveredTasks.Count)"
}
foreach ($taskId in $expectedTasks) {
    if ($coveredTasks -notcontains $taskId) {
        Fail "Unity staging handoff report coveredTasks is missing $taskId."
    }
}

$taskEvidence = @($handoffReport.taskEvidence)
if ($taskEvidence.Count -ne $expectedTasks.Count) {
    Fail "Unity staging handoff report must contain exactly six task evidence records. actual=$($taskEvidence.Count)"
}
$expectedStates = @{
    'UART-003' = 'LICENSED_UNITY_STAGE_AND_BIND_OK'
    'UART-004' = 'LICENSED_UNITY_STAGE_AND_BIND_OK'
    'UART-005' = 'LICENSED_UNITY_IMPORT_STAGE_OK'
    'UART-006' = 'LICENSED_UNITY_IMPORT_STAGE_OK'
    'UART-007' = 'LICENSED_UNITY_IMPORT_STAGE_OK'
    'URAC-011' = 'LICENSED_UNITY_TRACKED_LAYOUT_IMPORT_OK'
}
foreach ($taskId in $expectedTasks) {
    $records = @($taskEvidence | Where-Object { $_.taskId -eq $taskId })
    if ($records.Count -ne 1) {
        Fail "Unity staging handoff report requires exactly one evidence record for $taskId. actual=$($records.Count)"
    }
    $record = $records[0]
    if ($record.state -ne $expectedStates[$taskId]) {
        Fail "Unity staging handoff evidence state mismatch for $taskId. expected=$($expectedStates[$taskId]) actual=$($record.state)"
    }
    if ([string]::IsNullOrWhiteSpace([string]$record.sourceEvidence) -or
        [string]::IsNullOrWhiteSpace([string]$record.runtimeEvidence)) {
        Fail "Unity staging handoff evidence is incomplete for $taskId."
    }
    if ($record.verified -ne $false -or $record.runtimeVerified -ne $false -or $record.ownerAccepted -ne $false) {
        Fail "Unity staging handoff task evidence must remain unverified/unaccepted for $taskId."
    }
}
Write-Host "AFAREET_P1_STAGING_REPORT_BINDING_OK gitSha=$gitSha tasks=6 verified=false runtimeVerified=false ownerAccepted=false"

$changes = @(& $git.Source -C $RepoRoot status --porcelain --untracked-files=all 2>$null)
if ($LASTEXITCODE -ne 0) {
    Fail "Unable to inspect Git changes after licensed staging."
}
$changes | Set-Content -Encoding UTF8 $StatusPath

function Resolve-ChangedPath([string]$StatusLine) {
    if ([string]::IsNullOrWhiteSpace($StatusLine) -or $StatusLine.Length -lt 4) { return "" }
    $path = $StatusLine.Substring(3).Trim()
    if ($path.Contains(' -> ')) {
        $path = ($path -split ' -> ')[-1]
    }
    return $path.Trim('"').Replace('\\', '/')
}

$disallowed = @()
foreach ($change in $changes) {
    $changedPath = Resolve-ChangedPath $change
    if ([string]::IsNullOrWhiteSpace($changedPath)) { continue }
    if (-not $changedPath.StartsWith('unity_game/Assets/', [System.StringComparison]::Ordinal)) {
        $disallowed += $changedPath
    }
}

if ($disallowed.Count -gt 0) {
    $disallowed | ForEach-Object { Write-Warning "DISALLOWED_STAGING_CHANGE $_" }
    Fail "Licensed staging changed files outside unity_game/Assets/. Do not commit blindly. Review $StatusPath and reset/reconcile unexpected changes."
}

if ($changes.Count -eq 0) {
    Write-Host "AFAREET_P1_STAGING_HANDOFF_OK gitSha=$gitSha changed=0 trackedCommitRequired=false verified=false report=$ReportPath"
    Write-Host "No tracked staging delta was produced. Do not infer acceptance; continue only after the remaining runtime/manifest gates are legitimately satisfied."
    exit 0
}

Write-Host "AFAREET_P1_STAGING_HANDOFF_OK gitSha=$gitSha changed=$($changes.Count) trackedCommitRequired=true verified=false report=$ReportPath status=$StatusPath"
Write-Host "AFAREET_P1_STAGING_COMMIT_REQUIRED Review the exact Assets/ changes, commit approved source/import metadata/prefabs, then run tools/android/run_local_candidate_windows.ps1 from the new clean SHA."
