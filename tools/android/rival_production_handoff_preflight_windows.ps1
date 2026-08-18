param(
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Fail([string]$Message) {
    throw "AFAREET_UART004_RIVAL_NATIVE_PREFLIGHT_ERROR: $Message"
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
} else {
    $RepoRoot = (Resolve-Path $RepoRoot).Path
}

$git = Get-Command git -ErrorAction SilentlyContinue
if ($null -eq $git) {
    Fail "git is required for exact-SHA Rival handoff validation."
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

$dirty = @(& $git.Source -C $RepoRoot status --porcelain --untracked-files=all 2>$null)
if ($LASTEXITCODE -ne 0) {
    Fail "Unable to inspect Git working tree."
}
if ($dirty.Count -gt 0) {
    $dirty | ForEach-Object { Write-Warning "RIVAL_PREFLIGHT_DIRTY $_" }
    Fail "Rival dependency preflight requires a clean Git tree so every dependency is bound to the exact staging SHA."
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
        Fail "$Label must already be tracked in the exact staging commit: $normalized"
    }

    return $item.Length
}

function Convert-ToRepoRelative([string]$AbsolutePath, [string]$Label) {
    $full = [System.IO.Path]::GetFullPath($AbsolutePath)
    $root = [System.IO.Path]::GetFullPath($RepoRoot)
    $separator = [System.IO.Path]::DirectorySeparatorChar
    $rootPrefix = $root.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + $separator
    if (-not $full.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        Fail "$Label resolved outside the Git worktree: $full"
    }
    return $full.Substring($rootPrefix.Length).Replace('\', '/')
}

function Resolve-PackageDependency(
    [string]$PackageRoot,
    [string]$BaseDirectory,
    [string]$Reference,
    [string]$Label
) {
    if ([string]::IsNullOrWhiteSpace($Reference)) {
        Fail "$Label reference is empty."
    }

    $normalized = ($Reference.Trim().Trim('"') -replace '\\', '/')
    if ($normalized.StartsWith('/') -or $normalized.StartsWith('//') -or $normalized -match '^[A-Za-z]:/') {
        Fail "$Label must be a relative path inside the Rival handoff package: $Reference"
    }

    $package = [System.IO.Path]::GetFullPath($PackageRoot)
    $base = [System.IO.Path]::GetFullPath($BaseDirectory)
    $separator = [System.IO.Path]::DirectorySeparatorChar
    $packagePrefix = $package.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + $separator
    if (-not [string]::Equals($base, $package, [System.StringComparison]::OrdinalIgnoreCase) -and
        -not $base.StartsWith($packagePrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        Fail "$Label base directory escapes the Rival handoff package: $base"
    }

    $nativeReference = $normalized -replace '/', $separator
    $resolved = [System.IO.Path]::GetFullPath((Join-Path $base $nativeReference))
    if (-not [string]::Equals($resolved, $package, [System.StringComparison]::OrdinalIgnoreCase) -and
        -not $resolved.StartsWith($packagePrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        Fail "$Label escapes the Rival handoff package: $Reference"
    }
    if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
        Fail "$Label is missing: $Reference"
    }
    return $resolved
}

$packageRepoRelative = 'unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production'
$packageRoot = Join-Path $RepoRoot ($packageRepoRelative -replace '/', [System.IO.Path]::DirectorySeparatorChar)
if (-not (Test-Path -LiteralPath $packageRoot -PathType Container)) {
    Fail "Rival production package root is missing: $packageRepoRelative"
}
$packageRoot = (Resolve-Path $packageRoot).Path

$rivalSources = @(
    'Rival_01_WedgeCoupe_Production.obj',
    'Rival_02_FastbackMuscle_Production.obj',
    'Rival_03_CompactPrototype_Production.obj'
)
$textureDirectivePattern = '^(map_ka|map_kd|map_ks|map_ke|map_ns|map_d|map_bump|bump|disp|decal|norm|map_pr|map_pm)\s+(.+)$'
$mtlSeen = @{}
$textureSeen = @{}

foreach ($rivalFile in $rivalSources) {
    $objRepoRelative = "$packageRepoRelative/$rivalFile"
    $null = Assert-TrackedNonEmptyFile $objRepoRelative "Rival production OBJ"
    $null = Assert-TrackedNonEmptyFile ($objRepoRelative + '.meta') "Rival production OBJ Unity metadata"
    $objPath = Join-Path $packageRoot $rivalFile

    $requiredMaterials = @{}
    $mtllibReferences = New-Object System.Collections.Generic.List[string]
    foreach ($rawLine in Get-Content -LiteralPath $objPath) {
        $line = $rawLine.Trim()
        if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) { continue }
        if ($line -match '^mtllib\s+(.+)$') {
            $reference = $Matches[1].Trim()
            if (-not [string]::IsNullOrWhiteSpace($reference) -and -not $mtllibReferences.Contains($reference)) {
                $mtllibReferences.Add($reference)
            }
            continue
        }
        if ($line -match '^usemtl\s+(.+)$') {
            $material = $Matches[1].Trim()
            if (-not [string]::IsNullOrWhiteSpace($material)) {
                $requiredMaterials[$material] = $true
            }
        }
    }

    if ($mtllibReferences.Count -le 0) {
        Fail "$rivalFile must declare at least one mtllib before licensed staging."
    }
    if ($requiredMaterials.Count -le 0) {
        Fail "$rivalFile must use at least one material before licensed staging."
    }

    $mappedMaterials = @{}
    foreach ($mtllibReference in $mtllibReferences) {
        $mtlPath = Resolve-PackageDependency $packageRoot (Split-Path -Parent $objPath) $mtllibReference "$rivalFile mtllib"
        $mtlRepoRelative = Convert-ToRepoRelative $mtlPath "$rivalFile mtllib"
        $null = Assert-TrackedNonEmptyFile $mtlRepoRelative "Rival MTL dependency"
        $null = Assert-TrackedNonEmptyFile ($mtlRepoRelative + '.meta') "Rival MTL Unity metadata"
        $mtlSeen[$mtlRepoRelative] = $true

        $currentMaterial = ''
        foreach ($rawMtlLine in Get-Content -LiteralPath $mtlPath) {
            $mtlLine = $rawMtlLine.Trim()
            if ([string]::IsNullOrWhiteSpace($mtlLine) -or $mtlLine.StartsWith('#')) { continue }
            if ($mtlLine -match '^newmtl\s+(.+)$') {
                $currentMaterial = $Matches[1].Trim()
                continue
            }
            if ($mtlLine -notmatch $textureDirectivePattern) { continue }
            if ([string]::IsNullOrWhiteSpace($currentMaterial)) {
                Fail "$mtlRepoRelative contains a texture directive before newmtl."
            }

            $tokens = @($Matches[2].Trim() -split '\s+')
            if ($tokens.Count -le 0 -or [string]::IsNullOrWhiteSpace($tokens[-1])) {
                Fail "$mtlRepoRelative has an empty texture reference for material $currentMaterial."
            }
            $textureReference = $tokens[-1]
            $texturePath = Resolve-PackageDependency $packageRoot (Split-Path -Parent $mtlPath) $textureReference "$mtlRepoRelative texture"
            $textureRepoRelative = Convert-ToRepoRelative $texturePath "$mtlRepoRelative texture"
            $null = Assert-TrackedNonEmptyFile $textureRepoRelative "Rival texture dependency"
            $null = Assert-TrackedNonEmptyFile ($textureRepoRelative + '.meta') "Rival texture Unity metadata"
            $textureSeen[$textureRepoRelative] = $true
            $mappedMaterials[$currentMaterial] = $true
        }
    }

    foreach ($requiredMaterial in $requiredMaterials.Keys) {
        if (-not $mappedMaterials.ContainsKey($requiredMaterial)) {
            Fail "$rivalFile material is not texture-mapped by a supplied package-local MTL: $requiredMaterial"
        }
    }
}

Write-Host "AFAREET_UART004_RIVAL_NATIVE_PREFLIGHT_OK gitSha=$gitSha rivals=3 mtllibs=$($mtlSeen.Count) textures=$($textureSeen.Count) dependenciesTracked=true dependenciesPackageLocal=true mutationStarted=false verified=false"
