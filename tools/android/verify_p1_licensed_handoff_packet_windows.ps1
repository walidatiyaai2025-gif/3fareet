param(
    [Parameter(Mandatory = $true)]
    [string]$Packet,
    [Parameter(Mandatory = $true)]
    [string]$HeroSource,
    [string]$RepoRoot = "",
    [string]$Output = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ExpectedUnityVersion = "6000.5.8f1"
$ExpectedTasks = @('UART-003', 'UART-004', 'UART-005', 'UART-006', 'UART-007', 'URAC-011')
$ExpectedChainPath = 'tools/android/p1_operator_release_chain.json'

function Fail([string]$Message) {
    throw "AFAREET_P1_NATIVE_HANDOFF_VERIFY_ERROR: $Message"
}

function Require-False($Value, [string]$Label) {
    if ($Value -ne $false) {
        Fail "$Label must remain JSON false."
    }
}

function Normalize-HeroRepoPath([string]$Value) {
    $normalized = ($Value.Trim().Trim('"') -replace '\\', '/')
    while ($normalized.StartsWith('./', [System.StringComparison]::Ordinal)) {
        $normalized = $normalized.Substring(2)
    }
    if ($normalized.StartsWith('Assets/', [System.StringComparison]::Ordinal)) {
        $normalized = 'unity_game/' + $normalized
    }
    return $normalized
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
} else {
    $RepoRoot = (Resolve-Path $RepoRoot).Path
}

$git = Get-Command git -ErrorAction SilentlyContinue
if ($null -eq $git) {
    Fail "git is required for exact source-SHA handoff verification."
}

$gitTopRaw = (& $git.Source -C $RepoRoot rev-parse --show-toplevel 2>$null | Select-Object -First 1)
$gitTopSucceeded = $?
if (-not $gitTopSucceeded -or [string]::IsNullOrWhiteSpace($gitTopRaw)) {
    Fail "Unable to resolve Git worktree root: $RepoRoot"
}
$gitTop = (Resolve-Path $gitTopRaw.Trim()).Path
if (-not [string]::Equals($gitTop, $RepoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    Fail "RepoRoot must be the exact Git worktree root. resolved=$gitTop requested=$RepoRoot"
}

$headRaw = (& $git.Source -C $RepoRoot rev-parse HEAD 2>$null | Select-Object -First 1)
$headSucceeded = $?
$headSha = if ([string]::IsNullOrWhiteSpace($headRaw)) { "" } else { $headRaw.Trim().ToLowerInvariant() }
if (-not $headSucceeded -or $headSha -notmatch '^[0-9a-f]{40}$') {
    Fail "Unable to resolve a full 40-character Git HEAD SHA."
}

$dirty = @(& $git.Source -C $RepoRoot status --porcelain --untracked-files=all 2>$null)
$dirtySucceeded = $?
if (-not $dirtySucceeded) {
    Fail "Unable to inspect Git working tree cleanliness."
}
if ($dirty.Count -gt 0) {
    $dirty | ForEach-Object { Write-Warning "HANDOFF_TREE_DIRTY $_" }
    Fail "Licensed handoff verification requires a clean Git working tree."
}

$packetPath = if ([System.IO.Path]::IsPathRooted($Packet)) {
    $Packet
} else {
    Join-Path $RepoRoot $Packet
}
if (-not (Test-Path -LiteralPath $packetPath -PathType Leaf)) {
    Fail "Handoff packet is missing: $packetPath"
}
$packetPath = (Resolve-Path -LiteralPath $packetPath).Path
$artifactRoot = (Join-Path $RepoRoot 'artifacts')
$artifactPrefix = $artifactRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
if (-not $packetPath.StartsWith($artifactPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    Fail "Handoff packet must be under <repo>/artifacts/: $packetPath"
}

try {
    $handoff = Get-Content -Raw -LiteralPath $packetPath | ConvertFrom-Json
} catch {
    Fail "Handoff packet is not valid JSON: $($_.Exception.Message)"
}

if ($handoff.schemaVersion -ne 2 -or $handoff.state -ne 'READY_FOR_LICENSED_OPERATOR_HANDOFF') {
    Fail "Handoff packet must be schemaVersion=2 and state=READY_FOR_LICENSED_OPERATOR_HANDOFF."
}
if ($handoff.releaseHandoffEligible -ne $true) {
    Fail "Handoff packet must explicitly declare releaseHandoffEligible=true before licensed staging."
}
if ($handoff.fixedRegisterSize -ne 65) {
    Fail "Handoff packet fixedRegisterSize must remain 65."
}
if ($handoff.expectedUnityVersion -ne $ExpectedUnityVersion) {
    Fail "Handoff packet Unity version mismatch. expected=$ExpectedUnityVersion actual=$($handoff.expectedUnityVersion)"
}

foreach ($field in @(
    'licensedUnityExecuted',
    'candidateBuildStarted',
    'physicalDeviceEvidenceCaptured',
    'humanApprovalRecorded',
    'publicationEligible',
    'publicationPerformed',
    'verified',
    'runtimeVerified',
    'ownerAccepted'
)) {
    Require-False $handoff.$field "handoff.$field"
}

$identity = $handoff.gitIdentity
if ($null -eq $identity) {
    Fail "Handoff packet is missing gitIdentity."
}
if ($identity.status -ne 'EXACT_SOURCE_SHA' -or
    $identity.gitIdentityMatched -ne $true -or
    $identity.syntheticPullRequestMerge -ne $false -or
    $identity.exactSourceIdentitySatisfied -ne $true) {
    Fail "Handoff packet Git identity is not exact/non-synthetic. status=$($identity.status)"
}
$observedSha = ([string]$identity.observedGitSha).ToLowerInvariant()
$expectedSha = ([string]$identity.expectedGitSha).ToLowerInvariant()
if ($observedSha -notmatch '^[0-9a-f]{40}$' -or $expectedSha -notmatch '^[0-9a-f]{40}$') {
    Fail "Handoff packet observed/expected Git SHAs must be full 40-character values."
}
if ($observedSha -ne $expectedSha -or $observedSha -ne $headSha -or ([string]$handoff.gitSha).ToLowerInvariant() -ne $headSha) {
    Fail "Handoff packet Git identity does not match current HEAD. observed=$observedSha expected=$expectedSha head=$headSha packet=$($handoff.gitSha)"
}

$expectedHero = Normalize-HeroRepoPath $HeroSource
$packetHero = Normalize-HeroRepoPath ([string]$handoff.heroSource)
if ([string]::IsNullOrWhiteSpace($expectedHero) -or $expectedHero -notmatch '^unity_game/Assets/Afareet/ArtSource/Vehicles/HeroCar/.+') {
    Fail "HeroSource must resolve under unity_game/Assets/Afareet/ArtSource/Vehicles/HeroCar/."
}
if ($packetHero -ne $expectedHero) {
    Fail "Handoff packet Hero source mismatch. expected=$expectedHero actual=$packetHero"
}
if ($packetHero -match '(?i)/(Generated|Preview|Blockout|Rivals)/') {
    Fail "Handoff packet Hero source cannot use generated/preview/blockout/rival paths."
}
$heroAbsolute = Join-Path $RepoRoot ($packetHero -replace '/', [System.IO.Path]::DirectorySeparatorChar)
if (-not (Test-Path -LiteralPath $heroAbsolute -PathType Leaf)) {
    Fail "Handoff packet Hero source file is missing: $packetHero"
}
& $git.Source -C $RepoRoot ls-files --error-unmatch -- $packetHero *> $null
$heroTracked = $?
if (-not $heroTracked) {
    Fail "Handoff packet Hero source must be tracked at current HEAD: $packetHero"
}

$visual = $handoff.visualSourceSummary
if ($null -eq $visual -or $visual.sourceReadyCount -ne 6 -or $visual.blockedCount -ne 0) {
    Fail "Handoff packet must contain six source-ready visual/runtime tasks and zero source blockers."
}
$blockedIds = @($visual.blockedTaskIds)
if ($blockedIds.Count -ne 0) {
    Fail "Handoff packet visual blockedTaskIds must be empty before licensed staging."
}
$taskRecords = @($visual.tasks)
if ($taskRecords.Count -ne $ExpectedTasks.Count) {
    Fail "Handoff packet must contain exactly six visual/runtime task records."
}
foreach ($taskId in $ExpectedTasks) {
    $records = @($taskRecords | Where-Object { $_.taskId -eq $taskId })
    if ($records.Count -ne 1) {
        Fail "Handoff packet requires exactly one task record for $taskId. actual=$($records.Count)"
    }
    $record = $records[0]
    if ($record.sourceReady -ne $true) {
        Fail "Handoff task is not source-ready: $taskId"
    }
    if (@($record.blockedCheckIds).Count -ne 0) {
        Fail "Handoff task has source blockers: $taskId"
    }
    Require-False $record.verified "handoff.task[$taskId].verified"
    Require-False $record.runtimeVerified "handoff.task[$taskId].runtimeVerified"
    Require-False $record.ownerAccepted "handoff.task[$taskId].ownerAccepted"
}

$staging = $handoff.licensedStagingSummary
if ($null -eq $staging -or
    $staging.state -ne 'READY_FOR_LICENSED_STAGING' -or
    $staging.readyForLicensedStaging -ne $true -or
    @($staging.blockedCheckIds).Count -ne 0) {
    Fail "Handoff packet licensedStagingSummary is not fully ready."
}

$chain = $handoff.operatorChain
if ($null -eq $chain -or
    $chain.file -ne $ExpectedChainPath -or
    $chain.stageCount -ne 13 -or
    $chain.authoritativeForP1 -ne $true) {
    Fail "Handoff packet operator-chain identity is invalid."
}
$chainAbsolute = Join-Path $RepoRoot ($ExpectedChainPath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
if (-not (Test-Path -LiteralPath $chainAbsolute -PathType Leaf)) {
    Fail "Authoritative operator chain is missing: $ExpectedChainPath"
}
$actualChainSha = (Get-FileHash -Algorithm SHA256 -LiteralPath $chainAbsolute).Hash.ToLowerInvariant()
$packetChainSha = ([string]$chain.sha256).ToLowerInvariant()
if ($packetChainSha -notmatch '^[0-9a-f]{64}$' -or $packetChainSha -ne $actualChainSha) {
    Fail "Handoff packet operator-chain SHA-256 mismatch. expected=$actualChainSha actual=$packetChainSha"
}

$packetSha = (Get-FileHash -Algorithm SHA256 -LiteralPath $packetPath).Hash.ToLowerInvariant()
if ([string]::IsNullOrWhiteSpace($Output)) {
    $Output = Join-Path $RepoRoot 'artifacts\production-staging\p1-native-handoff-verification.json'
} elseif (-not [System.IO.Path]::IsPathRooted($Output)) {
    $Output = Join-Path $RepoRoot $Output
}
$outputFull = [System.IO.Path]::GetFullPath($Output)
if (-not $outputFull.StartsWith($artifactPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    Fail "Native handoff verification output must stay under <repo>/artifacts/."
}
$outputDir = Split-Path -Parent $outputFull
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
if (Test-Path -LiteralPath $outputFull) {
    Fail "Refusing to overwrite existing native handoff verification report: $outputFull"
}

$result = [ordered]@{
    schemaVersion = 1
    state = 'NATIVE_P1_HANDOFF_VERIFIED_FOR_LICENSED_STAGING'
    gitSha = $headSha
    heroSource = $packetHero
    packetFile = [System.IO.Path]::GetFileName($packetPath)
    packetSha256 = $packetSha
    operatorChainSha256 = $actualChainSha
    sourceReadyCount = 6
    licensedStagingReady = $true
    releaseHandoffEligible = $true
    licensedUnityExecuted = $false
    candidateBuildStarted = $false
    publicationEligible = $false
    publicationPerformed = $false
    verified = $false
    runtimeVerified = $false
    ownerAccepted = $false
}
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $outputFull -Encoding UTF8
Write-Host "AFAREET_P1_NATIVE_HANDOFF_VERIFY_OK gitSha=$headSha heroSource=$packetHero packetSha256=$packetSha sourceReady=6 licensedUnityExecuted=false verified=false output=$outputFull"
