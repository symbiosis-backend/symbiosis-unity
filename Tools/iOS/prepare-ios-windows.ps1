param(
    [string]$UnityEditor = "C:\Program Files\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe"
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$logDirectory = Join-Path $projectRoot "Builds\iOS"
$logFile = Join-Path $logDirectory "unity-ios-prepare.log"

if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) {
    throw "Unity Editor was not found: $UnityEditor"
}

New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null

$unityArguments = @(
    "-batchmode",
    "-quit",
    "-projectPath", "`"$projectRoot`"",
    "-executeMethod", "IosCiBuild.PrepareIosSettings",
    "-logFile", "`"$logFile`""
)

$unityProcess = Start-Process `
    -FilePath $UnityEditor `
    -ArgumentList $unityArguments `
    -WindowStyle Hidden `
    -Wait `
    -PassThru

if ($unityProcess.ExitCode -ne 0) {
    throw "Unity iOS preparation failed with exit code $($unityProcess.ExitCode). See $logFile"
}

Write-Output "iOS Player Settings are ready. Log: $logFile"
