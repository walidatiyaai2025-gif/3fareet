param(
    [string]$UnityPath = "",
    [string]$RepoRoot = "",
    [string]$StagingReport = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Fail([string]$Message) {
    throw "AFAREET_P1_STAGED_CANDIDATE_ERROR: $Message"
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
} else {
    $RepoRoot = (Resolve-Path $RepoRoot).Path
}

$lineageVerifier = Join-Path $RepoRoot "tools\android\verify_p1_staging_lineage_windows.ps1"
$genericRunner = Join-Path $RepoRoot "tools\android\run_local_candidate_windows.ps1"
foreach ($required in @($lineageVerifier, $genericRunner)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        Fail "Required P1 staged candidate component is missing: $required"
    }
}

if ([string]::IsNullOrWhiteSpace($StagingReport)) {
    $StagingReport = Join-Path $RepoRoot "artifacts\production-staging\p1-staging-handoff.json"
}
if (-not (Test-Path -LiteralPath $StagingReport -PathType Leaf)) {
    Fail "P1 staging report is missing: $StagingReport"
}
$StagingReport = (Resolve-Path -LiteralPath $StagingReport).Path

$git = Get-Command git -ErrorAction SilentlyContinue
if ($null -eq $git) { Fail "git is required for the P1 staged candidate chain." }
$currentSha = (& $git.Source -C $RepoRoot rev-parse HEAD 2>$null | Select-Object -First 1)
if ($LASTEXITCODE -ne 0 -or $currentSha -notmatch '^[0-9a-fA-F]{40}$') {
    Fail "Unable to resolve current candidate Git SHA."
}
$currentSha = $currentSha.Trim().ToLowerInvariant()

$artifactDir = Join-Path $RepoRoot "artifacts\production-staging"
New-Item -ItemType Directory -Force -Path $artifactDir | Out-Null
$lineageReport = Join-Path $artifactDir "p1-staging-lineage.json"
$linkManifest = Join-Path $RepoRoot "artifacts\p1-staged-candidate-manifest.json"
Remove-Item -Force $lineageReport, $linkManifest -ErrorAction SilentlyContinue

Write-Host "AFAREET_P1_STAGED_CANDIDATE_LINEAGE_START candidateGitSha=$currentSha stagingReport=$StagingReport"
& $lineageVerifier -RepoRoot $RepoRoot -StagingReport $StagingReport -Output $lineageReport | ForEach-Object { Write-Host $_ }
if (-not (Test-Path -LiteralPath $lineageReport -PathType Leaf) -or (Get-Item -LiteralPath $lineageReport).Length -le 0) {
    Fail "P1 lineage verifier did not produce a non-empty report: $lineageReport"
}
try {
    $lineage = Get-Content -Raw -LiteralPath $lineageReport -Encoding UTF8 | ConvertFrom-Json
} catch {
    Fail "P1 lineage report is not valid JSON: $($_.Exception.Message)"
}
if ($lineage.state -ne 'STAGING_PARENT_BOUND_TO_CANDIDATE' -or $lineage.candidateGitSha -ne $currentSha) {
    Fail "P1 lineage report is not bound to the current candidate SHA. state=$($lineage.state) candidate=$($lineage.candidateGitSha) expected=$currentSha"
}
if ($lineage.readyForLicensedCandidateTests -ne $true -or
    $lineage.verified -ne $false -or
    $lineage.runtimeVerified -ne $false -or
    $lineage.ownerAccepted -ne $false -or
    $lineage.publicationEligible -ne $false) {
    Fail "P1 lineage report crossed its tests-only/unverified boundary."
}
Write-Host "AFAREET_P1_STAGED_CANDIDATE_LINEAGE_OK stagingSourceGitSha=$($lineage.stagingSourceGitSha) candidateGitSha=$currentSha verified=false"

$genericParams = @{ RepoRoot = $RepoRoot }
if (-not [string]::IsNullOrWhiteSpace($UnityPath)) { $genericParams.UnityPath = $UnityPath }
& $genericRunner @genericParams

$localCandidateManifest = Join-Path $RepoRoot "artifacts\local-candidate-manifest.json"
if (-not (Test-Path -LiteralPath $localCandidateManifest -PathType Leaf) -or (Get-Item -LiteralPath $localCandidateManifest).Length -le 0) {
    Fail "Generic licensed candidate runner did not produce a candidate manifest: $localCandidateManifest"
}
try {
    $candidate = Get-Content -Raw -LiteralPath $localCandidateManifest -Encoding UTF8 | ConvertFrom-Json
} catch {
    Fail "Generic candidate manifest is not valid JSON: $($_.Exception.Message)"
}
if ($candidate.gitSha -ne $currentSha -or $candidate.readyForDeviceEvidence -ne $true -or $candidate.verified -ne $false) {
    Fail "Generic candidate manifest does not match the P1 staged candidate SHA/readiness boundary."
}
if ($candidate.verdict -ne 'READY_FOR_PHYSICAL_DEVICE_EVIDENCE') {
    Fail "Generic candidate verdict mismatch: $($candidate.verdict)"
}

$stagingReportHash = (Get-FileHash -LiteralPath $StagingReport -Algorithm SHA256).Hash.ToLowerInvariant()
$lineageHash = (Get-FileHash -LiteralPath $lineageReport -Algorithm SHA256).Hash.ToLowerInvariant()
$localManifestHash = (Get-FileHash -LiteralPath $localCandidateManifest -Algorithm SHA256).Hash.ToLowerInvariant()
$apkHash = ([string]$candidate.apk.sha256).Trim().ToLowerInvariant()
if ($apkHash -notmatch '^[0-9a-f]{64}$') { Fail "Generic candidate APK SHA-256 is invalid: $apkHash" }
if ($stagingReportHash -ne $lineage.stagingReportSha256) {
    Fail "Staging report bytes changed after lineage verification. lineage=$($lineage.stagingReportSha256) actual=$stagingReportHash"
}

$link = [ordered]@{
    schemaVersion = 1
    candidateType = 'p1-staged-local-windows-licensed-unity'
    generatedAtUtc = [DateTime]::UtcNow.ToString('o')
    stagingSourceGitSha = $lineage.stagingSourceGitSha
    candidateGitSha = $currentSha
    directParentGitSha = $lineage.directParentGitSha
    stagingReport = [ordered]@{
        path = $StagingReport
        sha256 = $stagingReportHash
        schemaVersion = 2
    }
    stagingLineage = [ordered]@{
        path = $lineageReport
        sha256 = $lineageHash
        state = $lineage.state
    }
    localCandidateManifest = [ordered]@{
        path = $localCandidateManifest
        sha256 = $localManifestHash
    }
    apkSha256 = $apkHash
    coveredTasks = @($lineage.coveredTasks)
    readyForDeviceEvidence = $true
    verified = $false
    runtimeVerified = $false
    ownerAccepted = $false
    publicationEligible = $false
    verdict = 'P1_STAGED_CANDIDATE_READY_FOR_PHYSICAL_DEVICE_EVIDENCE'
    notes = @(
        'This envelope binds the reviewed staging parent, exact candidate commit, staging report, local candidate manifest, and APK hash.',
        'It does not mark UART/URAC runtime proof, physical-device evidence, owner approval, or publication as complete.'
    )
}
$link | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $linkManifest -Encoding UTF8

Write-Host "AFAREET_P1_STAGED_CANDIDATE_OK stagingSourceGitSha=$($lineage.stagingSourceGitSha) candidateGitSha=$currentSha apkSha256=$apkHash manifest=$linkManifest verified=false"
Write-Host "Next: bind physical Android device evidence to artifacts/local-candidate-manifest.json and preserve this P1 lineage envelope for visual/runtime review."
