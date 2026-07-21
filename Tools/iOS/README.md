# Symbiosis iOS build

The Unity project targets iPhone, landscape orientation, iOS 15.0+, IL2CPP, and automatic Apple signing.

## Windows preparation

Run `prepare-ios-windows.ps1` to validate scripts and persist the iOS Player Settings. A Windows PC cannot create a signed IPA; the final Xcode archive must be produced on macOS.

## Mac prerequisites

- macOS with stable Xcode and Command Line Tools.
- Unity `6000.5.1f1` with iOS Build Support.
- CocoaPods (`pod`) for Google Mobile Ads dependencies.
- An active Apple Developer Program team with the latest agreement accepted.
- An iOS AdMob App ID. Do not reuse the Android App ID.

## TestFlight build

1. Export the values from `env.example` using real non-secret identifiers.
2. Run `bash Tools/iOS/build-ios-testflight.sh` from any directory.
3. The generated IPA is written to `Builds/iOS/ipa`.
4. Upload through Xcode Organizer or enable the optional upload block in the script.

Keep Apple passwords, App Store Connect private keys, provisioning profiles, and signing certificates outside the repository.
