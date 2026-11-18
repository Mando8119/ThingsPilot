# Privacy

ThingPilot runs locally. It does not contain telemetry, advertising, analytics, an account system, a server component, or SmartThings API credentials. It makes no network requests.

The extension reads routine names and the selected location name from the signed-in SmartThings Windows app through Windows UI Automation. It stores the most recently discovered list in `%LOCALAPPDATA%/ThingPilot/routines.json` for fast search. The file contains names, location names, and deterministic local identifiers; it contains no Samsung password, token, device state, or routine actions.

Choosing a result asks the SmartThings app to invoke that manual routine's Play button. SmartThings itself handles account access and device communication under its own terms and privacy policy. Removing ThingPilot does not sign out of SmartThings. Delete `%LOCALAPPDATA%/ThingPilot` to remove ThingPilot's cache.
