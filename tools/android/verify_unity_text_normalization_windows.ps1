param(
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Fail([string]$Message) {
    throw "AFAREET_UNITY_TEXT_NORMALIZATION_ERROR: $Message"
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
    Fail "no tracked Unity metadata files matched the LF normalization contract"
}

$failures = New-Object System.Collections.Generic.List[string]
foreach ($relativePath in $paths) {
    $attrLines = @(& $git.Source -C $RepoRoot check-attr text eol -- $relativePath 2>$null)
    if ($LASTEXITCODE -ne 0) {
        $failures.Add("${relativePath}: git check-attr failed")
        continue
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

    if ($textValue -ne 'set') {
        $failures.Add("${relativePath}: text='$textValue', expected 'set'")
    }
    if ($eolValue -ne 'lf') {
        $failures.Add("${relativePath}: eol='$eolValue', expected 'lf'")
    }

    $filePath = Join-Path $RepoRoot ($relativePath -replace '/', '\')
    if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
        $failures.Add("${relativePath}: tracked working-tree file is missing")
        continue
    }

    $bytes = [System.IO.File]::ReadAllBytes($filePath)
    for ($i = 0; $i -lt ($bytes.Length - 1); $i++) {
        if ($bytes[$i] -eq 13 -and $bytes[$i + 1] -eq 10) {
            $failures.Add("${relativePath}: working-tree content contains CRLF bytes")
            break
        }
    }
}

if ($failures.Count -gt 0) {
    Fail ($failures -join '; ')
}

Write-Host "AFAREET_UNITY_TEXT_NORMALIZATION_OK files=$($paths.Count) eol=lf verifier=windows-powershell"
