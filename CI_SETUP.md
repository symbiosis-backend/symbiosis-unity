# Symbiosis Android CI

This project is prepared for GitHub Actions Android builds.

## Required GitHub repository secrets

Unity activation:

- `UNITY_LICENSE`
- `UNITY_EMAIL` - optional when `UNITY_LICENSE` is used
- `UNITY_PASSWORD` - optional when `UNITY_LICENSE` is used

VPS upload:

- `SERVER_HOST` - for example `91.99.176.77`
- `SERVER_USER` - SSH user that can write to the backend downloads folder
- `SERVER_SSH_KEY` - private SSH key for that user
- `SERVER_DOWNLOADS_PATH` - optional, defaults to `/opt/symbiosis/backend/downloads`

Release signing, recommended before public testing:

- `ANDROID_KEYSTORE_BASE64` - base64 encoded `.keystore` file
- `ANDROID_KEYSTORE_PASS`
- `ANDROID_KEY_ALIAS_NAME`
- `ANDROID_KEY_ALIAS_PASS`

If the keystore secrets are missing, Unity default signing is used. That is okay for first CI tests, but Android updates only install cleanly over the previous APK when the signing key is the same.

## Workflow

### Unity Personal license

GameCI v4 uses the Unity license that Unity Hub creates on a local machine. This is the right flow when the Unity account signs in through Google or 2FA and there is no password for CI.

1. Open Unity Hub locally.
2. Sign in to Unity Hub.
3. Open `Preferences` > `Licenses`.
4. Click `Add`.
5. Select `Get a free personal license`.
6. Find the license file at `C:\ProgramData\Unity\Unity_lic.ulf`.
7. Open `Unity_lic.ulf` as text.
8. Add the whole file contents as the repository secret `UNITY_LICENSE`.

After `UNITY_LICENSE` is set, the Android build workflow can run without `UNITY_PASSWORD`.

### Android build

The workflow file is:

`.github/workflows/android-build-deploy.yml`

It can be started manually from GitHub Actions, or automatically on push to `main`.
CI Android `versionCode` starts at `1000 + GitHub run number`, so the first CI APK can update over the current local `versionCode: 1` build.

The build creates:

- `Builds/Android/symbiosis-latest.apk`
- `Builds/Android/android-update.json`

Then it uploads both files to the VPS downloads folder. The backend serves the APK from:

`http://91.99.176.77:8080/downloads/symbiosis-latest.apk`

The game checks:

`http://91.99.176.77:8080/updates/android`

The deployment status endpoint is:

`http://91.99.176.77:8080/updates/android/status`

Local verification:

```powershell
cd "C:\Unity Projects\Symbiosis"
.\Tools\Deploy\Verify-SymbiosisServer.ps1
```

Manual upload, when you have a valid VPS SSH user/key:

```powershell
cd "C:\Unity Projects\Symbiosis"
.\Tools\Deploy\Publish-AndroidUpdate.ps1 `
  -ServerUser deploy `
  -SshKeyPath "$env:USERPROFILE\.ssh\symbiosis_unity_actions"
```

## Self-hosted Windows runner as a service

The Android workflow runs on a self-hosted Windows runner with these labels:

`self-hosted`, `Windows`, `X64`

Install it as a Windows service from an elevated PowerShell window:

```powershell
cd "C:\Unity Projects\Symbiosis"
.\Tools\Deploy\Install-GitHubRunnerService.ps1 -RegistrationToken "<runner-registration-token>"
```

You can get the registration token from GitHub:

`symbiosis-unity` > `Settings` > `Actions` > `Runners` > `New self-hosted runner`.

The script downloads the latest Windows x64 GitHub Actions runner, configures it for this repository, installs it as a service, and starts it. Once the service is online, queued Android builds should begin automatically.

## Connect local repository to GitHub

Create a GitHub repo for the Unity project, then run:

```powershell
cd "C:\Unity Projects\Symbiosis"
& "C:\Program Files\Git\cmd\git.exe" remote add origin https://github.com/YOUR_ORG_OR_USER/symbiosis-unity.git
& "C:\Program Files\Git\cmd\git.exe" push -u origin main
```

After that, every pushed commit to `main` can build and deploy a new Android APK.
