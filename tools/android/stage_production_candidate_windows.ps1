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
    Fail "Staging handoff requires a clean Git tree. Commit the real Hero and Rival production source packages first, then rerun."
}

function Assert-TrackedNonEmptyFile([string]$RepoRelative, [string]$Label) {
    $normalized = ($RepoRelative.Trim().Trim('"') -replace '\\', '/')
    $absolute = Join-Path $RepoRoot ($normalized -replace '/', [System.IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) {
        Fail "$Label is missing: $normalized"
    }

    $item = Get-Item -LiteralPath $absolute
    if ($item.Length -le 0) {
        Fail "$Label is empty: $normalized"
    }

    & $git.Source -C $RepoRoot ls-files --error-unmatch -- $normalized *> $null
    if ($LASTEXITCODE -ne 0) {
        Fail "$Label must already be tracked in the clean starting commit before licensed staging: $normalized"
    }

    return $item.Length
}

$HeroSource = ($HeroSource.Trim().Trim('"') -replace '\\', '/')
if (-not $HeroSource.StartsWith('Assets/', [System.StringComparison]::Ordinal)) {
    Fail "HeroSource must be a Unity Assets/ path, for example Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing.fbx"
}
if ($HeroSource.Contains('../')) {
    Fail "HeroSource cannot contain ../ traversal: $HeroSource"
}
if ($HeroSource -notmatch '(?i)/Vehicles/') {
    Fail "HeroSource must resolve under a /Vehicles/ role path: $HeroSource"
}
if ($HeroSource -match '(?i)/Rivals/') {
    Fail "HeroSource cannot reuse /Rivals/ production art as the Hero source: $HeroSource"
}
if ($HeroSource -match '(?i)/(Generated|Placeholder|LegacyProcedural|Preview|Refinement|RefinementCandidates|Blockout|Review|ReviewPackaging)/') {
    Fail "HeroSource cannot be under Generated, Placeholder, LegacyProcedural, Preview, Refinement, Blockout, Review or ReviewPackaging: $HeroSource"
}
$extension = [System.IO.Path]::GetExtension($HeroSource).ToLowerInvariant()
if ($extension -notin @('.fbx', '.obj', '.blend', '.glb', '.gltf')) {
    Fail "Unsupported HeroSource extension: $extension"
}

$heroRepoRelative = ('unity_game/' + $HeroSource).Replace('//', '/')
$heroMetaRelative = $heroRepoRelative + '.meta'
$heroBytes = Assert-TrackedNonEmptyFile $heroRepoRelative 'Hero production source'
$heroMetaBytes = Assert-TrackedNonEmptyFile $heroMetaRelative 'Hero production source Unity metadata'

$rivalSources = @(
    'unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_01_WedgeCoupe_Production.obj',
    'unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_02_FastbackMuscle_Production.obj',
    'unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_03_CompactPrototype_Production.obj'
)

$rivalBytes = 0L
foreach ($rivalSource in $rivalSources) {
    $rivalBytes += Assert-TrackedNonEmptyFile $rivalSource 'Rival production source'
    $rivalMetaBytes = Assert-TrackedNonEmptyFile ($rivalSource + '.meta') 'Rival production source Unity metadata'
    if ($rivalMetaBytes -le 0) {
        Fail "Rival production source Unity metadata unexpectedly reported zero bytes: $($rivalSource).meta"
    }
}

Write-Host "AFAREET_STAGING_EXTERNAL_SOURCE_PREFLIGHT_OK gitSha=$gitSha heroBytes=$heroBytes heroMetaBytes=$heroMetaBytes rivalSources=3 rivalBytes=$rivalBytes verified=false"

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
$ReportDir = Join-Path $RepoRoot "artifacts\production-staging"
New-Item -ItemType Directory -Force -Path $LogDir, $ReportDir | Out-Null
$LogPath = Join-Path $LogDir "unity-production-staging-handoff.log"
$StatusPath = Join-Path $ReportDir "p1-staging-handoff.git-status.txt"
$ReportPath = Join-Path $ReportDir "p1-staging-handoff.json"
Remove-Item -Force $LogPath, $StatusPath, $ReportPath -ErrorAction SilentlyContinue

Write-Host "AFAREET_P1_STAGING_HANDOFF_START gitSha=$gitSha heroSource=$HeroSource rivals=3 unity=$UnityPath"

$unityArgs = @(
    '-batchmode',
    '-quit',
    '-projectPath', ('"{0}"' -f $ProjectPath),
    '-executeMethod', 'Afareet.Editor.P1ProductionCandidateStagingHandoff.StageForCommit',
    '-afareetHeroSource', ('"{0}"' -f $HeroSource),
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
