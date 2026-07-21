# Symbiosis iOS TestFlight Release

Current App Store setup date: 2026-07-21.

## App Store Connect metadata

- Existing App Store Connect app name: `DLSymbiosis`
- On-device display name: `Symbiosis`
- Bundle ID: `com.ozkullar.dlsymbiosis`
- SKU suggestion: `symbiosis-ios-001`
- Team ID: `32VM68DZD8`
- Version: `1.0.23`
- Build: `100024`
- Primary category: `Games`
- Privacy Policy URL: `https://dlsymbiosis.com/privacy`
- Support URL: `https://dlsymbiosis.com/`
- Account deletion URL: `https://dlsymbiosis.com/account-deletion`
- Current Apple account state: membership is active, the latest agreement is accepted, and the explicit App ID exists.

## Before creating the App Store record

1. In App Store Connect, create a new iOS app using the existing Bundle ID `com.ozkullar.dlsymbiosis`.
2. In Agreements, Tax, and Banking, accept any pending agreements.
3. In AdMob, create the iOS app and copy its iOS App ID into `ADMOB_IOS_APP_ID` before the release build.

Use a Mac with Unity `6000.5.1f1`, iOS Build Support, a stable Xcode release, CocoaPods, and the Apple Developer account. Windows can prepare the project, but App Store upload needs the Mac/Xcode signing toolchain.

## Fast TestFlight release path

On the Mac, from the Symbiosis project root:

```bash
chmod +x Tools/iOS/build-ios-testflight.sh

export APPLE_DEVELOPER_TEAM_ID="32VM68DZD8"
export IOS_BUNDLE_ID="com.ozkullar.dlsymbiosis"
export ADMOB_IOS_APP_ID="ca-app-pub-XXXXXXXXXXXXXXXX~YYYYYYYYYY"

Tools/iOS/build-ios-testflight.sh
```

The script exports the Unity iOS Xcode project, runs `pod install` when a `Podfile` exists, archives with Xcode, exports an App Store Connect `.ipa`, and prints the IPA path.

To upload directly after export:

```bash
export UPLOAD_TO_APP_STORE_CONNECT=true
export APPLE_ID="developer@example.com"
export APP_SPECIFIC_PASSWORD="xxxx-xxxx-xxxx-xxxx"

Tools/iOS/build-ios-testflight.sh
```

Apple also supports uploading builds through Xcode Organizer, Transporter, and `xcrun altool`.

## Manual Unity Xcode export

```bash
export APPLE_DEVELOPER_TEAM_ID="32VM68DZD8"
export IOS_BUNDLE_ID="com.ozkullar.dlsymbiosis"
export IOS_AUTOMATIC_SIGNING=true
export ADMOB_IOS_APP_ID="ca-app-pub-XXXXXXXXXXXXXXXX~YYYYYYYYYY"
export BUILD_OUTPUT_PATH="Builds/iOS/Symbiosis-iOS"

/Applications/Unity/Hub/Editor/6000.5.1f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath "$PWD" \
  -executeMethod IosCiBuild.BuildXcodeProject \
  -logFile Builds/iOS/unity-ios-export.log
```

Optional overrides:

- `BUILD_VERSION_NAME` sets App Store version, otherwise `ProjectSettings/SymbiosisVersion.json` is used.
- `IOS_BUILD_NUMBER` sets Apple build number, otherwise `versionCode` from `ProjectSettings/SymbiosisVersion.json` is used.
- `IOS_DISPLAY_NAME` sets the visible app name, default `Symbiosis`.
- `IOS_MIN_TARGET` sets minimum iOS version, default `15.0`.
- `ADMOB_IOS_APP_ID` sets the iOS Google Mobile Ads app id.
- `REQUIRE_IOS_ADMOB_ID=true` fails the export if `ADMOB_IOS_APP_ID` is empty.

For manual signing:

```bash
export IOS_AUTOMATIC_SIGNING=false
export APPLE_DEVELOPER_TEAM_ID="32VM68DZD8"
export IOS_PROVISIONING_PROFILE_ID="YOUR_PROFILE_UUID"
```

## Archive and upload

After Unity creates the Xcode project:

```bash
cd Builds/iOS/Symbiosis-iOS
pod install || true
xcodebuild -workspace Unity-iPhone.xcworkspace \
  -scheme Unity-iPhone \
  -configuration Release \
  -sdk iphoneos \
  -archivePath ../Symbiosis.xcarchive \
  archive
```

Then open Xcode Organizer, validate the archive, and upload to App Store Connect.

## Current blockers to fill before release

- Apple Developer Team ID is not stored in the project; pass `APPLE_DEVELOPER_TEAM_ID` on the Mac.
- Google Mobile Ads iOS app id is empty in `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`; pass `ADMOB_IOS_APP_ID` before an ad-enabled App Store build.
- Windows cannot produce the final App Store `.ipa`; use the Mac script in `Tools/iOS/build-ios-testflight.sh`.
