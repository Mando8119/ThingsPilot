# ThingsPilot

ThingsPilot is a Windows PowerToys Command Palette extension for searching and running existing Samsung SmartThings **Manually run routines**. Type **good night**, choose **Good Night**, and ThingsPilot invokes its Play button in the signed-in SmartThings Windows app.

ThingsPilot is an independent community project. It is not affiliated with or endorsed by Samsung or Microsoft.

## Install the free preview

**[Download ThingsPilot v0.1.3](https://github.com/Mando8119/ThingsPilot/releases/tag/v0.1.3)** — no .NET SDK or Visual Studio required.

1. Install [Microsoft PowerToys](https://aka.ms/powertoys) with **Command Palette** enabled, and **Samsung SmartThings** from Microsoft Store. Sign in to SmartThings.
2. Download the **x64 ZIP** for most PCs, or **ARM64 ZIP** for an ARM-based PC such as a Snapdragon Surface. Choose the ThingsPilot ZIP under **Assets**, not GitHub's source-code ZIP.
3. Extract the entire ZIP, then double-click **Install.cmd**. If it opens Windows Settings, enable **Developer Mode**, then run **Install.cmd** again.
4. Press **Win+Alt+Space**, search **ThingsPilot**, choose **Open routines**, then **Refresh from SmartThings**.
5. Search for a manual routine such as **good night** and press **Enter once** to run it.

If ThingsPilot does not appear, use **Command Palette Settings → Extensions → Installed → More options → Reload extensions**. **Try demo routines** previews search and feedback without controlling anything.

This first release is an **unsigned developer-mode preview**. The installer requires no administrator terminal, does not import certificates, and does not change your system's execution policy. Its launcher uses a process-only PowerShell policy override to run the included installer; review it and only install releases you trust. Managed work PCs may prohibit developer registration. A signed Store/WinGet release is a future step.

The installer copies ThingsPilot to your Windows user profile, so you can delete the extracted ZIP folder afterward. To update, run **Install.cmd** from a newer release. To uninstall, run **Uninstall.cmd** from a release ZIP or remove ThingsPilot in Windows Settings. Your SmartThings account and routines are unaffected.

Version 0.1.3 corrects the product name to **ThingsPilot**. Its installer replaces the earlier preview registration and copies your cached routines automatically. Reload Command Palette extensions after updating.

## Why the Windows app?

SmartThings calls manually run routines **Scenes** in some developer and Windows documentation. The paid SmartThings API exposes those scenes, but mobile-created automatic routines are separate. ThingsPilot avoids the paid API entirely: it uses Microsoft's documented Windows UI Automation client API to read the SmartThings app's own **Manually run routines** list and invoke its semantic **Play** control.

This design has no SmartThings API fees, OAuth service, developer account, access token, client secret, or cloud component. It was verified against SmartThings for Windows `1.26391.0.0` on 2026-11-17.

## Experience

- Routine names appear directly in Command Palette and in a dedicated searchable ThingsPilot page.
- Search is local, ranked, case-insensitive, and supports multiple words and the current location name.
- Results are cached in `%LOCALAPPDATA%/ThingsPilot/routines.json`, so ordinary searches do not open SmartThings.
- Refresh opens or restores SmartThings, selects **Routines**, reads only **Manually run routines**, and minimizes the app afterward if ThingsPilot launched it.
- Running a result finds that routine in the current SmartThings location and invokes Play exactly once.
- SmartThings can start closed or minimized. ThingsPilot launches/restores it, focuses the exact Play button and sends one Enter activation, then minimizes it again if it was previously hidden. You do not need to press Enter inside SmartThings.
- **Try demo routines** opens a separate, labeled sample catalog with simulated execution. It never opens SmartThings or controls devices.
- Feedback says **Play invoked**. This confirms the Windows control accepted the invocation; SmartThings does not provide ThingsPilot with per-device completion status.

The SmartThings Windows app must be installed, signed in, and able to show the routine. If you use multiple locations, switch locations in SmartThings and refresh ThingsPilot. The current implementation keeps the latest location's list because switching locations programmatically has not been validated. Automatic routines are deliberately ignored.

To get started, open **ThingsPilot → Refresh from SmartThings** once. Loading the extension and typing searches never launch SmartThings automatically. Refresh again after changing routines or locations. If two manual routines in one location share exactly the same name, rename them before using ThingsPilot.

If opening ThingsPilot appears to do nothing, reload extensions under **Command Palette Settings → Extensions → Installed → More options**, then select the ThingsPilot result whose action is **Open routines**. Builds before 0.1.1 also exposed a standalone Windows app entry that silently exited; this entry is now hidden, and direct executable launches display setup instructions.

UI Automation is a supported Windows API, but Samsung does not publish an automation contract for its app. A SmartThings update can rename or restructure controls and require a ThingsPilot compatibility update. The app may appear briefly when a refresh or trigger requires it.

## Build and verify

Requirements are Windows 11, PowerToys with Command Palette, the SmartThings Windows app, the .NET 10 SDK, and the Windows SDK with MakeAppx.

```powershell
./tools/Validate.ps1 -ProbeSmartThings
```

This runs the unit tests, optional live read-only accessibility probe, both MSIX package builds, the architecture bundle, and out-of-process COM validation. Omit `-ProbeSmartThings` on machines without SmartThings. The probe checks Play controls without invoking them. Individual commands are also available:

```powershell
dotnet test tests/ThingsPilot.Tests -c Release
./tools/Package.ps1 -Architecture x64
./tools/Package.ps1 -Architecture arm64
```

Unsigned packages are written under `artifacts/`. Nothing is installed or published automatically. For a local development deployment, enable Windows Developer Mode and register the latest unpacked x64 staging manifest:

```powershell
$stage = Get-ChildItem artifacts -Directory -Filter stage-x64-* |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
Add-AppxPackage -Register "$($stage.FullName)/AppxManifest.xml"
```

The equivalent helper is `./tools/Install-Dev.ps1`. It requires Developer Mode already enabled and registers only the development identity.

Reload extensions in Command Palette settings if needed. To remove the development registration:

```powershell
Get-AppxPackage ThingsPilot.Dev | Remove-AppxPackage
```

The COM smoke host checks the real out-of-process extension activation without running a routine:

```powershell
dotnet run --project tests/ThingsPilot.SmokeHost -c Release -- "$($stage.FullName)/ThingsPilot.exe"
```

See [architecture and limitations](docs/architecture-and-limitations.md), [local validation](docs/validation.md), [release preparation](docs/releasing.md), and [privacy](docs/privacy.md). ThingsPilot uses the [MIT license](LICENSE).
