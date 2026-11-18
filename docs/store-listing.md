# Store listing and certification draft

Replace publisher-specific support and privacy URLs before submitting. Do not use third-party logos.

## Display name

ThingsPilot

## Short description

Search and launch your existing SmartThings manually run routines from PowerToys Command Palette.

## Description

ThingsPilot integrates with the Windows Command Palette to search and launch the manually run routines you have already created in Samsung SmartThings. Find a routine by name, see its location, and invoke its Play button without navigating the SmartThings interface yourself.

Requirements: Windows 11, PowerToys with Command Palette enabled, and Samsung SmartThings for Windows installed and signed in. No SmartThings developer account, API subscription, or access token is required by ThingsPilot.

Search runs locally. Routine and location names are cached on your PC, with no analytics or ThingsPilot cloud service. Refresh reads the current location in SmartThings; switch locations in SmartThings and refresh when needed. The SmartThings window may appear briefly during discovery or execution.

ThingsPilot uses Windows UI Automation. Compatibility depends on the installed SmartThings interface and may require updates when Samsung changes that app. Execution feedback confirms that Play was invoked, not that every action or device finished.

Includes a clearly labeled demo with simulated routines. Creating or editing routines, automatic routines, and individual device controls are outside this version's scope.

ThingsPilot is MIT licensed and is an independent community project, not affiliated with or endorsed by Samsung or Microsoft.

## Notes for certification

This product is a PowerToys Command Palette extension. It does not have a standalone application window. Install current PowerToys, enable Command Palette, and open it with Win+Alt+Space. Reload extensions in Command Palette settings if needed, then select ThingsPilot.

For review without Samsung credentials: select **Try demo routines**. The page is labeled **ThingsPilot demo — no devices controlled**. It includes Good Night in Demo Home and Demo Cabin, plus Study in Demo Home. Search and select a demo routine; feedback explicitly states it is simulated. This mode never opens SmartThings or invokes a real Play button.

For the live integration: install Samsung SmartThings for Windows from Microsoft Store and sign in to a reviewer-owned account containing a harmless manually run routine. In ThingsPilot select **Refresh from SmartThings**, search for its name, and explicitly run it. Do not use a routine controlling locks, alarms, or hazardous equipment as a review fixture. Account sign-in remains entirely inside Samsung's app.

The `runFullTrust` capability is needed for the out-of-process Command Palette COM server and normal desktop Windows UI Automation client. ThingsPilot does not request `uiAccess`, administrator privileges, or network capability, and does not read Samsung tokens or passwords.

The demo verifies search and feedback only; it does not establish live SmartThings compatibility. Review notes must include the app version and results from the final signed release tests. Complete the privacy/support URLs, declarations, ratings, screenshots, and Partner Center identity before submission.
