#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
UNITY_BIN="${UNITY_BIN:-/Applications/Unity/Hub/Editor/6000.5.1f1/Unity.app/Contents/MacOS/Unity}"
BUILD_ROOT="${BUILD_ROOT:-$PROJECT_ROOT/Builds/iOS}"
XCODE_DIR="${BUILD_OUTPUT_PATH:-$BUILD_ROOT/Symbiosis-iOS}"
ARCHIVE_PATH="${ARCHIVE_PATH:-$BUILD_ROOT/Symbiosis.xcarchive}"
IPA_DIR="${IPA_DIR:-$BUILD_ROOT/ipa}"
EXPORT_OPTIONS_PLIST="$BUILD_ROOT/ExportOptions-AppStore.generated.plist"
LOG_FILE="${LOG_FILE:-$BUILD_ROOT/unity-ios-export.log}"

require_env() {
  local name="$1"
  if [[ -z "${!name:-}" ]]; then
    echo "Missing required environment variable: $name" >&2
    exit 2
  fi
}

export APPLE_DEVELOPER_TEAM_ID="${APPLE_DEVELOPER_TEAM_ID:-32VM68DZD8}"
require_env APPLE_DEVELOPER_TEAM_ID
require_env ADMOB_IOS_APP_ID

mkdir -p "$BUILD_ROOT" "$IPA_DIR"

export BUILD_OUTPUT_PATH="$XCODE_DIR"
export IOS_AUTOMATIC_SIGNING="${IOS_AUTOMATIC_SIGNING:-true}"
export IOS_BUNDLE_ID="${IOS_BUNDLE_ID:-com.ozkullar.dlsymbiosis}"
export IOS_DISPLAY_NAME="${IOS_DISPLAY_NAME:-Symbiosis}"
export IOS_MIN_TARGET="${IOS_MIN_TARGET:-15.0}"
export REQUIRE_IOS_ADMOB_ID="${REQUIRE_IOS_ADMOB_ID:-true}"

if [[ ! -x "$UNITY_BIN" ]]; then
  echo "Unity was not found or is not executable: $UNITY_BIN" >&2
  exit 2
fi

"$UNITY_BIN" \
  -batchmode \
  -quit \
  -projectPath "$PROJECT_ROOT" \
  -executeMethod IosCiBuild.BuildXcodeProject \
  -logFile "$LOG_FILE"

if [[ -f "$XCODE_DIR/Podfile" ]]; then
  (cd "$XCODE_DIR" && pod install)
fi

cat > "$EXPORT_OPTIONS_PLIST" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>method</key>
  <string>app-store-connect</string>
  <key>teamID</key>
  <string>${APPLE_DEVELOPER_TEAM_ID}</string>
  <key>signingStyle</key>
  <string>automatic</string>
  <key>stripSwiftSymbols</key>
  <true/>
  <key>uploadSymbols</key>
  <true/>
  <key>compileBitcode</key>
  <false/>
</dict>
</plist>
PLIST

if [[ -d "$XCODE_DIR/Unity-iPhone.xcworkspace" ]]; then
  XCODE_CONTAINER=(-workspace "$XCODE_DIR/Unity-iPhone.xcworkspace")
else
  XCODE_CONTAINER=(-project "$XCODE_DIR/Unity-iPhone.xcodeproj")
fi

xcodebuild \
  "${XCODE_CONTAINER[@]}" \
  -scheme Unity-iPhone \
  -configuration Release \
  -sdk iphoneos \
  -destination 'generic/platform=iOS' \
  -archivePath "$ARCHIVE_PATH" \
  -allowProvisioningUpdates \
  DEVELOPMENT_TEAM="$APPLE_DEVELOPER_TEAM_ID" \
  CODE_SIGN_STYLE=Automatic \
  archive

xcodebuild \
  -exportArchive \
  -archivePath "$ARCHIVE_PATH" \
  -exportPath "$IPA_DIR" \
  -exportOptionsPlist "$EXPORT_OPTIONS_PLIST"

IPA_PATH="$(find "$IPA_DIR" -maxdepth 1 -name '*.ipa' -print -quit)"
if [[ -z "$IPA_PATH" ]]; then
  echo "IPA export failed: no .ipa found in $IPA_DIR" >&2
  exit 1
fi

echo "IPA ready: $IPA_PATH"

if [[ "${UPLOAD_TO_APP_STORE_CONNECT:-false}" == "true" ]]; then
  require_env APPLE_ID
  require_env APP_SPECIFIC_PASSWORD
  xcrun altool --validate-app -f "$IPA_PATH" -t ios -u "$APPLE_ID" -p "$APP_SPECIFIC_PASSWORD"
  xcrun altool --upload-app -f "$IPA_PATH" -t ios -u "$APPLE_ID" -p "$APP_SPECIFIC_PASSWORD"
  echo "Upload requested. Wait for processing in App Store Connect -> TestFlight."
fi
