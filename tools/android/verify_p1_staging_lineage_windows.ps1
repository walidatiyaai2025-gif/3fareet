param(
    [Parameter(Mandatory = $true)][string]$StagingReport,
    [string]$RepoRoot = "",
    [string]$Output = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Fail([string]$Message) {
    throw "AFAREET_P1_STAGING_LINEAGE_ERROR: $Message"
}

function Get-Value($Object, [string]$Name) {
    if ($null -eq $Object) { return $null }
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }
    return $property.Value
}

function Require-Bool($Object, [string]$Key, [bool]$Expected, [string]$Label) {
    $value = Get-Value $Object $Key
    if ($null -eq $value -or $value -isnot [bool] -or $value -ne $Expected) {
        Fail "$Label.$Key must be JSON boolean $($Expected.ToString().ToLowerInvariant()), found '$value'"
    }
}

function Normalize-Sha($Value, [string]$Label) {
    $sha = ([string]$Value).Trim().ToLowerInvariant()
    if ($sha -notmatch '^[0-9a-f]{40}$') {
        Fail "$Label must be a full 40-character Git SHA, found '$Value'"
    }
    return $sha
}

function Normalize-Sha256($Value, [string]$Label) {
    $sha = ([string]$Value).Trim().ToLowerInvariant()
    if ($sha -notmatch '^[0-9a-f]{64}$') {
        Fail "$Label must be a SHA-256 hex digest, found '$Value'"
    }
    return $sha
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
} else {
    $RepoRoot = (Resolve-Path $RepoRoot).Path
}

$git = Get-Command git -ErrorAction SilentlyContinue
if ($null -eq $git) { Fail "git is required for P1 staging lineage verification." }

$gitPrefix = (& $git.Source -C $RepoRoot rev-parse --show-prefix 2>$null | Select-Object -First 1)
$gitPrefixOk = $?
if (-not $gitPrefixOk) { Fail "Unable to inspect Git worktree prefix: $RepoRoot" }
if (-not [string]::IsNullOrWhiteSpace([string]$gitPrefix)) {
    Fail "RepoRoot must be the exact Git worktree root, not a subdirectory. prefix=$gitPrefix"
}
$gitTop = (& $git.Source -C $RepoRoot rev-parse --show-toplevel 2>$null | Select-Object -First 1)
$gitTopOk = $?
if (-not $gitTopOk -or [string]::IsNullOrWhiteSpace($gitTop)) {
    Fail "Unable to resolve Git worktree root: $RepoRoot"
}
$RepoRoot = (Resolve-Path $gitTop.Trim()).Path

$dirty = @(& $git.Source -C $RepoRoot status --porcelain --untracked-files=all 2>$null)
$dirtyStatusOk = $?
if (-not $dirtyStatusOk) { Fail "Unable to inspect candidate Git working tree." }
if ($dirty.Count -gt 0) {
    Fail "P1 staged candidate lineage requires a clean candidate worktree. Commit/reconcile all staging output first."
}

$candidateSha = (& $git.Source -C $RepoRoot rev-parse HEAD 2>$null | Select-Object -First 1)
$candidateShaOk = $?
if (-not $candidateShaOk) { Fail "Unable to resolve candidate HEAD." }
$candidateSha = Normalize-Sha $candidateSha 'candidateGitSha'

$StagingReport = (Resolve-Path -LiteralPath $StagingReport).Path
if (-not (Test-Path -LiteralPath $StagingReport -PathType Leaf) -or (Get-Item -LiteralPath $StagingReport).Length -le 0) {
    Fail "Staging report is missing or empty: $StagingReport"
}
try {
    $report = Get-Content -Raw -LiteralPath $StagingReport -Encoding UTF8 | ConvertFrom-Json
} catch {
    Fail "Staging report is not valid JSON: $($_.Exception.Message)"
}
if ($report.schemaVersion -ne 3) { Fail "Staging report schema must be 3, found '$($report.schemaVersion)'" }
if ($report.state -ne 'STAGED_FOR_COMMIT_NOT_CANDIDATE') { Fail "Unexpected staging report state: '$($report.state)'" }
Require-Bool $report 'verified' $false 'stagingReport'
Require-Bool $report 'runtimeVerified' $false 'stagingReport'
Require-Bool $report 'ownerAccepted' $false 'stagingReport'
Require-Bool $report 'publicationEligible' $false 'stagingReport'
Require-Bool $report 'candidateBuildStarted' $false 'stagingReport'

$stagingSourceSha = Normalize-Sha (Get-Value $report 'gitSha') 'stagingReport.gitSha'
$authorizationSourceSha = Normalize-Sha (Get-Value $report 'authorizationSourceGitSha') 'stagingReport.authorizationSourceGitSha'
if ($authorizationSourceSha -ne $stagingSourceSha) {
    Fail "Staging authorization source SHA must equal staging report gitSha. authorization=$authorizationSourceSha staging=$stagingSourceSha"
}
$handoffPacketSha256 = Normalize-Sha256 (Get-Value $report 'handoffPacketSha256') 'stagingReport.handoffPacketSha256'
$nativeHandoffVerificationSha256 = Normalize-Sha256 (Get-Value $report 'nativeHandoffVerificationSha256') 'stagingReport.nativeHandoffVerificationSha256'
$operatorChainSha256 = Normalize-Sha256 (Get-Value $report 'operatorChainSha256') 'stagingReport.operatorChainSha256'

if ($stagingSourceSha -eq $candidateSha) {
    Fail "P1 staged candidate must be a new reviewed commit after staging; stagingSourceGitSha equals candidateGitSha=$candidateSha"
}

& $git.Source -C $RepoRoot cat-file -e "$stagingSourceSha^{commit}" 2>$null
$stagingCommitExists = $?
if (-not $stagingCommitExists) { Fail "Staging source commit is not available in this Git repository: $stagingSourceSha" }

$parentLine = (& $git.Source -C $RepoRoot rev-list --parents -n 1 HEAD 2>$null | Select-Object -First 1)
$parentLineOk = $?
if (-not $parentLineOk -or [string]::IsNullOrWhiteSpace($parentLine)) {
    Fail "Unable to resolve candidate commit parent."
}
$parentParts = @($parentLine.Trim() -split '\s+')
if ($parentParts.Count -ne 2) {
    Fail "P1 staged candidate commit must have exactly one parent; merge/root commits are not accepted. parentFields=$($parentParts.Count)"
}
$directParentSha = Normalize-Sha $parentParts[1] 'candidateDirectParentSha'
if ($directParentSha -ne $stagingSourceSha) {
    Fail "Candidate must be the direct reviewed staging-output commit. stagingSource=$stagingSourceSha directParent=$directParentSha candidate=$candidateSha"
}

$changedPaths = @(& $git.Source -C $RepoRoot diff-tree --no-commit-id --name-only -r HEAD 2>$null | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$changedPathsOk = $?
if (-not $changedPathsOk) { Fail "Unable to inspect candidate staging commit paths." }
if ($changedPaths.Count -le 0) { Fail "Candidate staging commit contains no tracked changes." }
foreach ($changedPathRaw in $changedPaths) {
    $changedPath = ([string]$changedPathRaw).Trim().Replace('\\', '/')
    if (-not $changedPath.StartsWith('unity_game/Assets/', [System.StringComparison]::Ordinal)) {
        Fail "Candidate staging commit changed a path outside unity_game/Assets/: $changedPath"
    }
}

$expectedTasks = @('UART-003', 'UART-004', 'UART-005', 'UART-006', 'UART-007', 'URAC-011')
$coveredTasks = @($report.coveredTasks)
if ($coveredTasks.Count -ne $expectedTasks.Count) {
    Fail "Staging report must cover exactly six tasks. actual=$($coveredTasks.Count)"
}
foreach ($taskId in $expectedTasks) {
    if ($coveredTasks -notcontains $taskId) { Fail "Staging report coveredTasks is missing $taskId." }
}

$taskEvidence = @($report.taskEvidence)
if ($taskEvidence.Count -ne $expectedTasks.Count) {
    Fail "Staging report must contain exactly six task evidence records. actual=$($taskEvidence.Count)"
}
foreach ($taskId in $expectedTasks) {
    $records = @($taskEvidence | Where-Object { $_.taskId -eq $taskId })
    if ($records.Count -ne 1) { Fail "Staging report requires exactly one task evidence record for $taskId." }
    $record = $records[0]
    if ([string]::IsNullOrWhiteSpace([string]$record.state) -or
        [string]::IsNullOrWhiteSpace([string]$record.sourceEvidence) -or
        [string]::IsNullOrWhiteSpace([string]$record.runtimeEvidence)) {
        Fail "Staging task evidence is incomplete for $taskId."
    }
    Require-Bool $record 'verified' $false "taskEvidence[$taskId]"
    Require-Bool $record 'runtimeVerified' $false "taskEvidence[$taskId]"
    Require-Bool $record 'ownerAccepted' $false "taskEvidence[$taskId]"
}

$stagingReportHash = (Get-FileHash -LiteralPath $StagingReport -Algorithm SHA256).Hash.ToLowerInvariant()
$lineage = [ordered]@{
    schemaVersion = 1
    state = 'STAGING_PARENT_BOUND_TO_CANDIDATE'
    stagingSourceGitSha = $stagingSourceSha
    candidateGitSha = $candidateSha
    directParentGitSha = $directParentSha
    stagingReportPath = $StagingReport
    stagingReportSha256 = $stagingReportHash
    stagingReportSchemaVersion = 3
    stagingAuthorization = [ordered]@{
        authorizationSourceGitSha = $authorizationSourceSha
        handoffPacketSha256 = $handoffPacketSha256
        nativeHandoffVerificationSha256 = $nativeHandoffVerificationSha256
        operatorChainSha256 = $operatorChainSha256
    }
    coveredTasks = $expectedTasks
    candidateCommitChangedPaths = @($changedPaths | ForEach-Object { ([string]$_).Trim().Replace('\\', '/') })
    readyForLicensedCandidateTests = $true
    verified = $false
    runtimeVerified = $false
    ownerAccepted = $false
    publicationEligible = $false
    nextAction = 'Run licensed Unity tests/build on candidateGitSha, then bind the resulting candidate manifest and staging authorization fingerprints before physical-device evidence.'
}

if (-not [string]::IsNullOrWhiteSpace($Output)) {
    $outputPath = [System.IO.Path]::GetFullPath($Output)
    $artifactRoot = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot 'artifacts'))
    if (-not $outputPath.StartsWith($artifactRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        Fail "Output must stay under <repo>/artifacts/."
    }
    if (Test-Path -LiteralPath $outputPath) { Fail "Refusing to overwrite existing lineage report: $outputPath" }
    $parentDir = Split-Path -Parent $outputPath
    New-Item -ItemType Directory -Force -Path $parentDir | Out-Null
    $lineage | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $outputPath -Encoding UTF8
}

Write-Host "AFAREET_P1_STAGING_LINEAGE_OK stagingSourceGitSha=$stagingSourceSha candidateGitSha=$candidateSha reportSha256=$stagingReportHash packetSha256=$handoffPacketSha256 nativeVerificationSha256=$nativeHandoffVerificationSha256 operatorChainSha256=$operatorChainSha256 tasks=6 verified=false"
$lineage | ConvertTo-Json -Depth 10
