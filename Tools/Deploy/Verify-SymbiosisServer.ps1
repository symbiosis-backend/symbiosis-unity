param(
    [string]$BaseUrl = "http://91.99.176.77:8080",
    [int]$TimeoutSec = 15
)

$ErrorActionPreference = "Stop"
$base = $BaseUrl.TrimEnd("/")

function Invoke-JsonCheck {
    param(
        [string]$Name,
        [string]$Url
    )

    Write-Host "== $Name =="
    $response = Invoke-WebRequest -UseBasicParsing -Uri $Url -TimeoutSec $TimeoutSec
    Write-Host "HTTP $($response.StatusCode) $Url"
    $json = $response.Content | ConvertFrom-Json
    $json | ConvertTo-Json -Depth 8
    return $json
}

function Invoke-HeadCheck {
    param(
        [string]$Name,
        [string]$Url,
        [switch]$Required
    )

    Write-Host "== $Name =="
    try {
        $response = Invoke-WebRequest -UseBasicParsing -Method Head -Uri $Url -TimeoutSec $TimeoutSec
        Write-Host "HTTP $($response.StatusCode) $Url"
        Write-Host "Content-Length: $($response.Headers['Content-Length'])"
        return $true
    } catch {
        Write-Host "FAILED $Url"
        Write-Host $_.Exception.Message
        if ($Required) {
            throw
        }
        return $false
    }
}

$health = Invoke-JsonCheck -Name "Backend health" -Url "$base/health"
$manifest = Invoke-JsonCheck -Name "Android update manifest" -Url "$base/updates/android"
$multiplayer = Invoke-JsonCheck -Name "Multiplayer config" -Url "$base/multiplayer/config"

$status = $null
try {
    $status = Invoke-JsonCheck -Name "Downloads status" -Url "$base/updates/android/status"
} catch {
    Write-Host "Downloads status endpoint is not deployed yet."
}

$apkOk = Invoke-HeadCheck -Name "APK download" -Url "$base/downloads/symbiosis-latest.apk"

if ($status -ne $null -and $status.addressables.fileCount -gt 0) {
    Write-Host "Addressables files reported: $($status.addressables.fileCount)"
} else {
    Write-Host "Addressables files were not reported by the server."
}

if (-not $health.success) {
    throw "Backend health check failed."
}

if (-not $manifest.success) {
    throw "Android update manifest check failed."
}

if (-not $multiplayer.success) {
    throw "Multiplayer config check failed."
}

if (-not $apkOk) {
    throw "APK is not publicly downloadable. Publish Builds/Android/symbiosis-latest.apk to the server downloads folder."
}

Write-Host "Symbiosis server is ready for Android update checks."
