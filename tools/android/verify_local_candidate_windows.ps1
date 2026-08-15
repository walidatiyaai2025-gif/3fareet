param(
    [Parameter(Mandatory = $true)][string]$TestMetadata,
    [Parameter(Mandatory = $true)][string]$BuildMetadata,
    [Parameter(Mandatory = $true)][string]$Apk,
    [Parameter(Mandatory = $true)][string]$Output
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ExpectedUnityVersion = "6000.5.8f1"
$ExpectedPackageId = "com.fiftysolutions.afareetunity3d"
$ExpectedAbi = "arm64-v8a"
$ExpectedMinSdk = 26
$ExpectedArtifact = "afareet-unity3d-debug.apk"

function Fail([string]$Message) {
    throw "AFAREET_LOCAL_CANDIDATE_ERROR: $Message"
}

function Read-JsonObject([string]$Path, [string]$Label) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Fail "Required metadata file is missing: $Path"
    }
    try {
        $payload = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    } catch {
        Fail "Could not read valid JSON from ${Label}: $($_.Exception.Message)"
    }
    if ($null -eq $payload) {
        Fail "Metadata root must be a JSON object: $Path"
    }
    return $payload
}

function Get-Value($Object, [string]$Name) {
    if ($null -eq $Object) { return $null }
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }
    return $property.Value
}

function Require-Bool($Object, [string]$Key, [bool]$Expected, [string]$Label) {
    $value = Get-Value $Object $Key
    if ($null -eq $value -or [bool]$value -ne $Expected) {
        Fail "$Label.$Key must be $($Expected.ToString().ToLowerInvariant()), found '$value'"
    }
}

function Normalize-Sha($Value, [string]$Label) {
    $sha = ([string]$Value).Trim().ToLowerInvariant()
    if ($sha -notmatch '^[0-9a-f]{40}$') {
        Fail "$Label must contain a full 40-character Git SHA, found '$Value'"
    }
    return $sha
}

function Verify-TestMode($Mode, [string]$Label) {
    if ($null -eq $Mode) {
        Fail "$Label test metadata is missing or invalid"
    }

    $result = ([string](Get-Value $Mode 'result')).Trim()
    try {
        $total = [int](Get-Value $Mode 'total')
        $passed = [int](Get-Value $Mode 'passed')
        $failed = [int](Get-Value $Mode 'failed')
        $skipped = [int](Get-Value $Mode 'skipped')
        $inconclusive = [int](Get-Value $Mode 'inconclusive')
    } catch {
        Fail "$Label counters are not integers"
    }

    if ($total -le 0) { Fail "$Label executed zero tests" }
    if ($passed -le 0) { Fail "$Label contains no passing tests; all-skipped/non-executed evidence is not release eligible" }
    if ($failed -ne 0) { Fail "$Label contains failed tests: $failed" }
    if ($inconclusive -ne 0) { Fail "$Label contains inconclusive tests: $inconclusive" }
    if ($result.ToLowerInvariant() -notin @('passed', 'success')) { Fail "$Label result is not passing: '$result'" }
    if ([Math]::Min([Math]::Min($passed, $failed), [Math]::Min($skipped, $inconclusive)) -lt 0) {
        Fail "$Label counters cannot be negative: total=$total passed=$passed failed=$failed skipped=$skipped inconclusive=$inconclusive"
    }
    $accounted = $passed + $failed + $skipped + $inconclusive
    if ($accounted -ne $total) {
        Fail "$Label counters do not account for every test: total=$total accounted=$accounted passed=$passed failed=$failed skipped=$skipped inconclusive=$inconclusive"
    }

    return [ordered]@{
        result = $result
        total = $total
        passed = $passed
        failed = $failed
        skipped = $skipped
        inconclusive = $inconclusive
    }
}

if (-not (Test-Path -LiteralPath $Apk -PathType Leaf) -or (Get-Item -LiteralPath $Apk).Length -le 0) {
    Fail "APK is missing or empty: $Apk"
}

$tests = Read-JsonObject -Path $TestMetadata -Label 'test metadata'
$build = Read-JsonObject -Path $BuildMetadata -Label 'build metadata'

if (([string](Get-Value $tests 'source')) -ne 'local-windows-licensed-unity-tests') {
    Fail "Test metadata source is not the supported local Unity test path"
}
if (([string](Get-Value $build 'source')) -ne 'local-windows-licensed-unity') {
    Fail "Build metadata source is not a clean local Unity build"
}

Require-Bool -Object $tests -Key 'releaseEvidenceEligible' -Expected $true -Label 'testMetadata'
Require-Bool -Object $tests -Key 'dirtyTree' -Expected $false -Label 'testMetadata'
Require-Bool -Object $build -Key 'releaseEvidenceEligible' -Expected $true -Label 'buildMetadata'
Require-Bool -Object $build -Key 'gitDirty' -Expected $false -Label 'buildMetadata'

$testSha = Normalize-Sha (Get-Value $tests 'gitSha') 'testMetadata.gitSha'
$buildSha = Normalize-Sha (Get-Value $build 'gitSha') 'buildMetadata.gitSha'
if ($testSha -ne $buildSha) {
    Fail "Git SHA mismatch: tests=$testSha build=$buildSha"
}

$testUnity = [string](Get-Value $tests 'unityVersion')
$buildUnity = [string](Get-Value $build 'unityVersion')
if ($testUnity -ne $ExpectedUnityVersion -or $buildUnity -ne $ExpectedUnityVersion) {
    Fail "Unity version must be ${ExpectedUnityVersion}: tests='$testUnity' build='$buildUnity'"
}

$edit = Verify-TestMode (Get-Value $tests 'editMode') 'EditMode'
$play = Verify-TestMode (Get-Value $tests 'playMode') 'PlayMode'

if (([string](Get-Value $build 'artifact')) -ne $ExpectedArtifact) {
    Fail "Unexpected artifact name: '$(Get-Value $build 'artifact')'"
}
if (([string](Get-Value $build 'packageId')) -ne $ExpectedPackageId) {
    Fail "Unexpected package id: '$(Get-Value $build 'packageId')'"
}
try { $minSdk = [int](Get-Value $build 'minSdk') } catch { Fail "Unexpected minSdk: '$(Get-Value $build 'minSdk')'" }
if ($minSdk -ne $ExpectedMinSdk) { Fail "Unexpected minSdk: '$minSdk'" }
if (([string](Get-Value $build 'abi')) -ne $ExpectedAbi) {
    Fail "Unexpected ABI: '$(Get-Value $build 'abi')'"
}

$declaredHash = ([string](Get-Value $build 'sha256')).Trim().ToLowerInvariant()
if ($declaredHash -notmatch '^[0-9a-f]{64}$') {
    Fail "Build metadata SHA-256 is invalid: '$declaredHash'"
}
$actualHash = (Get-FileHash -LiteralPath $Apk -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualHash -ne $declaredHash) {
    Fail "APK SHA-256 mismatch: metadata=$declaredHash actual=$actualHash"
}

$actualSize = (Get-Item -LiteralPath $Apk).Length
try { $declaredSize = [long](Get-Value $build 'sizeBytes') } catch { Fail "Build metadata sizeBytes is invalid" }
if ($actualSize -ne $declaredSize) {
    Fail "APK size mismatch: metadata=$declaredSize actual=$actualSize"
}

$branch = [string](Get-Value $build 'gitBranch')
if ([string]::IsNullOrWhiteSpace($branch)) {
    $branch = [string](Get-Value $tests 'gitBranch')
}

$resolvedApk = (Resolve-Path -LiteralPath $Apk).Path
$manifest = [ordered]@{
    schemaVersion = 1
    generatedAtUtc = [DateTime]::UtcNow.ToString('o')
    candidateType = 'local-windows-licensed-unity'
    gitSha = $testSha
    gitBranch = $branch
    unityVersion = $ExpectedUnityVersion
    packageId = $ExpectedPackageId
    minSdk = $ExpectedMinSdk
    abi = $ExpectedAbi
    apk = [ordered]@{
        path = $resolvedApk
        fileName = [System.IO.Path]::GetFileName($resolvedApk)
        sizeBytes = $actualSize
        sha256 = $actualHash
    }
    unityTests = [ordered]@{
        editMode = $edit
        playMode = $play
    }
    releaseEvidenceEligible = $true
    readyForDeviceEvidence = $true
    verified = $false
    verdict = 'READY_FOR_PHYSICAL_DEVICE_EVIDENCE'
    notes = @(
        'This manifest proves same-SHA local Unity test/build integrity only.',
        'It does not make GitHub Unity Production CI green.',
        'It does not replace physical-device, performance, visual, or human approval gates.'
    )
}

$outputDir = Split-Path -Parent $Output
if (-not [string]::IsNullOrWhiteSpace($outputDir)) {
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
}
$manifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $Output -Encoding UTF8

Write-Host "AFAREET_LOCAL_CANDIDATE_READY gitSha=$testSha apkSha256=$actualHash output=$Output verifier=windows-powershell"
