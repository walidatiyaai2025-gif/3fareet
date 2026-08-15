param(
    [string]$UnityPath = "",
    [string]$RepoRoot = "",
    [string]$PythonPath = ""
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Fail([string]$Message) {
    throw "AFAREET_LOCAL_CANDIDATE_ERROR: $Message"
}
