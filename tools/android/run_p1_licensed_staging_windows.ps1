param(
    [Parameter(Mandatory = $true)]
    [string]$HeroSource,
    [Parameter(Mandatory = $true)]
    [string]$HandoffPacket,
    [string]$UnityPath = "",
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Fail([string]$Message) {
    throw "AFAREET_P1_LICENSED_STAGING_RUNNER_ERROR: $Message"
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
} else {
    $RepoRoot = (Resolve-Path $RepoRoot).Path
}

$verifyScript = Join-Path $RepoRoot 'tools\android\verify_p1_licensed_handoff_packet_windows.ps1'
$stageScript = Join-Path $RepoRoot 'tools\android\stage_production_candidate_windows.ps1'
$verifyOutput = Join-Path $RepoRoot 'artifacts\production-staging\p1-native-handoff-verification.json'

if (-not (Test-Path -LiteralPath $verifyScript -PathType Leaf)) {
    Fail "Native P1 handoff verifier is missing: $verifyScript"
}
if (-not (Test-Path -LiteralPath $stageScript -PathType Leaf)) {
    Fail "Licensed staging implementation is missing: $stageScript"
}
if (Test-Path -LiteralPath $verifyOutput) {
    Remove-Item -Force -LiteralPath $verifyOutput
}

Write-Host "AFAREET_P1_LICENSED_STAGING_PACKET_VERIFY_START heroSource=$HeroSource packet=$HandoffPacket"
& $verifyScript -Packet $HandoffPacket -HeroSource $HeroSource -RepoRoot $RepoRoot -Output $verifyOutput | ForEach-Object { Write-Host $_ }
if (-not (Test-Path -LiteralPath $verifyOutput -PathType Leaf) -or (Get-Item $verifyOutput).Length -le 0) {
    Fail "Native P1 handoff verifier did not produce its evidence report."
}
try {
    $verification = Get-Content -Raw -LiteralPath $verifyOutput | ConvertFrom-Json
} catch {
    Fail "Native P1 handoff verification report is invalid JSON: $($_.Exception.Message)"
}
if ($verification.state -ne 'NATIVE_P1_HANDOFF_VERIFIED_FOR_LICENSED_STAGING' -or
    $verification.releaseHandoffEligible -ne $true -or
    $verification.licensedUnityExecuted -ne $false -or
    $verification.candidateBuildStarted -ne $false -or
    $verification.publicationEligible -ne $false -or
    $verification.publicationPerformed -ne $false -or
    $verification.verified -ne $false -or
    $verification.runtimeVerified -ne $false -or
    $verification.ownerAccepted -ne $false) {
    Fail "Native handoff verification report crossed the staging-only safety boundary."
}
Write-Host "AFAREET_P1_LICENSED_STAGING_PACKET_VERIFY_OK gitSha=$($verification.gitSha) heroSource=$($verification.heroSource) verified=false"

$stageParams = @{
    HeroSource = $HeroSource
    RepoRoot = $RepoRoot
}
if (-not [string]::IsNullOrWhiteSpace($UnityPath)) {
    $stageParams.UnityPath = $UnityPath
}

Write-Host "AFAREET_P1_LICENSED_STAGING_START gitSha=$($verification.gitSha) heroSource=$HeroSource"
& $stageScript @stageParams | ForEach-Object { Write-Host $_ }
Write-Host "AFAREET_P1_LICENSED_STAGING_RUNNER_OK gitSha=$($verification.gitSha) heroSource=$HeroSource verified=false publicationPerformed=false"
