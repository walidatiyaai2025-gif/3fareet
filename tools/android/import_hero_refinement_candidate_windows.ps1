param(
    [Parameter(Mandatory = $true)]
    [string]$SourceFbx,

    [string]$RepositoryRoot = "",

    [string]$UnityPath = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ExpectedSha256 = "97b02c87118c451d068c881fc551787d6e468ec8002cce7802db62258cc4cda2"
$ExpectedSize = 1475244
$DestinationRelative = "unity_game\Assets\Afareet\ArtSource\Vehicles\RefinementCandidates\AfareetKing_Hero.fbx"

if (-not (Test-Path -LiteralPath $SourceFbx -PathType Leaf)) {
    throw "Hero refinement source FBX not found: $SourceFbx"
}

$SourceFbx = (Resolve-Path -LiteralPath $SourceFbx).Path

if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
} else {
    $RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
}

$actualHash = (Get-FileHash -LiteralPath $SourceFbx -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualHash -ne $ExpectedSha256) {
    throw "Hero refinement FBX SHA-256 mismatch. expected=$ExpectedSha256 actual=$actualHash"
}

$actualSize = (Get-Item -LiteralPath $SourceFbx).Length
if ($actualSize -ne $ExpectedSize) {
    throw "Hero refinement FBX size mismatch. expected=$ExpectedSize actual=$actualSize"
}

$destination = Join-Path $RepositoryRoot $DestinationRelative
$destinationDir = Split-Path -Parent $destination
New-Item -ItemType Directory -Force -Path $destinationDir | Out-Null

if (Test-Path -LiteralPath $destination -PathType Leaf) {
    $existingHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($existingHash -ne $ExpectedSha256) {
        throw "Destination exists with different bytes: $destination"
    }
} else {
    Copy-Item -LiteralPath $SourceFbx -Destination $destination
}

$copiedHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant()
if ($copiedHash -ne $ExpectedSha256) {
    throw "Copied Hero refinement FBX failed SHA-256 verification."
}

Write-Host "AFAREET_HERO_REFINEMENT_INTAKE_OK path=$destination sha256=$copiedHash productionGate=false"

if (-not [string]::IsNullOrWhiteSpace($UnityPath)) {
    if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
        throw "Unity executable not found: $UnityPath"
    }

    $projectPath = Join-Path $RepositoryRoot "unity_game"
    $args = @(
        "-batchmode",
        "-quit",
        "-projectPath", $projectPath,
        "-executeMethod", "Afareet.Editor.HeroCarRefinementCandidateStager.StageCurrentCandidate",
        "-logFile", "-"
    )

    $process = Start-Process -FilePath $UnityPath -ArgumentList $args -Wait -PassThru -NoNewWindow
    if ($process.ExitCode -ne 0) {
        throw "Unity refinement candidate staging failed with exit code $($process.ExitCode)."
    }

    Write-Host "AFAREET_HERO_REFINEMENT_UNITY_STAGE_OK productionGate=false"
}
