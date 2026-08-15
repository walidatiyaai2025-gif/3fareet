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

function Test-ContainsCrLf([string]$FilePath) {
    $bytes = [System.IO.File]::ReadAllBytes($FilePath)
    for ($i = 0; $i -lt ($bytes.Length - 1); $i++) {
        if ($bytes[$i] -eq 13 -and $bytes[$i + 1] -eq 10) {
            return $true
        }
    }
    return $false
}

# A long-lived Windows clone can retain stale physical CRLF bytes from before
# .gitattributes pinned these Unity metadata files to LF. Do not rewrite those
# bytes directly: on some Windows Git configurations that can leave the
# working tree/index stat state disagreeing even though the canonical blob is
# already LF. After validating text/eol=lf, ask Git itself to force the path
# back out of the current index. This preserves HEAD/index content and applies
# Git's own working-tree conversion rules.
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
        Fail "${relativePath}: refusing to rematerialize because attributes are text='$textValue' eol='$eolValue', expected text='set' eol='lf'"
    }

    $filePath = Join-Path $RepoRoot $relativePath
    if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
        Fail "${relativePath}: tracked working-tree file is missing"
    }

    if (-not (Test-ContainsCrLf -FilePath $filePath)) {
        continue
    }

    & $git.Source -C $RepoRoot checkout-index --force -- $relativePath 2>$null
    if ($LASTEXITCODE -ne 0) {
        Fail "${relativePath}: git checkout-index --force failed with exit code $LASTEXITCODE"
    }

    if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
        Fail "${relativePath}: file disappeared during git rematerialization"
    }
    if (Test-ContainsCrLf -FilePath $filePath) {
        Fail "${relativePath}: Git rematerialization still produced CRLF despite text eol=lf"
    }

    $rewritten++
}

Write-Host "AFAREET_UNITY_LF_MATERIALIZED files=$($paths.Count) rewritten=$rewritten eol=lf source=git-index"
