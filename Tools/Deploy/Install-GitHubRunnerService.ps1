param(
    [string]$RepoUrl = "https://github.com/symbiosis-backend/symbiosis-unity",
    [string]$RunnerDirectory = "C:\actions-runner\symbiosis-unity",
    [string]$RunnerName = "symbiosis-unity-windows",
    [string]$Labels = "self-hosted,Windows,X64",
    [string]$RegistrationToken = "",
    [string]$GitHubPat = "",
    [switch]$ReplaceExisting
)

$ErrorActionPreference = "Stop"

function Assert-Admin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Run this script from an elevated PowerShell window because installing the runner as a Windows service requires administrator rights."
    }
}

function Get-RepoParts {
    param([string]$Url)

    if ($Url -notmatch "github\.com[:/](?<owner>[^/]+)/(?<repo>[^/.]+)") {
        throw "Could not parse GitHub owner/repo from '$Url'."
    }

    return @{
        Owner = $Matches.owner
        Repo = $Matches.repo
    }
}

function Get-RegistrationToken {
    param(
        [string]$Token,
        [string]$Pat,
        [string]$RepoUrl
    )

    if (-not [string]::IsNullOrWhiteSpace($Token)) {
        return $Token.Trim()
    }

    if ([string]::IsNullOrWhiteSpace($Pat)) {
        throw "Provide -RegistrationToken from GitHub runner setup UI, or provide -GitHubPat with repo administration access."
    }

    $repo = Get-RepoParts -Url $RepoUrl
    $apiUrl = "https://api.github.com/repos/$($repo.Owner)/$($repo.Repo)/actions/runners/registration-token"
    $headers = @{
        Authorization = "Bearer $Pat"
        Accept = "application/vnd.github+json"
        "X-GitHub-Api-Version" = "2022-11-28"
        "User-Agent" = "SymbiosisRunnerInstaller"
    }

    $response = Invoke-RestMethod -Method Post -Uri $apiUrl -Headers $headers
    return $response.token
}

function Get-LatestRunnerDownload {
    $headers = @{
        Accept = "application/vnd.github+json"
        "User-Agent" = "SymbiosisRunnerInstaller"
    }

    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/actions/runner/releases/latest" -Headers $headers
    $asset = $release.assets | Where-Object { $_.name -match "^actions-runner-win-x64-.*\.zip$" } | Select-Object -First 1
    if ($null -eq $asset) {
        throw "Could not find the latest Windows x64 GitHub Actions runner asset."
    }

    return @{
        Version = $release.tag_name
        Url = $asset.browser_download_url
        Name = $asset.name
    }
}

Assert-Admin

$token = Get-RegistrationToken -Token $RegistrationToken -Pat $GitHubPat -RepoUrl $RepoUrl
$runner = Get-LatestRunnerDownload

Write-Host "Runner: $($runner.Version) $($runner.Name)"
Write-Host "Directory: $RunnerDirectory"

if ((Test-Path -LiteralPath $RunnerDirectory) -and $ReplaceExisting) {
    Push-Location $RunnerDirectory
    try {
        if (Test-Path ".\svc.cmd") {
            .\svc.cmd stop | Out-Host
            .\svc.cmd uninstall | Out-Host
        }

        if (Test-Path ".\config.cmd") {
            .\config.cmd remove --unattended --token $token | Out-Host
        }
    } finally {
        Pop-Location
    }

    Remove-Item -LiteralPath $RunnerDirectory -Recurse -Force
}

if (Test-Path -LiteralPath $RunnerDirectory) {
    throw "Runner directory already exists: $RunnerDirectory. Use -ReplaceExisting to reconfigure it."
}

New-Item -ItemType Directory -Force -Path $RunnerDirectory | Out-Null
$zipPath = Join-Path $RunnerDirectory $runner.Name

Write-Host "Downloading runner..."
Invoke-WebRequest -UseBasicParsing -Uri $runner.Url -OutFile $zipPath

Write-Host "Extracting runner..."
Expand-Archive -LiteralPath $zipPath -DestinationPath $RunnerDirectory -Force
Remove-Item -LiteralPath $zipPath -Force

Push-Location $RunnerDirectory
try {
    Write-Host "Configuring runner..."
    .\config.cmd `
        --unattended `
        --url $RepoUrl `
        --token $token `
        --name $RunnerName `
        --labels $Labels `
        --work "_work" `
        --replace | Out-Host

    Write-Host "Installing Windows service..."
    .\svc.cmd install | Out-Host
    .\svc.cmd start | Out-Host
    .\svc.cmd status | Out-Host
} finally {
    Pop-Location
}

Write-Host "GitHub Actions runner service is installed and started."
Write-Host "Queued jobs with labels '$Labels' should now start automatically."
