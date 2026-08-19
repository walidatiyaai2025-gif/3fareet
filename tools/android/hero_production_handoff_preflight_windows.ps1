param(
    [Parameter(Mandatory = $true)]
    [string]$Source,
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$SupportedExtensions = @('.obj', '.fbx', '.glb', '.gltf', '.blend')
$ForbiddenSegments = @('generated','placeholder','legacyprocedural','preview','refinement','refinementcandidates','blockout','review','reviewpackaging')
$PolicyRelative = 'unity_game/Assets/Afareet/Scripts/Vehicle/HeroCarLodPolicy.cs'
$BaseColorDirectives = @('map_kd','map_basecolor','map_base_color')

function Fail([string]$Message) {
    throw "AFAREET_UART003_HERO_NATIVE_PREFLIGHT_ERROR: $Message"
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
} else {
    $RepoRoot = (Resolve-Path $RepoRoot).Path
}

$git = Get-Command git -ErrorAction SilentlyContinue
if ($null -eq $git) { Fail 'git is required for UART-003 Hero preflight.' }
$topOutput = @(& $git.Source -C $RepoRoot rev-parse --show-toplevel 2>$null)
$topExit = $LASTEXITCODE
$top = ($topOutput | Select-Object -First 1)
if ($topExit -ne 0 -or [string]::IsNullOrWhiteSpace($top)) { Fail "Unable to resolve Git worktree root: $RepoRoot" }
$top = (Resolve-Path $top.Trim()).Path
if (-not [string]::Equals($top, $RepoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    Fail "RepoRoot must be the exact Git worktree root. resolved=$top requested=$RepoRoot"
}

function Normalize-Source([string]$Value) {
    $normalized = ($Value.Trim().Trim('"') -replace '\\','/')
    while ($normalized.StartsWith('./')) { $normalized = $normalized.Substring(2) }
    if ($normalized.StartsWith('Assets/', [System.StringComparison]::Ordinal)) { $normalized = 'unity_game/' + $normalized }
    return $normalized
}

function Assert-TrackedNonEmpty([string]$Relative, [string]$Label) {
    $relative = ($Relative -replace '\\','/')
    $absolute = Join-Path $RepoRoot ($relative -replace '/', [System.IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) { Fail "$Label is missing: $relative" }
    if ((Get-Item -LiteralPath $absolute).Length -le 0) { Fail "$Label is empty: $relative" }
    & $git.Source -C $RepoRoot ls-files --error-unmatch -- $relative *> $null
    $trackedExit = $LASTEXITCODE
    if ($trackedExit -ne 0) { Fail "$Label is not tracked by Git: $relative" }
    return $absolute
}

function Convert-ToRepoRelative([string]$Absolute, [string]$Label) {
    $full = [System.IO.Path]::GetFullPath($Absolute)
    $root = [System.IO.Path]::GetFullPath($RepoRoot)
    $separator = [System.IO.Path]::DirectorySeparatorChar
    $prefix = $root.TrimEnd([System.IO.Path]::DirectorySeparatorChar,[System.IO.Path]::AltDirectorySeparatorChar) + $separator
    if (-not $full.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) { Fail "$Label escapes the Git worktree: $full" }
    return $full.Substring($prefix.Length).Replace('\','/')
}

function Assert-TrackedFileWithMeta([string]$Absolute, [string]$Label) {
    $relative = Convert-ToRepoRelative $Absolute $Label
    $null = Assert-TrackedNonEmpty $relative $Label
    $metaRelative = $relative + '.meta'
    $null = Assert-TrackedNonEmpty $metaRelative "$Label Unity metadata"
    return $relative
}

function Resolve-PackageDependency([string]$PackageRoot,[string]$BaseDirectory,[string]$Reference,[string]$Label) {
    if ([string]::IsNullOrWhiteSpace($Reference)) { Fail "$Label reference is empty." }
    $normalized = ($Reference.Trim().Trim('"') -replace '\\','/')
    if ($normalized.StartsWith('/') -or $normalized.StartsWith('//') -or $normalized -match '^[A-Za-z]:/') {
        Fail "$Label must be a relative path inside the Hero handoff package: $Reference"
    }
    $package = [System.IO.Path]::GetFullPath($PackageRoot)
    $base = [System.IO.Path]::GetFullPath($BaseDirectory)
    $separator = [System.IO.Path]::DirectorySeparatorChar
    $prefix = $package.TrimEnd([System.IO.Path]::DirectorySeparatorChar,[System.IO.Path]::AltDirectorySeparatorChar) + $separator
    if (-not [string]::Equals($base,$package,[System.StringComparison]::OrdinalIgnoreCase) -and -not $base.StartsWith($prefix,[System.StringComparison]::OrdinalIgnoreCase)) {
        Fail "$Label base directory escapes the Hero handoff package: $base"
    }
    $native = $normalized -replace '/', $separator
    $resolved = [System.IO.Path]::GetFullPath((Join-Path $base $native))
    if (-not [string]::Equals($resolved,$package,[System.StringComparison]::OrdinalIgnoreCase) -and -not $resolved.StartsWith($prefix,[System.StringComparison]::OrdinalIgnoreCase)) {
        Fail "$Label escapes the Hero handoff package: $Reference"
    }
    return $resolved
}

function Split-WavefrontArguments([string]$Value,[string]$Label) {
    if ($null -eq $Value) { $Value = '' }
    $tokens = New-Object System.Collections.Generic.List[string]
    $current = New-Object System.Text.StringBuilder
    $quote = [char]0
    $tokenStarted = $false
    foreach ($char in $Value.ToCharArray()) {
        if ($quote -ne [char]0) {
            if ($char -eq $quote) { $quote = [char]0 } else { [void]$current.Append($char) }
            $tokenStarted = $true
            continue
        }
        if ($char -eq [char]34 -or $char -eq [char]39) {
            $quote = $char
            $tokenStarted = $true
            continue
        }
        if ($char -eq [char]35) { break }
        if ([char]::IsWhiteSpace($char)) {
            if ($tokenStarted) {
                $tokens.Add($current.ToString())
                [void]$current.Clear()
                $tokenStarted = $false
            }
            continue
        }
        [void]$current.Append($char)
        $tokenStarted = $true
    }
    if ($quote -ne [char]0) { Fail "$Label has an unterminated quoted argument." }
    if ($tokenStarted) { $tokens.Add($current.ToString()) }
    return $tokens.ToArray()
}

function Parse-PolicyArray([string]$Text,[string]$Name) {
    $match = [regex]::Match($Text,[regex]::Escape($Name) + '\s*=\s*\{\s*([^}]*)\}')
    if (-not $match.Success) { Fail "cannot parse HeroCarLodPolicy.$Name" }
    $values = @($match.Groups[1].Value.Split(',') | ForEach-Object { [int]$_.Trim() })
    if ($values.Count -ne 3) { Fail "HeroCarLodPolicy.$Name must contain exactly three values" }
    return $values
}

function Resolve-Lod([string]$Name) {
    $upper = if ($null -eq $Name) { '' } else { $Name.ToUpperInvariant() }
    $matches = New-Object System.Collections.Generic.List[int]
    for ($lod = 0; $lod -lt 3; $lod++) {
        $token = "_LOD$lod"
        $start = 0
        while ($start -lt $upper.Length) {
            $index = $upper.IndexOf($token,$start,[System.StringComparison]::Ordinal)
            if ($index -lt 0) { break }
            $suffix = $index + $token.Length
            if ($suffix -eq $upper.Length -or -not [char]::IsDigit($upper[$suffix])) { $matches.Add($lod); break }
            $start = $suffix
        }
    }
    if ($matches.Count -eq 1) { return $matches[0] }
    return -1
}

function Resolve-ObjIndex([string]$Token,[int]$Count) {
    $value = 0
    if (-not [int]::TryParse($Token,[ref]$value)) { Fail "invalid OBJ index: $Token" }
    if ($value -eq 0) { Fail 'OBJ index 0 is invalid' }
    $resolved = if ($value -gt 0) { $value - 1 } else { $Count + $value }
    if ($resolved -lt 0 -or $resolved -ge $Count) { Fail "OBJ index out of range: $Token" }
    return $resolved
}

$normalized = Normalize-Source $Source
if (-not $normalized.StartsWith('unity_game/Assets/',[System.StringComparison]::Ordinal)) { Fail 'Hero source must be a Unity Assets/ path.' }
if ($normalized.Contains('../') -or $normalized.EndsWith('/..')) { Fail "Hero source cannot contain traversal: $normalized" }
$lower = $normalized.ToLowerInvariant()
if (-not $lower.Contains('/vehicles/')) { Fail "Hero production source must resolve under a /Vehicles/ role path: $normalized" }
if ($lower.Contains('/rivals/')) { Fail "Rival production art cannot be reused as the Hero source: $normalized" }
$segments = @($normalized.Split('/') | ForEach-Object { $_.ToLowerInvariant() })
foreach ($forbidden in $ForbiddenSegments) { if ($segments -contains $forbidden) { Fail "Hero production source uses forbidden path segment: $forbidden" } }
$extension = [System.IO.Path]::GetExtension($normalized).ToLowerInvariant()
if ($extension -notin $SupportedExtensions) { Fail "unsupported Hero source format: $extension" }
$sourcePath = Assert-TrackedNonEmpty $normalized 'Hero production source'
$null = Assert-TrackedNonEmpty ($normalized + '.meta') 'Hero production source Unity metadata'

if ($extension -ne '.obj') {
    Write-Host "AFAREET_UART003_HERO_NATIVE_PREFLIGHT_OK verdict=UNITY_INSPECTION_REQUIRED source=$normalized sourceInspection=OPAQUE_SOURCE_UNITY_INSPECTION_REQUIRED unityInspectionRequired=true mutationStarted=false verified=false"
    exit 0
}

$policyPath = Assert-TrackedNonEmpty $PolicyRelative 'Hero LOD policy'
$policyText = Get-Content -Raw -LiteralPath $policyPath
$minimumVertices = Parse-PolicyArray $policyText 'MinimumVertices'
$vertexBudgets = Parse-PolicyArray $policyText 'VertexBudgets'
$minimumTriangles = Parse-PolicyArray $policyText 'MinimumTriangles'
$triangleBudgets = Parse-PolicyArray $policyText 'TriangleBudgets'

$vertexCount = 0
$texcoordCount = 0
$normalCount = 0
$currentObject = ''
$currentMaterial = ''
$unclassifiedFaces = 0
$mtllibs = New-Object System.Collections.Generic.List[string]
$groups = @{}
for ($lod = 0; $lod -lt 3; $lod++) {
    $groups[$lod] = @{ Name=''; Vertices=[System.Collections.Generic.HashSet[int]]::new(); Triangles=0; UvComplete=$true; NormalComplete=$true; Materials=[System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal) }
}

foreach ($raw in Get-Content -LiteralPath $sourcePath) {
    $line = $raw.Trim()
    if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) { continue }
    $parts = @($line -split '\s+')
    $op = $parts[0]
    if ($op -eq 'v') { if ($parts.Count -lt 4) { Fail 'OBJ vertex requires xyz' }; $vertexCount++; continue }
    if ($op -eq 'vt') { if ($parts.Count -lt 3) { Fail 'OBJ vt requires uv' }; $texcoordCount++; continue }
    if ($op -eq 'vn') { if ($parts.Count -lt 4) { Fail 'OBJ vn requires xyz' }; $normalCount++; continue }
    if ($op -eq 'o' -or $op -eq 'g') {
        $currentObject = (($parts | Select-Object -Skip 1) -join ' ').Trim()
        $currentMaterial = ''
        $lod = Resolve-Lod $currentObject
        if ($lod -ge 0) {
            $existing = [string]$groups[$lod].Name
            if (-not [string]::IsNullOrWhiteSpace($existing) -and $existing -ne $currentObject) { Fail "OBJ must expose exactly one authored object/group for LOD$lod" }
            $groups[$lod].Name = $currentObject
        }
        continue
    }
    if ($op -eq 'usemtl') { $currentMaterial = (($parts | Select-Object -Skip 1) -join ' ').Trim(); continue }
    if ($op -eq 'mtllib') {
        $references = @(Split-WavefrontArguments ($line.Substring('mtllib'.Length).Trim()) 'Hero OBJ mtllib')
        if ($references.Count -eq 0) { Fail 'Hero OBJ has an empty mtllib reference.' }
        foreach ($reference in $references) {
            if (-not $mtllibs.Contains($reference)) { $mtllibs.Add($reference) }
        }
        continue
    }
    if ($op -ne 'f') { continue }
    $lod = Resolve-Lod $currentObject
    if ($lod -lt 0) { $unclassifiedFaces++; continue }
    $refs = @($parts | Select-Object -Skip 1)
    if ($refs.Count -lt 3) { Fail "LOD$lod OBJ face has fewer than three vertices" }
    if ([string]::IsNullOrWhiteSpace($currentMaterial)) { Fail "LOD$lod face appears before usemtl" }
    foreach ($ref in $refs) {
        $components = @($ref.Split('/'))
        if ($components.Count -lt 1 -or [string]::IsNullOrWhiteSpace($components[0])) { Fail "LOD$lod face is missing vertex index" }
        $vi = Resolve-ObjIndex $components[0] $vertexCount
        [void]$groups[$lod].Vertices.Add($vi)
        $hasUv = $components.Count -ge 2 -and -not [string]::IsNullOrWhiteSpace($components[1])
        $hasNormal = $components.Count -ge 3 -and -not [string]::IsNullOrWhiteSpace($components[2])
        if ($hasUv) { [void](Resolve-ObjIndex $components[1] $texcoordCount) }
        if ($hasNormal) { [void](Resolve-ObjIndex $components[2] $normalCount) }
        $groups[$lod].UvComplete = [bool]$groups[$lod].UvComplete -and $hasUv
        $groups[$lod].NormalComplete = [bool]$groups[$lod].NormalComplete -and $hasNormal
    }
    $groups[$lod].Triangles = [int]$groups[$lod].Triangles + ($refs.Count - 2)
    [void]$groups[$lod].Materials.Add($currentMaterial)
}

if ($unclassifiedFaces -gt 0) { Fail "Hero OBJ contains $unclassifiedFaces faces outside explicit _LOD0/_LOD1/_LOD2 objects" }
if ($mtllibs.Count -eq 0) { Fail 'Hero OBJ must reference at least one MTL file.' }

$usedMaterials = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$lodTriangles = @()
for ($lod = 0; $lod -lt 3; $lod++) {
    $group = $groups[$lod]
    if ([string]::IsNullOrWhiteSpace([string]$group.Name)) { Fail "Hero OBJ is missing an authored _LOD$lod object/group" }
    if ([int]$group.Triangles -le 0) { Fail "Hero LOD$lod has no faces" }
    if (-not [bool]$group.UvComplete) { Fail "Hero LOD$lod is missing complete UV0" }
    if (-not [bool]$group.NormalComplete) { Fail "Hero LOD$lod is missing authored normals" }
    if ($group.Materials.Count -eq 0) { Fail "Hero LOD$lod has no material" }
    $vertices = $group.Vertices.Count
    $triangles = [int]$group.Triangles
    if ($vertices -lt $minimumVertices[$lod] -or $vertices -gt $vertexBudgets[$lod]) { Fail "Hero LOD$lod vertex count $vertices is outside [$($minimumVertices[$lod]), $($vertexBudgets[$lod])]" }
    if ($triangles -lt $minimumTriangles[$lod] -or $triangles -gt $triangleBudgets[$lod]) { Fail "Hero LOD$lod triangle count $triangles is outside [$($minimumTriangles[$lod]), $($triangleBudgets[$lod])]" }
    foreach ($material in $group.Materials) { [void]$usedMaterials.Add($material) }
    $lodTriangles += $triangles
}
if (-not ($lodTriangles[0] -gt $lodTriangles[1] -and $lodTriangles[1] -gt $lodTriangles[2])) { Fail 'Hero triangle counts must decrease LOD0 > LOD1 > LOD2' }

$packageRoot = Split-Path -Parent $sourcePath
$mappedMaterials = @{}
foreach ($material in $usedMaterials) { $mappedMaterials[$material] = $false }
$mtlCount = 0
$textureSeen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($mtllib in $mtllibs) {
    $mtlPath = Resolve-PackageDependency $packageRoot $packageRoot $mtllib "$normalized mtllib"
    $null = Assert-TrackedFileWithMeta $mtlPath 'Hero MTL dependency'
    $mtlCount++
    $currentMtlMaterial = ''
    foreach ($rawMtl in Get-Content -LiteralPath $mtlPath) {
        $mtlLine = $rawMtl.Trim()
        if ([string]::IsNullOrWhiteSpace($mtlLine) -or $mtlLine.StartsWith('#')) { continue }
        if ($mtlLine -match '^newmtl\s+(.+)$') { $currentMtlMaterial = $Matches[1].Trim(); continue }
        $tokens = @(Split-WavefrontArguments $mtlLine 'Hero MTL texture directive')
        if ($tokens.Count -lt 2 -or ($tokens[0].ToLowerInvariant() -notin $BaseColorDirectives)) { continue }
        if ([string]::IsNullOrWhiteSpace($currentMtlMaterial)) { Fail 'Hero MTL contains a base-color texture directive before newmtl.' }
        $textureRef = $tokens[-1]
        $texturePath = Resolve-PackageDependency $packageRoot (Split-Path -Parent $mtlPath) $textureRef 'Hero MTL texture'
        $textureRelative = Assert-TrackedFileWithMeta $texturePath 'Hero texture dependency'
        [void]$textureSeen.Add($textureRelative)
        if ($mappedMaterials.ContainsKey($currentMtlMaterial)) { $mappedMaterials[$currentMtlMaterial] = $true }
    }
}
foreach ($material in $usedMaterials) { if (-not [bool]$mappedMaterials[$material]) { Fail "Hero material is not base-color texture-mapped by a supplied package-local MTL: $material" } }

Write-Host "AFAREET_UART003_HERO_NATIVE_PREFLIGHT_OK verdict=READY_FOR_LICENSED_UNITY_IMPORT source=$normalized sourceInspection=OBJ_STRUCTURAL_PASS lods=3 mtllibs=$mtlCount textures=$($textureSeen.Count) dependenciesTracked=true dependenciesPackageLocal=true unityInspectionRequired=false mutationStarted=false verified=false"