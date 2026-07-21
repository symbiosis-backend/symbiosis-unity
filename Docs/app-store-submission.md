# Symbiosis App Store Submission Checklist

Date: 2026-05-16

## Apple Account

- Apple Developer Program: wait for identity verification approval.
- App Store Connect: should open only after membership is active.
- Team ID: copy from <https://developer.apple.com/account> and export as `APPLE_DEVELOPER_TEAM_ID` on the Mac.

## App Store Connect App

- Platform: iOS
- Name: Symbiosis
- Bundle ID: `com.ozkullar.dlsymbiosis`
- SKU: `symbiosis-ios-001`
- Privacy Policy URL: `https://dlsymbiosis.com/privacy`
- Support URL: `https://dlsymbiosis.com/`
- Account deletion URL: `https://dlsymbiosis.com/account-deletion`

## Unity Release Settings

- Product name: Symbiosis
- Company: Ozkullar
- Bundle ID: `com.ozkullar.dlsymbiosis`
- Version: `1.0.15`
- iOS build number: `100016`
- Minimum iOS: `15.0`
- Signing: automatic, Team selected in Xcode or passed via `APPLE_DEVELOPER_TEAM_ID`
- Google Mobile Ads iOS App ID: create in AdMob before final release and export as `ADMOB_IOS_APP_ID`

## Privacy Labels Draft

Use this as the first App Store Connect privacy questionnaire pass. Adjust if SDKs or features change.

- Contact Info: email address for account/login/support.
- User Content: chat messages, nicknames, friend/social content.
- Identifiers: user ID/player ID, device/session identifiers, advertising ID through ads SDK.
- Purchases: product IDs and transaction state.
- Usage Data: gameplay progress, matches, rewards, ad interactions.
- Diagnostics: crash/error logs and server request logs.
- Data is used for app functionality, account management, analytics/diagnostics, advertising, fraud prevention, and support.
- Data is linked to the user when tied to account/profile/player ID.
- The game includes user-generated chat; moderation/report/block flows must stay available.

## TestFlight Build

On the Mac:

```bash
chmod +x Tools/iOS/build-ios-testflight.sh

export APPLE_DEVELOPER_TEAM_ID="YOUR_TEAM_ID"
export IOS_BUNDLE_ID="com.ozkullar.dlsymbiosis"
export ADMOB_IOS_APP_ID="ca-app-pub-XXXXXXXXXXXXXXXX~YYYYYYYYYY"

Tools/iOS/build-ios-testflight.sh
```

Then upload the `.ipa` through Xcode Organizer, Transporter, or the script's optional upload mode.

## Review Notes Draft

```text
Symbiosis is an online mahjong/battle puzzle game with account progress, matchmaking, chat, friends, rewarded ads, and optional purchases. TestFlight build 100016 corresponds to version 1.0.15.

If a reviewer needs a test account, create one through the in-app registration flow or contact support@dlsymbiosis.com.
```
