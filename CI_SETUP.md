# Symbiosis Android CI

This project is prepared for GitHub Actions Android builds.

## Required GitHub repository secrets

Unity activation:

- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

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

## Connect local repository to GitHub

Create a GitHub repo for the Unity project, then run:

```powershell
cd "C:\Unity Projects\Symbiosis"
& "C:\Program Files\Git\cmd\git.exe" remote add origin https://github.com/YOUR_ORG_OR_USER/symbiosis-unity.git
& "C:\Program Files\Git\cmd\git.exe" push -u origin main
```

After that, every pushed commit to `main` can build and deploy a new Android APK.
