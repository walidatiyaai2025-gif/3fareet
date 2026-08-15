param(
    [string]$ManifestPath = "",
    [string]$LockPath = "",
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Fail([string]$Message) {
    throw "AFAREET_UNITY_PACKAGE_LOCK_ERROR: $Message"
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
} else {
    $RepoRoot = (Resolve-Path $RepoRoot).Path
}
if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    $ManifestPath = Join-Path $RepoRoot "unity_game/Packages/manifest.json"
}
if ([string]::IsNullOrWhiteSpace($LockPath)) {
    $LockPath = Join-Path $RepoRoot "unity_game/Packages/packages-lock.json"
}

function Read-JsonObject([string]$Path, [string]$Label) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Fail "missing file: $Path"
    }
    try {
        $payload = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    } catch {
        Fail "invalid JSON in ${Label}: $($_.Exception.Message)"
    }
    if ($null -eq $payload) {
        Fail "expected JSON object in $Label"
    }
    return $payload
}

function Get-PropertyValue($Object, [string]$Name) {
    if ($null -eq $Object) { return $null }
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }
    return $property.Value
}

function Convert-ObjectMap($Object) {
    $map = @{}
    if ($null -eq $Object) { return $map }
    foreach ($property in $Object.PSObject.Properties) {
        $map[$property.Name] = [string]$property.Value
    }
    return $map
}

$knownDirectDependencies = @{
    'com.unity.inputsystem' = @{
        'com.unity.modules.uielements' = '1.0.0'
    }
    'com.unity.ugui' = @{
        'com.unity.modules.ui' = '1.0.0'
        'com.unity.modules.imgui' = '1.0.0'
        'com.unity.modules.audio' = '1.0.0'
        'com.unity.modules.physics2d' = '1.0.0'
        'com.unity.modules.physics' = '1.0.0'
    }
    'com.unity.modules.vehicles' = @{
        'com.unity.modules.physics' = '1.0.0'
    }
}

$manifest = Read-JsonObject -Path $ManifestPath -Label 'manifest'
$lock = Read-JsonObject -Path $LockPath -Label 'package lock'
$manifestDeps = Get-PropertyValue $manifest 'dependencies'
$lockDeps = Get-PropertyValue $lock 'dependencies'
if ($null -eq $manifestDeps -or $manifestDeps.PSObject.Properties.Count -eq 0) {
    Fail "manifest dependencies must be a non-empty object"
}
if ($null -eq $lockDeps -or $lockDeps.PSObject.Properties.Count -eq 0) {
    Fail "package lock dependencies must be a non-empty object"
}

$checked = New-Object System.Collections.Generic.List[string]
foreach ($manifestProperty in $manifestDeps.PSObject.Properties) {
    $package = [string]$manifestProperty.Name
    $expectedVersion = [string]$manifestProperty.Value
    $entry = Get-PropertyValue $lockDeps $package
    if ($null -eq $entry) {
        Fail "direct dependency missing from lock: $package"
    }

    $actualVersion = [string](Get-PropertyValue $entry 'version')
    if ($actualVersion -ne $expectedVersion) {
        Fail "direct dependency version mismatch for ${package}: manifest='$expectedVersion' lock='$actualVersion'"
    }

    $depthValue = Get-PropertyValue $entry 'depth'
    if ($null -eq $depthValue -or [int]$depthValue -ne 0) {
        Fail "direct dependency must have depth 0 in lock: ${package} depth='$depthValue'"
    }

    if ($knownDirectDependencies.ContainsKey($package)) {
        $expectedChildren = $knownDirectDependencies[$package]
        $actualChildren = Convert-ObjectMap (Get-PropertyValue $entry 'dependencies')
        if ($actualChildren.Count -ne $expectedChildren.Count) {
            Fail "known dependency contract mismatch for ${package}: expectedChildren=$($expectedChildren.Count) actualChildren=$($actualChildren.Count)"
        }
        foreach ($childPackage in $expectedChildren.Keys) {
            $childVersion = [string]$expectedChildren[$childPackage]
            if (-not $actualChildren.ContainsKey($childPackage) -or $actualChildren[$childPackage] -ne $childVersion) {
                $actualChildVersion = if ($actualChildren.ContainsKey($childPackage)) { $actualChildren[$childPackage] } else { '<missing>' }
                Fail "known dependency contract mismatch for ${package}: child=$childPackage expected='$childVersion' actual='$actualChildVersion'"
            }

            $childEntry = Get-PropertyValue $lockDeps $childPackage
            if ($null -eq $childEntry) {
                Fail "resolved child dependency missing from lock: $package -> $childPackage"
            }
            $resolvedVersion = [string](Get-PropertyValue $childEntry 'version')
            if ($resolvedVersion -ne $childVersion) {
                Fail "resolved child dependency version mismatch for $package -> ${childPackage}: expected='$childVersion' actual='$resolvedVersion'"
            }
        }
    }

    $checked.Add($package)
}

Write-Host "AFAREET_UNITY_PACKAGE_LOCK_OK directDependencies=$($checked.Count) packages=$($checked -join ',') verifier=windows-powershell"
