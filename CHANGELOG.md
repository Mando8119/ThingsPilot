# Changelog

## 0.1.2 — first public preview

- Search and run existing SmartThings manually run routines from PowerToys Command Palette.
- Local cache, routine-name and location search, explicit refresh, and a separate safe demo mode.
- Open or restore SmartThings automatically and activate the selected Play button with one Enter press. Samsung's accessibility InvokePattern alone only focused the button.
- Guard keyboard activation with verified app identity, foreground window, button focus, and released modifier keys. No automatic routine-execution retries.
- Explain that activation does not confirm device or routine completion.
- Self-contained x64 and ARM64 preview ZIPs with guided per-user installation, updates, and removal. Requires Windows Developer Mode; no SDK or certificate import.
- MIT licensed; no ThingPilot server, account, telemetry, or SmartThings developer API subscription.

Known limits: the current location in SmartThings must be selected before refreshing, the app can appear briefly, and Samsung UI changes can require compatibility updates. Windows x64 with English SmartThings 1.26391.0.0 is the verified baseline; ARM64 is cross-built, not hardware-tested.
