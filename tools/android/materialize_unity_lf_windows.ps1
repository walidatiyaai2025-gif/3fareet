param(
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Fail([string]$Message) {
    Write-Host "::error::$Message"
    throw "AFAREET_UNITY_LF_MATERIALIZE_ERROR: $Message"
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
} else {
    $RepoRoot = (Resolve-Path $RepoRoot).Path
}

$git = Get-Command git -ErrorAction SilentlyContinue
if ($null -eq $git) {
    Fail "git is required"
}

$patterns = @(
    "unity_game/ProjectSettings/*.asset",
    "unity_game/ProjectSettings/*.txt",
    "unity_game/Packages/*.json"
)

$paths = @(& $git.Source -C $RepoRoot ls-files -- @patterns 2>$null)
if ($LASTEXITCODE -ne 0) {
    Fail "git ls-files failed with exit code $LASTEXITCODE"
}
$paths = @($paths | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($paths.Count -eq 0) {
    Fail "no tracked Unity metadata files matched the LF materialization contract"
}

# A long-lived Windows clone can retain pre-.gitattributes CRLF bytes even when
# git status is clean. Git normalizes those bytes when comparing to the index,
# so reset --hard is not guaranteed to rewrite the physical working-tree copy.
# Before Unity starts, rematerialize only the files that are explicitly governed
# by text/eol=lf. The orchestrator immediately asserts that the repository is
# still clean after this byte-only repair.
$rewritten = 0
foreach ($relativePath in $paths) {
    $attrLines = @(& $git.Source -C $RepoRoot check-attr text eol -- $relativePath 2>$null)
    if ($LASTEXITCODE -ne 0) {
        Fail "${relativePath}: git check-attr failed"
    }

    $textValue = $null
    $eolValue = $null
    foreach ($line in $attrLines) {
        if ($line -match '^(.*?):\s+text:\s+(.*)$') {
            $textValue = $Matches[2].Trim()
        } elseif ($line -match '^(.*?):\s+eol:\s+(.*)$') {
            $eolValue = $Matches[2].Trim()
        }
    }

    if ($textValue -ne 'set' -or $eolValue -ne 'lf') {
        Fail "${relativePath}: refusing to rewrite because attributes are text='$textValue' eol='$eolValue', expected text='set' eol='lf'"
    }

    $filePath = Join-Path $RepoRoot $relativePath
    if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
        Fail "${relativePath}: tracked working-tree file is missing"
    }

    $bytes = [System.IO.File]::ReadAllBytes($filePath)
    $containsCrLf = $false
    for ($i = 0; $i -lt ($bytes.Length - 1); $i++) {
        if ($bytes[$i] -eq 13 -and $bytes[$i + 1] -eq 10) {
            $containsCrLf = $true
            break
        }
    }
    if (-not $containsCrLf) {
        continue
    }

    $normalized = New-Object System.Collections.Generic.List[byte]
    for ($i = 0; $i -lt $bytes.Length; $i++) {
        if ($bytes[$i] -eq 13 -and ($i + 1) -lt $bytes.Length -and $bytes[$i + 1] -eq 10) {
            $normalized.Add([byte]10)
            $i++
        } else {
            $normalized.Add($bytes[$i])
        }
    }

    [System.IO.File]::WriteAllBytes($filePath, $normalized.ToArray())
    $rewritten++
}

Write-Host "AFAREET_UNITY_LF_MATERIALIZED files=$($paths.Count) rewritten=$rewritten eol=lf"
