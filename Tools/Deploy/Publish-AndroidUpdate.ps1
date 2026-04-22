param(
    [string]$ServerHost = "91.99.176.77",
    [string]$ServerUser = "deploy",
    [string]$SshKeyPath = "$env:USERPROFILE\.ssh\symbiosis_unity_actions",
    [string]$ServerDownloadsPath = "/app/downloads",
    [string]$ApkPath = "Builds/Android/symbiosis-latest.apk",
    [string]$ManifestPath = "Builds/Android/android-update.json",
    [string]$AddressablesPath = "ServerData/Android",
    [string]$PublicBaseUrl = "http://91.99.176.77:8080"
)

$ErrorActionPreference = "Stop"

function Require-File {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing required file: $Path"
    }
}

function Require-Directory {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "Missing required directory: $Path"
    }
}

Require-File $SshKeyPath
Require-File $ApkPath
Require-File $ManifestPath
$remote = "$ServerUser@$ServerHost"
$sshOptions = @("-i", $SshKeyPath, "-o", "IdentitiesOnly=yes", "-o", "StrictHostKeyChecking=no", "-o", "BatchMode=yes")

Write-Host "Creating remote downloads folders..."
ssh @sshOptions $remote "mkdir -p '$ServerDownloadsPath' '$ServerDownloadsPath/addressables/Android'"

Write-Host "Uploading APK and update manifest..."
scp @sshOptions $ApkPath $ManifestPath "$remote`:$ServerDownloadsPath/"

if (Test-Path -LiteralPath $AddressablesPath -PathType Container) {
    Write-Host "Uploading Addressables..."
    $root = (Resolve-Path -LiteralPath $AddressablesPath).Path
    $files = Get-ChildItem -LiteralPath $root -Recurse -File
    foreach ($file in $files) {
        $relative = [IO.Path]::GetRelativePath($root, $file.FullName).Replace("\", "/")
        $remoteDir = [IO.Path]::GetDirectoryName($relative)
        if ([string]::IsNullOrWhiteSpace($remoteDir)) {
            $remoteDir = "."
        } else {
            $remoteDir = $remoteDir.Replace("\", "/")
        }

        ssh @sshOptions $remote "mkdir -p '$ServerDownloadsPath/addressables/Android/$remoteDir'"
        scp @sshOptions $file.FullName "$remote`:$ServerDownloadsPath/addressables/Android/$relative"
    }
} else {
    Write-Host "No Addressables directory found at '$AddressablesPath'. Skipping Addressables upload."
}

Write-Host "Verifying remote files..."
ssh @sshOptions $remote "ls -lh '$ServerDownloadsPath/symbiosis-latest.apk' '$ServerDownloadsPath/android-update.json' && find '$ServerDownloadsPath/addressables/Android' -maxdepth 2 -type f | sort | tail -20"

Write-Host "Verifying public URLs..."
& "$PSScriptRoot\Verify-SymbiosisServer.ps1" -BaseUrl $PublicBaseUrl
