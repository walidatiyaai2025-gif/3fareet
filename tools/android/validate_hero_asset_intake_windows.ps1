param(
    [Parameter(Mandatory = $true)]
    [string]$Source,
    [string]$RepoRoot = "",
    [string]$Output = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ExpectedRoot = 'unity_game/Assets/Afareet/ArtSource/Vehicles/HeroCar'
$SupportedExtensions = @('.obj', '.fbx', '.glb', '.gltf', '.blend')
$ForbiddenSegments = @('generated', 'preview', 'blockout', 'rivals')
$PolicyRelativePath = 'unity_game/Assets/Afareet/Scripts/Vehicle/HeroCarLodPolicy.cs'

function Fail([string]$Message) {
    throw "AFAREET_UART003_NATIVE_INTAKE_ERROR: $Message"
}

function Resolve-RepoRoot([string]$RequestedRoot) {
    if ([string]::IsNullOrWhiteSpace($RequestedRoot)) {
        $RequestedRoot = Join-Path $PSScriptRoot '..\..'
    }
    $resolved = (Resolve-Path $RequestedRoot).Path
    $git = Get-Command git -ErrorAction SilentlyContinue
    if ($null -eq $git) { Fail 'git is required for UART-003 native intake.' }
    $top = (& $git.Source -C $resolved rev-parse --show-toplevel 2>$null | Select-Object -First 1)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($top)) { Fail "Unable to resolve Git worktree root: $resolved" }
    $top = (Resolve-Path $top.Trim()).Path
    if (-not [string]::Equals($top, $resolved, [System.StringComparison]::OrdinalIgnoreCase)) {
        Fail "RepoRoot must be the exact Git worktree root. resolved=$top requested=$resolved"
    }
    return $resolved
}

function Normalize-Source([string]$Value) {
    $normalized = ($Value.Trim().Trim('"') -replace '\\', '/')
    while ($normalized.StartsWith('./')) { $normalized = $normalized.Substring(2) }
    if ($normalized.StartsWith('Assets/', [System.StringComparison]::Ordinal)) {
        $normalized = 'unity_game/' + $normalized
    }
    return $normalized
}

function Assert-Tracked([string]$Root, [string]$RelativePath, [string]$Label) {
    $git = Get-Command git -ErrorAction Stop
    & $git.Source -C $Root ls-files --error-unmatch -- $RelativePath *> $null
    if ($LASTEXITCODE -ne 0) { Fail "$Label is not tracked by Git: $RelativePath" }
}

function Parse-PolicyArray([string]$Text, [string]$Name) {
    $match = [regex]::Match($Text, [regex]::Escape($Name) + '\s*=\s*\{\s*([^}]*)\}')
    if (-not $match.Success) { Fail "cannot parse HeroCarLodPolicy.$Name" }
    $values = @($match.Groups[1].Value.Split(',') | ForEach-Object { [int]$_.Trim() })
    if ($values.Count -ne 3) { Fail "HeroCarLodPolicy.$Name must contain exactly 3 values" }
    return $values
}

function Resolve-ObjIndex([string]$Token, [int]$Count) {
    $value = 0
    if (-not [int]::TryParse($Token, [ref]$value)) { Fail "invalid OBJ index: $Token" }
    if ($value -eq 0) { Fail 'OBJ index 0 is invalid' }
    $resolved = if ($value -gt 0) { $value - 1 } else { $Count + $value }
    if ($resolved -lt 0 -or $resolved -ge $Count) { Fail "OBJ index out of range: $Token" }
    return $resolved
}

function Inspect-Obj([string]$Root, [string]$RelativePath) {
    $sourcePath = Join-Path $Root ($RelativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
    $policyPath = Join-Path $Root ($PolicyRelativePath -replace '/', [System.IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $policyPath -PathType Leaf)) { Fail "missing authoritative Hero LOD policy: $PolicyRelativePath" }
    Assert-Tracked $Root $PolicyRelativePath 'Hero LOD policy'

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
    $mtllibs = New-Object System.Collections.Generic.List[string]
    $groups = @{}
    for ($lod = 0; $lod -lt 3; $lod++) {
        $groups[$lod] = @{
            Name = ''
            Vertices = [System.Collections.Generic.HashSet[int]]::new()
            Triangles = 0
            UvComplete = $true
            NormalComplete = $true
            Materials = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        }
    }

    foreach ($raw in Get-Content -LiteralPath $sourcePath) {
        $line = $raw.Trim()
        if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith('#')) { continue }
        $parts = @($line -split '\s+')
        $op = $parts[0]
        switch ($op) {
            'v' { if ($parts.Count -lt 4) { Fail 'OBJ vertex requires xyz' }; $vertexCount++; continue }
            'vt' { if ($parts.Count -lt 3) { Fail 'OBJ vt requires uv' }; $texcoordCount++; continue }
            'vn' { if ($parts.Count -lt 4) { Fail 'OBJ vn requires xyz' }; $normalCount++; continue }
            'o' { $currentObject = (($parts | Select-Object -Skip 1) -join ' ').Trim() }
            'g' { $currentObject = (($parts | Select-Object -Skip 1) -join ' ').Trim() }
            'usemtl' { $currentMaterial = (($parts | Select-Object -Skip 1) -join ' ').Trim(); continue }
            'mtllib' {
                foreach ($name in ($parts | Select-Object -Skip 1)) { if (-not [string]::IsNullOrWhiteSpace($name)) { $mtllibs.Add($name) } }
                continue
            }
            default { }
        }

        $lodMatch = [regex]::Match($currentObject, '_LOD([0-2])$', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        if ($lodMatch.Success -and ($op -eq 'o' -or $op -eq 'g')) {
            $lod = [int]$lodMatch.Groups[1].Value
            $existing = [string]$groups[$lod].Name
            if (-not [string]::IsNullOrWhiteSpace($existing) -and $existing -ne $currentObject) {
                Fail "OBJ must expose exactly one object/group for LOD$lod"
            }
            $groups[$lod].Name = $currentObject
            continue
        }

        if ($op -ne 'f') { continue }
        if (-not $lodMatch.Success) { continue }
        $lod = [int]$lodMatch.Groups[1].Value
        $refs = @($parts | Select-Object -Skip 1)
        if ($refs.Count -lt 3) { Fail "LOD$lod OBJ face has fewer than 3 vertices" }
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
        if (-not [string]::IsNullOrWhiteSpace($currentMaterial)) { [void]$groups[$lod].Materials.Add($currentMaterial) }
    }

    if ($mtllibs.Count -eq 0) { Fail 'OBJ must reference at least one MTL file' }
    $textures = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $sourceDir = [System.IO.Path]::GetDirectoryName($sourcePath)
    foreach ($mtlName in $mtllibs) {
        $mtlPath = [System.IO.Path]::GetFullPath((Join-Path $sourceDir $mtlName))
        if (-not (Test-Path -LiteralPath $mtlPath -PathType Leaf)) { Fail "missing MTL file: $mtlName" }
        $mtlRelative = [System.IO.Path]::GetRelativePath($Root, $mtlPath).Replace('\\', '/')
        Assert-Tracked $Root $mtlRelative 'referenced MTL'
        $mapped = 0
        foreach ($raw in Get-Content -LiteralPath $mtlPath) {
            $line = $raw.Trim()
            if ($line -match '^(?i:map_kd|map_basecolor|map_base_color)\s+(.+)$') {
                $textureName = $Matches[1].Trim()
                $texturePath = [System.IO.Path]::GetFullPath((Join-Path ([System.IO.Path]::GetDirectoryName($mtlPath)) $textureName))
                if (-not (Test-Path -LiteralPath $texturePath -PathType Leaf)) { Fail "referenced texture does not exist: $textureName" }
                $textureRelative = [System.IO.Path]::GetRelativePath($Root, $texturePath).Replace('\\', '/')
                Assert-Tracked $Root $textureRelative 'referenced texture'
                [void]$textures.Add($textureRelative)
                $mapped++
            }
        }
        if ($mapped -eq 0) { Fail "MTL has no base-color texture mapping: $mtlName" }
    }

    $lods = @()
    for ($lod = 0; $lod -lt 3; $lod++) {
        $group = $groups[$lod]
        if ([string]::IsNullOrWhiteSpace([string]$group.Name)) { Fail "OBJ is missing object/group suffix _LOD$lod" }
        if ([int]$group.Triangles -le 0) { Fail "LOD$lod has no faces" }
        if (-not [bool]$group.UvComplete) { Fail "LOD$lod is missing complete UV0 on one or more face vertices" }
        if (-not [bool]$group.NormalComplete) { Fail "LOD$lod is missing authored normals on one or more face vertices" }
        if ($group.Materials.Count -eq 0) { Fail "LOD$lod does not use a material" }
        $usedVertices = $group.Vertices.Count
        $triangles = [int]$group.Triangles
        if ($usedVertices -lt $minimumVertices[$lod] -or $usedVertices -gt $vertexBudgets[$lod]) {
            Fail "LOD$lod vertex count $usedVertices is outside policy range $($minimumVertices[$lod])..$($vertexBudgets[$lod])"
        }
        if ($triangles -lt $minimumTriangles[$lod] -or $triangles -gt $triangleBudgets[$lod]) {
            Fail "LOD$lod triangle count $triangles is outside policy range $($minimumTriangles[$lod])..$($triangleBudgets[$lod])"
        }
        $lods += [ordered]@{
            lod = $lod
            objectName = [string]$group.Name
            vertices = $usedVertices
            triangles = $triangles
            hasCompleteUv0 = [bool]$group.UvComplete
            hasCompleteNormals = [bool]$group.NormalComplete
            materialNames = @($group.Materials | Sort-Object)
        }
    }
    if (-not ($lods[0].triangles -gt $lods[1].triangles -and $lods[1].triangles -gt $lods[2].triangles)) {
        Fail 'Hero triangle counts must decrease LOD0 > LOD1 > LOD2'
    }
    return [ordered]@{
        sourceInspection = 'OBJ_STRUCTURAL_PASS'
        lods = $lods
        textureFiles = @($textures | Sort-Object)
    }
}

$RepoRoot = Resolve-RepoRoot $RepoRoot
$normalized = Normalize-Source $Source
$extension = [System.IO.Path]::GetExtension($normalized).ToLowerInvariant()
if ($extension -notin $SupportedExtensions) { Fail "unsupported Hero source format: $extension" }
if (-not ($normalized -eq $ExpectedRoot -or $normalized.StartsWith($ExpectedRoot + '/', [System.StringComparison]::Ordinal))) {
    Fail "Hero source must be under $ExpectedRoot"
}
$segments = @($normalized.Split('/') | ForEach-Object { $_.ToLowerInvariant() })
foreach ($forbidden in $ForbiddenSegments) {
    if ($segments -contains $forbidden) { Fail "Hero production source uses forbidden path segment: $forbidden" }
}
$absolute = Join-Path $RepoRoot ($normalized -replace '/', [System.IO.Path]::DirectorySeparatorChar)
if (-not (Test-Path -LiteralPath $absolute -PathType Leaf)) { Fail "Hero source does not exist: $normalized" }
Assert-Tracked $RepoRoot $normalized 'Hero source'

$result = [ordered]@{
    schemaVersion = 1
    task = 'UART-003'
    source = $normalized
    verified = $false
    productionArtApproved = $false
}
if ($extension -eq '.obj') {
    $obj = Inspect-Obj $RepoRoot $normalized
    foreach ($key in $obj.Keys) { $result[$key] = $obj[$key] }
    $result.verdict = 'READY_FOR_LICENSED_UNITY_IMPORT'
} else {
    $result.sourceInspection = 'BINARY_OR_DCC_SOURCE_NOT_INSPECTED'
    $result.verdict = 'UNITY_INSPECTION_REQUIRED'
}

$json = $result | ConvertTo-Json -Depth 8
if (-not [string]::IsNullOrWhiteSpace($Output)) {
    $outputPath = [System.IO.Path]::GetFullPath($Output)
    $artifactRoot = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot 'artifacts'))
    if (-not $outputPath.StartsWith($artifactRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        Fail 'Output must stay under <repo>/artifacts/.'
    }
    $parent = [System.IO.Path]::GetDirectoryName($outputPath)
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
    $json | Set-Content -LiteralPath $outputPath -Encoding UTF8
}
Write-Host "AFAREET_UART003_NATIVE_INTAKE verdict=$($result.verdict) source=$normalized verified=false"
Write-Output $json
