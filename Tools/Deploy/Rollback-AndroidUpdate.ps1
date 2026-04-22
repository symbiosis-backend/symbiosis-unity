param(
    [string]$Release = "1.0.0-100000",
    [string]$ServerHost = "91.99.176.77",
    [string]$ServerUser = "deploy",
    [string]$SshKeyPath = "$env:USERPROFILE\.ssh\symbiosis_unity_actions",
    [string]$ServerDownloadsPath = "/app/downloads",
    [string]$PublicBaseUrl = "http://91.99.176.77:8080"
)

$ErrorActionPreference = "Stop"

function Require-File {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing required file: $Path"
    }
}

Require-File $SshKeyPath

$remote = "$ServerUser@$ServerHost"
$sshOptions = @(
    "-i", $SshKeyPath,
    "-o", "IdentitiesOnly=yes",
    "-o", "StrictHostKeyChecking=no",
    "-o", "BatchMode=yes"
)

$releasePath = "$ServerDownloadsPath/releases/$Release"

Write-Host "Checking rollback release: $releasePath"
ssh @sshOptions $remote "test -f '$releasePath/symbiosis-latest.apk' && test -f '$releasePath/android-update.json'"
if ($LASTEXITCODE -ne 0) {
    throw "Rollback release '$Release' does not contain symbiosis-latest.apk and android-update.json."
}

Write-Host "Restoring Android APK and manifest from release '$Release'..."
ssh @sshOptions $remote @"
set -e
cp '$ServerDownloadsPath/symbiosis-latest.apk' '$ServerDownloadsPath/symbiosis-latest.apk.before-rollback' 2>/dev/null || true
cp '$ServerDownloadsPath/android-update.json' '$ServerDownloadsPath/android-update.json.before-rollback' 2>/dev/null || true
cp '$releasePath/symbiosis-latest.apk' '$ServerDownloadsPath/symbiosis-latest.apk'
cp '$releasePath/android-update.json' '$ServerDownloadsPath/android-update.json'
sha256sum '$ServerDownloadsPath/symbiosis-latest.apk'
cat '$ServerDownloadsPath/android-update.json'
"@

if ($LASTEXITCODE -ne 0) {
    throw "Rollback copy failed on server."
}

Write-Host "Verifying public Android update endpoints..."
& "$PSScriptRoot\Verify-SymbiosisServer.ps1" -BaseUrl $PublicBaseUrl

Write-Host "Android rollback to '$Release' completed."
