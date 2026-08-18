param(
    [Parameter(Mandatory = $true)][string]$Fbx,
    [Parameter(Mandatory = $true)][string]$Glb,
    [Parameter(Mandatory = $true)][string]$Blend,
    [string]$Receipt = "",
    [string]$RefinementManifest = "",
    [string]$Output = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$ExpectedSchema = 1
$ExpectedTask = 'UART-003'
$ExpectedClassification = 'REFINEMENT_CANDIDATE'
$ExpectedOrigin = 'EXTERNAL_USER_HANDOFF'
$ExpectedBoundary = 'BYTE_IDENTITY_ONLY_LICENSED_UNITY_INSPECTION_REQUIRED'
$ExpectedFbxRole = 'UNITY_REFINEMENT_INTAKE'
$ExpectedGlbRole = 'INSPECTION_COMPANION'
$ExpectedBlendRole = 'DCC_SOURCE_COMPANION'
$Verdict = 'REFINEMENT_HANDOFF_MATCH_NOT_PRODUCTION'

function Fail([string]$Message) {
    throw "AFAREET_HERO_REFINEMENT_HANDOFF_NATIVE_BLOCKED: $Message"
}

function Get-Value($Object, [string]$Name) {
    if ($null -eq $Object) { return $null }
    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }
    return $property.Value
}

function Read-JsonObject([string]$Path, [string]$Label) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        Fail "$Label does not exist: $Path"
    }
    try {
        $payload = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    } catch {
        Fail "$Label is not valid JSON: $($_.Exception.Message)"
    }
    if ($null -eq $payload) { Fail "$Label root must be a JSON object" }
    return $payload
}

function Require-False($Object, [string]$Key, [string]$Label) {
    $value = Get-Value $Object $Key
    if ($null -eq $value -or $value -isnot [bool] -or $value) {
        $foundType = if ($null -eq $value) { '<null>' } else { $value.GetType().FullName }
        Fail "$Label.$Key must be JSON boolean false, found '$value' type=$foundType"
    }
}

function Require-Text($Object, [string]$Key, [string]$Expected, [string]$Label) {
    $value = [string](Get-Value $Object $Key)
    if ($value -ne $Expected) {
        Fail "$Label.$Key must be '$Expected', found '$value'"
    }
}

function Require-PositiveInt64($Object, [string]$Key, [string]$Label) {
    $raw = Get-Value $Object $Key
    if ($null -eq $raw -or $raw -is [bool]) { Fail "$Label.$Key must be a positive integer" }
    $value = 0L
    if (-not [long]::TryParse([string]$raw, [ref]$value) -or $value -le 0) {
        Fail "$Label.$Key must be a positive integer, found '$raw'"
    }
    return $value
}

function Require-Sha256($Object, [string]$Key, [string]$Label) {
    $value = ([string](Get-Value $Object $Key)).Trim().ToLowerInvariant()
    if ($value -notmatch '^[0-9a-f]{64}$') {
        Fail "$Label.$Key must be a lowercase SHA-256, found '$value'"
    }
    return $value
}

function Validate-FileRecord($Record, [string]$Label, [string]$ExpectedRole) {
    if ($null -eq $Record) { Fail "receipt.files.$Label must be an object" }
    $fileName = ([string](Get-Value $Record 'fileName')).Trim()
    if ([string]::IsNullOrWhiteSpace($fileName)) { Fail "receipt.files.$Label.fileName must be non-blank" }
    $size = Require-PositiveInt64 $Record 'sizeBytes' "receipt.files.$Label"
    $sha = Require-Sha256 $Record 'sha256' "receipt.files.$Label"
    Require-Text $Record 'role' $ExpectedRole "receipt.files.$Label"
    return [ordered]@{
        fileName = $fileName
        sizeBytes = $size
        sha256 = $sha
        role = $ExpectedRole
    }
}

function Verify-ExactFile([string]$Path, $Record, [string]$Label) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { Fail "$Label file does not exist: $Path" }
    $resolved = (Resolve-Path -LiteralPath $Path).Path
    $actualName = [System.IO.Path]::GetFileName($resolved)
    if ($actualName -ne [string]$Record.fileName) {
        Fail "$Label file name mismatch: expected=$($Record.fileName) actual=$actualName"
    }
    $actualSize = (Get-Item -LiteralPath $resolved).Length
    if ($actualSize -ne [long]$Record.sizeBytes) {
        Fail "$Label size mismatch: expected=$($Record.sizeBytes) actual=$actualSize"
    }
    $actualSha = (Get-FileHash -LiteralPath $resolved -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualSha -ne [string]$Record.sha256) {
        Fail "$Label SHA-256 mismatch: expected=$($Record.sha256) actual=$actualSha"
    }
    return [ordered]@{
        fileName = $actualName
        sizeBytes = $actualSize
        sha256 = $actualSha
        byteIdentityMatch = $true
    }
}

if ([string]::IsNullOrWhiteSpace($Receipt)) {
    $Receipt = Join-Path $PSScriptRoot 'hero_refinement_handoff_receipt.json'
}
if ([string]::IsNullOrWhiteSpace($RefinementManifest)) {
    $RefinementManifest = Join-Path $PSScriptRoot 'hero_refinement_candidate_manifest.json'
}

$receiptObject = Read-JsonObject $Receipt 'handoff receipt'
$manifestObject = Read-JsonObject $RefinementManifest 'refinement manifest'

$schema = Get-Value $receiptObject 'schemaVersion'
if ($schema -ne $ExpectedSchema) { Fail "receipt.schemaVersion must be $ExpectedSchema, found '$schema'" }
Require-Text $receiptObject 'task' $ExpectedTask 'receipt'
Require-Text $receiptObject 'classification' $ExpectedClassification 'receipt'
Require-Text $receiptObject 'origin' $ExpectedOrigin 'receipt'
Require-Text $receiptObject 'inspectionBoundary' $ExpectedBoundary 'receipt'
foreach ($key in @('productionGate', 'visualAcceptance', 'ownerApproval', 'verified')) {
    Require-False $receiptObject $key 'receipt'
}

$manifestSchema = Get-Value $manifestObject 'schemaVersion'
if ($manifestSchema -ne $ExpectedSchema) { Fail "refinement manifest schemaVersion must be $ExpectedSchema, found '$manifestSchema'" }
Require-Text $manifestObject 'classification' $ExpectedClassification 'refinement manifest'
Require-False $manifestObject 'productionGate' 'refinement manifest'
Require-False $manifestObject 'visualAcceptance' 'refinement manifest'

$files = Get-Value $receiptObject 'files'
if ($null -eq $files) { Fail 'receipt.files must be an object' }
$fbxRecord = Validate-FileRecord (Get-Value $files 'fbx') 'fbx' $ExpectedFbxRole
$glbRecord = Validate-FileRecord (Get-Value $files 'glb') 'glb' $ExpectedGlbRole
$blendRecord = Validate-FileRecord (Get-Value $files 'blend') 'blend' $ExpectedBlendRole

$manifestFileName = ([string](Get-Value $manifestObject 'sourceFileName')).Trim()
$manifestSha = Require-Sha256 $manifestObject 'sha256' 'refinement manifest'
$manifestSize = Require-PositiveInt64 $manifestObject 'sizeBytes' 'refinement manifest'
if ($fbxRecord.fileName -ne $manifestFileName) {
    Fail 'receipt FBX fileName must match hero_refinement_candidate_manifest.json'
}
if ($fbxRecord.sha256 -ne $manifestSha) {
    Fail 'receipt FBX SHA-256 must match hero_refinement_candidate_manifest.json'
}
if ($fbxRecord.sizeBytes -ne $manifestSize) {
    Fail 'receipt FBX sizeBytes must match hero_refinement_candidate_manifest.json'
}

$uniqueNames = @(@($fbxRecord.fileName, $glbRecord.fileName, $blendRecord.fileName) | Select-Object -Unique)
if ($uniqueNames.Count -ne 3) { Fail 'handoff file names must be unique' }

$verifiedFiles = [ordered]@{
    fbx = Verify-ExactFile $Fbx $fbxRecord 'FBX'
    glb = Verify-ExactFile $Glb $glbRecord 'GLB'
    blend = Verify-ExactFile $Blend $blendRecord 'BLEND'
}

$result = [ordered]@{
    schemaVersion = 1
    task = $ExpectedTask
    classification = $ExpectedClassification
    origin = $ExpectedOrigin
    verdict = $Verdict
    handoffByteIdentityMatch = $true
    files = $verifiedFiles
    productionGate = $false
    visualAcceptance = $false
    ownerApproval = $false
    verified = $false
    inspectionBoundary = $ExpectedBoundary
}

$json = $result | ConvertTo-Json -Depth 8
if (-not [string]::IsNullOrWhiteSpace($Output)) {
    $outputPath = [System.IO.Path]::GetFullPath($Output)
    if (Test-Path -LiteralPath $outputPath) { Fail "refusing to overwrite existing result: $outputPath" }
    $parent = [System.IO.Path]::GetDirectoryName($outputPath)
    if (-not [string]::IsNullOrWhiteSpace($parent)) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
    $json | Set-Content -LiteralPath $outputPath -Encoding UTF8
}

Write-Host "AFAREET_HERO_REFINEMENT_HANDOFF_NATIVE_OK verdict=$Verdict fbxSha256=$($verifiedFiles.fbx.sha256) productionGate=false verified=false"
Write-Output $json
