# ThingsPilot v0.1.3 — free community preview

Search and run your existing Samsung SmartThings **manually run routines** from PowerToys Command Palette. No paid SmartThings developer API, token, hosted service, Visual Studio, or .NET SDK required.

This update corrects the name to **ThingsPilot** everywhere. Run the included installer to replace the earlier preview registration and preserve cached routines, then reload Command Palette extensions.

## Download and install

1. Download **ThingsPilot-0.1.3.0-x64.zip** for most PCs, or **ThingsPilot-0.1.3.0-arm64.zip** for ARM PCs. Do not download the source-code archive to install.
2. Extract the entire ZIP and double-click **Install.cmd**.
3. If asked, enable **Windows Developer Mode**, then run **Install.cmd** again.
4. Open Command Palette (**Win+Alt+Space**), choose **ThingsPilot → Open routines → Refresh from SmartThings**.
5. Search for your manual routine by name and press **Enter once**.

Requires Windows 11, Microsoft PowerToys with Command Palette enabled, and Samsung SmartThings for Windows installed and signed in. If ThingsPilot is missing, reload extensions in Command Palette Settings → Extensions → Installed → More options.

**Try demo routines** works without a Samsung account and never controls devices.

## Preview installation details

These ZIPs contain the complete runtime and a guided per-user installer. This is an unsigned developer-mode release, not a Store-signed package. No certificate import or administrator terminal is needed. Install.cmd runs its included PowerShell script using a process-only execution-policy override; it does not change your system policy or disable Windows package signature checks. Only run the installer if you trust this release. Managed devices may block developer registration.

After installation you may delete the extracted folder. Run Install.cmd from a newer release to update; run Uninstall.cmd to remove. Nothing changes your existing SmartThings routines or account.

## What works and limitations

- Fast local search with routine names and current location context.
- Opens/restores SmartThings and activates its exact Play button once. The app may appear briefly.
- No automatic execution retry. Feedback confirms activation was sent, not that every device finished.
- Reads the location currently selected in SmartThings. Switch locations there and refresh ThingsPilot when needed.
- Manual routines only; no creation, editing, automatic-routine execution, or device controls.
- Samsung UI changes can require a compatibility update.

Validated with ten tests, out-of-process COM activation and demo feedback, read-only live routine discovery, and a live Study activation confirmed by the user. x64 and ARM64 packages passed MakeAppx validation. ARM64 is cross-built and still needs testing on actual ARM hardware. The verified SmartThings baseline is English Windows app 1.26391.0.0.

The initial release assets were built and validated locally. GitHub-hosted CI was unavailable for this release and did not execute the workflow.

Use PREVIEW-SHA256SUMS.txt to verify download hashes. Source is MIT licensed. Independent community project, not affiliated with Samsung or Microsoft.
