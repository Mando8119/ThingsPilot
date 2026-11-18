# Release preparation

Nothing in this repository publishes automatically. Package identity, publisher, Store product ID, WinGet identifier, and Gallery author remain configurable.

## Unsigned GitHub preview

The first public release uses unpacked, self-contained developer-registration ZIPs rather than asking users to install unsigned MSIX files or trust a self-signed certificate. [Preview.ps1](../tools/Preview.ps1) builds both architectures, takes the exact staging paths recorded by the packaging script, adds the guided installers and getting-started text, and writes ZIPs plus `PREVIEW-SHA256SUMS.txt`. It refuses to overwrite an existing ZIP.

```powershell
./tools/Validate.ps1
./tools/Preview.ps1 -SkipBuild
```

Extract each ZIP to a fresh directory and run its installer with `-CheckOnly` to check payload completeness. On a test Windows account, run Install.cmd, verify Command Palette discovery and demo execution, then test update and uninstall. Publish the two ZIPs and preview checksums as a GitHub prerelease with the [release notes](release-notes-0.1.3.md). Explicitly state that Developer Mode is required and ARM64 has not been hardware-tested. Do not label it a signed or Store-certified release.

The installer copies the application into a per-user version directory and registers its manifest. It does not import certificates, modify Developer Mode or machine execution policy, require an SDK, contact a download server, or run a routine. The batch launcher selects an execution-policy override only for its PowerShell child process. Users can inspect all installer source in this repository before running it.

## GitHub and WinGet

GitHub can host the source and signed release packages. Public MSIX packages must be signed with a trusted code-signing identity whose certificate subject exactly matches the manifest publisher. Timestamp and verify signatures, then test clean installation, update, and removal. Do not ask public users to disable signature verification.

Run the packaging script for both architectures:

```powershell
./tools/Package.ps1 -Architecture x64 `
  -IdentityName 'YOUR_PACKAGE_IDENTITY' `
  -Publisher 'CN=YOUR_EXACT_PUBLISHER' `
  -PublisherDisplayName 'YOUR_PUBLISHER_NAME' -Version '0.1.3.0'
./tools/Package.ps1 -Architecture arm64 `
  -IdentityName 'YOUR_PACKAGE_IDENTITY' `
  -Publisher 'CN=YOUR_EXACT_PUBLISHER' `
  -PublisherDisplayName 'YOUR_PUBLISHER_NAME' -Version '0.1.3.0'
```

Complete the templates under `packaging/winget` with stable HTTPS release URLs, signed-package hashes, the package family name, and real identifiers. Run `winget validate` and test the resulting installation. Keep the `windows-commandpalette-extension` tag.

For an architecture bundle and SHA-256 checksums, run [Bundle.ps1](../tools/Bundle.ps1) with the same identity parameters. It builds both packages and produces `ThingsPilot-0.1.3.0.msixbundle` and `SHA256SUMS.txt`. To sign through an existing certificate with its private key in the current user's certificate store, pass `-CertificateThumbprint YOUR_THUMBPRINT`; the script timestamps and verifies the signatures. It does not generate or install certificates. Store uploads can use an unsigned bundle with the Partner Center identity; public sideloading requires trusted signing.

## Microsoft Store

Microsoft provides [specific Store instructions for Command Palette extensions](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/publish-extension-store). Reserve the product in Partner Center, replace the development identity with the exact assigned identity, upload an MSIX bundle, describe the PowerToys dependency, and include certification notes.

Store acceptance is not guaranteed. Current [Store policy](https://learn.microsoft.com/en-us/windows/apps/publish/store-policies) requires products using a third-party service to be permitted under that service's terms and requires the product to be testable. Before submission:

- review Samsung's current app terms for third-party desktop automation;
- use the implemented **Try demo routines** mode and supply the [certification notes and listing draft](store-listing.md); a real account is still needed to verify live SmartThings compatibility;
- explain that ThingsPilot uses the documented Windows accessibility API and does not access Samsung credentials or APIs;
- run WACK and clean-machine tests; and
- provide support and privacy URLs.

Use original ThingsPilot artwork and state that the project is independent. Do not use Samsung or Microsoft logos or imply endorsement.

## Command Palette Extensions Gallery

The Gallery is a curated directory and does not host packages. Microsoft's current [submission guide](https://github.com/microsoft/CmdPal-Extensions/blob/main/docs/CONTRIBUTING.md) requires the extension to be published first through WinGet or the Microsoft Store.

Create `extensions/<author>/thingspilot/` from `packaging/gallery`, replace the author, URLs, extension ID, and install source, then set `listed` to `true`. The ID must match the lowercase folder path. Submit the folder to `microsoft/CmdPal-Extensions`, complete the CLA, pass schema checks, and allow maintainer review.

## Release gate

Before any public submission, verify the current SmartThings app on clean Windows 11 x64 and ARM64 systems, login and first-run states, multiple display scales, localization behavior, package updates, privacy text, and recovery after a SmartThings UI change. Recheck Microsoft and Samsung policies because distribution approval is a policy decision, not something the code can guarantee.
