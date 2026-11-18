# Architecture and limitations

Official sources were checked on 2026-11-17.

## SmartThings terminology and support boundary

Samsung's [Windows app documentation](https://www.samsung.com/levant/support/apps-services/use-smartthings-on-your-windows-10-and-11-pc/) says the app can activate existing Scenes and cannot access or control Automations. In the current app, its Routines page labels these groups **Manually run routines** and **Automatic routines**. ThingsPilot reads only the first group.

SmartThings' [API quick start](https://developer.smartthings.com/docs/getting-started/quickstart) states that routines created in the mobile app are not accessible as API Rules. Its current [API plan](https://developer.smartthings.com/smartthingsapi) also charges for API Access Apps. ThingsPilot does not use the SmartThings API, CLI, Rules API, Scenes API, OAuth, or personal access tokens.

## Local bridge

Microsoft documents [Windows UI Automation](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-uiautomationoverview) as the Windows accessibility and automated-interaction framework. ThingsPilot uses its managed client API to:

1. Find or launch the packaged SmartThings Windows app.
2. Select the navigation element with automation ID `RoutinesNavItem`.
3. read the current `LocationNameTextBlock` and children of `ScenesGridView`;
4. ignore the separate `RulesGridView` containing automatic routines; and
5. find the selected manual routine's `ActionButton`, verify that SmartThings is foreground and the Play button has keyboard focus, then send one Enter key press with Windows SendInput.

In this Samsung app version, the button's UI Automation InvokePattern focuses the button without executing the routine. A live Study test confirmed that Enter on the focused Play button executes it. ThingsPilot therefore uses keyboard activation instead of InvokePattern, never both. It allows a short input-dispatch grace period before re-minimizing the window; this does not measure routine completion. Packaged-app launch uses Windows IApplicationActivationManager. Minimized UWP identity is checked through native child HWNDs because those descendants may be absent from the accessibility tree.

Names and locations are cached locally for fast Command Palette search. A SHA-256 digest of location plus name provides a stable local command ID; it is an identifier, not a security primitive. UI interaction is serialized, and duplicate execution is suppressed while an invocation is in progress. The process's application model ID must match Samsung's packaged application before ThingsPilot interacts with it; a window title alone is insufficient. Discovery uses the ItemContainer pattern when available to include virtualized entries. Routine names must be unique within a location; ambiguous names fail without invoking anything.

The bridge restores a minimized SmartThings window temporarily because its accessibility descendants are not available while minimized. If ThingsPilot launched the app, or found it minimized, it minimizes it when finished. It preserves an already visible window.

## Practical limits

- Samsung does not document these automation IDs as a compatibility contract. App updates can break discovery or invocation.
- A successful UI Automation invocation means Play was invoked. It does not prove the routine finished or every device succeeded.
- ThingsPilot cannot distinguish two manual routines with identical names in the same location; it rejects such a list until they are renamed.
- ThingsPilot currently reads the location selected in SmartThings. Switch location and refresh to replace the cache.
- The SmartThings app may appear briefly. Login prompts, first-run screens, dialogs, localization changes, or unavailable routines can require interaction in SmartThings.
- ThingsPilot runs with the user's normal desktop permissions. Windows UI Automation does not cross into another user or an elevated desktop.
- English SmartThings `1.26391.0.0` is the verified baseline. Other languages and app versions need compatibility checks.

The demo page has a separate in-memory bridge and catalog. Its results are never saved to the real cache and its execution method performs no Windows automation. Merely loading the provider does not launch SmartThings.

## Command Palette packaging

ThingsPilot follows Microsoft's [out-of-process Command Palette extension model](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/extensibility-overview). The MSIX registers one WinRT COM server and `com.microsoft.commandpalette`, uses the official SDK, includes x64 and ARM64 self-contained builds, and declares `runFullTrust`. It declares no network capability.
