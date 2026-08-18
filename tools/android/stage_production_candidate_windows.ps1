param(
    [Parameter(Mandatory = $true)]
    [string]$HeroSource,
    [Parameter(Mandatory = $true)]
    [string]$HandoffPacketSha256,
    [Parameter(Mandatory = $true)]
    [string]$NativeHandoffVerificationSha256,
    [Parameter(Mandatory = $true)]
    [string]$OperatorChainSha256,
    [string]$UnityPath = "",
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ExpectedUnityVersion = "6000.5.8f1"

function Fail([string]$Message) {
    throw "AFAREET_P1_STAGING_HANDOFF_ERROR: $Message"
}

function Normalize-Sha256($Value, [string]$Label) {
    $sha = ([string]$Value).Trim().ToLowerInvariant()
    if ($sha -notmatch '^[0-9a-f]{64}$') {
        Fail "$Label must be a SHA-256 hex digest."
    }
    return $sha
}

function Assert-TrackedNonEmptyFile([string]$RepoRelativePath, [string]$Label) {
    $normalized = $RepoRelativePath.Replace('\\', '/')
    $absolute = Join-Path $RepoRoot ($normalized -replace '/', [System.IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) {
        Fail "$Label is missing: $normalized"
    }
    $size = (Get-Item -LiteralPath $absolute).Length
    if ($size -le 0) {
        Fail "$Label is empty: $normalized"
    }
    & $git.Source -C $RepoRoot ls-files --error-unmatch -- $normalized *> $null
    if (-not $?) {
        Fail "$Label must already be tracked in the clean starting commit before licensed staging: $normalized"
    }
    return $size
}

$HandoffPacketSha256 = Normalize-Sha256 $HandoffPacketSha256 'HandoffPacketSha256'
$NativeHandoffVerificationSha256 = Normalize-Sha256 $NativeHandoffVerificationSha256 'NativeHandoffVerificationSha256'
$OperatorChainSha256 = Normalize-Sha256 $OperatorChainSha256 'OperatorChainSha256'

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
$gitTopSucceeded = $?
if (-not $gitTopSucceeded -or [string]::IsNullOrWhiteSpace($gitTop)) {
    Fail "Unable to resolve Git worktree root: $RepoRoot"
}
$gitTop = (Resolve-Path $gitTop.Trim()).Path
if (-not [string]::Equals($gitTop, $RepoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    Fail "RepoRoot must be the exact Git worktree root. resolved=$gitTop requested=$RepoRoot"
}

$gitShaRaw = (& $git.Source -C $RepoRoot rev-parse HEAD 2>$null | Select-Object -First 1)
$gitShaSucceeded = $?
$gitSha = if ([string]::IsNullOrWhiteSpace($gitShaRaw)) { "" } else { $gitShaRaw.Trim() }
if (-not $gitShaSucceeded -or $gitSha -notmatch '^[0-9a-fA-F]{40}$') {
    Fail "Unable to resolve a full 40-character Git SHA."
}
$gitSha = $gitSha.ToLowerInvariant()

$initialDirty = @(& $git.Source -C $RepoRoot status --porcelain --untracked-files=all 2>$null)
$initialStatusSucceeded = $?
if (-not $initialStatusSucceeded) {
    Fail "Unable to inspect the initial Git working tree."
}
if ($initialDirty.Count -gt 0) {
    $initialDirty | ForEach-Object { Write-Warning "INITIAL_TREE_DIRTY $_" }
    Fail "Staging handoff requires a clean Git tree. Commit the real Hero and Rival production source packages plus Unity metadata first, then rerun."
}

$HeroSource = ($HeroSource.Trim().Trim('"') -replace '\\', '/')
if (-not $HeroSource.StartsWith('Assets/', [System.StringComparison]::Ordinal)) {
    Fail "HeroSource must be a Unity Assets/ path, for example Assets/Afareet/ArtSource/Vehicles/HeroCar/Production/AfareetKing.fbx"
}
if ($HeroSource -match '(?i)/(Generated|Preview|Refinement|RefinementCandidates|Blockout|Rivals|Review|ReviewPackaging)/') {
    Fail "HeroSource cannot be under Generated, Preview, Refinement, Blockout, Rivals or Review paths: $HeroSource"
}
$extension = [System.IO.Path]::GetExtension($HeroSource).ToLowerInvariant()
if ($extension -notin @('.fbx', '.obj', '.blend', '.glb', '.gltf')) {
    Fail "Unsupported HeroSource extension: $extension"
}

$heroRepoRelative = ('unity_game/' + $HeroSource).Replace('//', '/')
$heroBytes = Assert-TrackedNonEmptyFile $heroRepoRelative 'Hero production source'
$heroMetaRelative = $heroRepoRelative + '.meta'
$heroMetaBytes = Assert-TrackedNonEmptyFile $heroMetaRelative 'Hero production source Unity metadata'
Write-Host "AFAREET_STAGING_HERO_SOURCE_OK path=$heroRepoRelative bytes=$heroBytes meta=$heroMetaRelative metaBytes=$heroMetaBytes tracked=true"

$RivalProductionSources = @(
    'unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_01_WedgeCoupe_Production.obj',
    'unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_02_FastbackMuscle_Production.obj',
    'unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_03_CompactPrototype_Production.obj'
)
if ((@($RivalProductionSources | Select-Object -Unique)).Count -ne 3) {
    Fail "UART-004 staging requires exactly three distinct production Rival exchange paths."
}
for ($variant = 0; $variant -lt $RivalProductionSources.Count; $variant++) {
    $rivalRepoRelative = $RivalProductionSources[$variant]
    $rivalBytes = Assert-TrackedNonEmptyFile $rivalRepoRelative "Rival $($variant + 1) production source"
    $rivalMetaRelative = $rivalRepoRelative + '.meta'
    $rivalMetaBytes = Assert-TrackedNonEmptyFile $rivalMetaRelative "Rival $($variant + 1) production source Unity metadata"
    Write-Host "AFAREET_STAGING_RIVAL_SOURCE_OK variant=$($variant + 1) path=$rivalRepoRelative bytes=$rivalBytes meta=$rivalMetaRelative metaBytes=$rivalMetaBytes tracked=true"
}
Write-Host "AFAREET_STAGING_EXTERNAL_SOURCE_PREFLIGHT_OK hero=1 rivals=3 sourcesAndMetaTracked=true mutationStarted=false verified=false"

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

Write-Host "AFAREET_P1_STAGING_HANDOFF_START gitSha=$gitSha heroSource=$HeroSource packetSha256=$HandoffPacketSha256 unity=$UnityPath"

$unityArgs = @(
    '-batchmode',
    '-quit',
    '-projectPath', ('"{0}"' -f $ProjectPath),
    '-executeMethod', 'Afareet.Editor.P1ProductionCandidateStagingHandoff.StageForCommit',
    '-afareetHeroSource', ('"{0}"' -f $HeroSource),
    '-afareetGitSha', $gitSha,
    '-afareetHandoffPacketSha256', $HandoffPacketSha256,
    '-afareetNativeHandoffVerificationSha256', $NativeHandoffVerificationSha256,
    '-afareetOperatorChainSha256', $OperatorChainSha256,
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
if ($handoffReport.schemaVersion -ne 3) {
    Fail "Unity staging handoff report schema mismatch. expected=3 actual=$($handoffReport.schemaVersion)"
}
if ($handoffReport.gitSha -ne $gitSha) {
    Fail "Unity staging handoff report Git SHA mismatch. expected=$gitSha actual=$($handoffReport.gitSha)"
}
if ($handoffReport.heroSource -ne $HeroSource) {
    Fail "Unity staging handoff report Hero source mismatch. expected=$HeroSource actual=$($handoffReport.heroSource)"
}
if (([string]$handoffReport.handoffPacketSha256).ToLowerInvariant() -ne $HandoffPacketSha256 -or
    ([string]$handoffReport.nativeHandoffVerificationSha256).ToLowerInvariant() -ne $NativeHandoffVerificationSha256 -or
    ([string]$handoffReport.operatorChainSha256).ToLowerInvariant() -ne $OperatorChainSha256 -or
    ([string]$handoffReport.authorizationSourceGitSha).ToLowerInvariant() -ne $gitSha) {
    Fail "Unity staging handoff report authorization fingerprints do not match the native READY-packet authorization."
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
Write-Host "AFAREET_P1_STAGING_REPORT_BINDING_OK gitSha=$gitSha packetSha256=$HandoffPacketSha256 nativeVerificationSha256=$NativeHandoffVerificationSha256 operatorChainSha256=$OperatorChainSha256 tasks=6 verified=false runtimeVerified=false ownerAccepted=false"

$changes = @(& $git.Source -C $RepoRoot status --porcelain --untracked-files=all 2>$null)
$postStageStatusSucceeded = $?
if (-not $postStageStatusSucceeded) {
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
    Write-Host "AFAREET_P1_STAGING_HANDOFF_OK gitSha=$gitSha changed=0 trackedCommitRequired=false packetSha256=$HandoffPacketSha256 verified=false report=$ReportPath"
    Write-Host "No tracked staging delta was produced. Do not infer acceptance; continue only after the remaining runtime/manifest gates are legitimately satisfied."
    exit 0
}

Write-Host "AFAREET_P1_STAGING_HANDOFF_OK gitSha=$gitSha changed=$($changes.Count) trackedCommitRequired=true packetSha256=$HandoffPacketSha256 verified=false report=$ReportPath status=$StatusPath"
Write-Host "AFAREET_P1_STAGING_COMMIT_REQUIRED Review the exact Assets/ changes, commit approved source/import metadata/prefabs, then run tools/android/run_p1_staged_candidate_windows.ps1 from the new clean SHA."