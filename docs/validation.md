# Local validation

Validated on Windows x64 on 2026-11-17 against SmartThings for Windows `1.26391.0.0`. With user authorization, Study was activated with one Enter key press on the focused Play button, and the user confirmed the routine took effect. Nothing was published.

## Completed checks

- The live SmartThings accessibility tree exposed `RoutinesNavItem`, `LocationNameTextBlock`, `ScenesGridView`, semantic names for all three visible manually run routines, and an `ActionButton` named Play for each.
- The reusable UI probe discovered three manual routines in the current location while the app began minimized, then returned it to the minimized state without triggering anything.
- The separate automatic-routine list was exposed as `RulesGridView` and is not read by ThingPilot.
- Ten tests cover search ranking, stable local identity, cache replacement and validation, Play-invocation wording, stale-routine removal and persistence, failed discovery, demo-only execution, and duplicate-trigger suppression.
- The extension and core build cleanly in Release x64.
- The out-of-process COM smoke host checks provider activation, the local Refresh/Open commands, and the separate demo page with sample routines.
- Regression checks resolve the ThingPilot page by command ID, validate the Refresh action label, invoke a demo routine through COM, and verify that its asynchronous simulated result reaches the host's status callback.
- The default read-only probe validates the real Play lookup and keyboard eligibility for all three routines without activating the controls. An explicit `--run "Study"` mode is available for authorized live tests; the validation script never uses it.
- Packaging produces self-contained x64 and ARM64 MSIX packages and a combined MSIX bundle with MakeAppx validation and SHA-256 checksums.
- The Gallery template passed the current official JSON schema; its original icon is below 100 KB.
- Both public preview ZIPs were extracted and their payload identities and installer contents checked. x64 per-user installation was tested, including same-version registration repair and verifying that Windows points to the persistent LocalAppData copy. The ARM64 installer correctly rejects this x64 test PC. Hosted CI was unavailable for the initial public preview; local validation results are not a claim of a successful GitHub Actions run.
- The development package registered successfully with Windows (status Ok) and was discovered by the installed Command Palette. Its page rendered real manual routine names with location context. Live testing exposed that Samsung's InvokePattern only focused Play; verified keyboard activation replaced it. App preparation now handles minimized UWP windows and waits for the Routines navigation to be ready before reading controls.

## Commands

```powershell
./tools/Validate.ps1 -ProbeSmartThings
```

Or run individual checks:

```powershell
dotnet test tests/ThingPilot.Tests -c Release
dotnet run --project tests/ThingPilot.UiProbe -c Release
./tools/Package.ps1 -Architecture x64
./tools/Package.ps1 -Architecture arm64
```

Then run the COM smoke host against the latest x64 staging output as shown in the README.

## Manual release matrix

Before a public release, use harmless manual routines in an owned account:

1. Verify first launch, signed-out state, login, current location, no routines, and renamed or removed routines.
2. Confirm search never invokes a routine. Explicitly run one harmless routine and independently verify its effects in SmartThings.
3. Confirm the message says Play was invoked and never claims completion.
4. Test SmartThings already visible, minimized, closed, updating, and showing a modal dialog.
5. Test multiple locations by switching in SmartThings and refreshing; do not claim automatic location switching until it has been implemented and validated.
6. Test signed install, update, uninstall, WACK, x64, ARM64 hardware, screen readers, keyboard navigation, scaling, and supported languages.
7. Repeat the accessibility probe after every SmartThings update. Treat missing automation IDs as a compatibility failure rather than attempting coordinate clicks.

Store certification, signed installation, ARM64 execution, multiple locations, non-English SmartThings builds, and live routine execution remain release checks.
