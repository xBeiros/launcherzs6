# ZODIAC SIX Launcher

Official open-source Windows desktop launcher for **ZODIAC SIX Roleplay** ([zodiac.duran.li](https://zodiac.duran.li)).

## Overview & Features
- **Single-EXE Installer & Uninstaller**: Installs cleanly to `%LocalAppData%\ZodiacSixLauncher` with optional Desktop/Start Menu shortcuts and full uninstallation via Windows "Installed Apps" (`--uninstall`).
- **Automatic Updater**: Checks `https://zodiac.duran.li/launcher/version.json` on startup, verifies SHA-256 checksums, and updates the launcher atomically.
- **Live System Check**: Verifies local installation status of Grand Theft Auto V, FiveM, TeamSpeak 3, SaltyChat plugin, and Discord.
- **FiveM Cache Manager**: Calculates real FiveM cache size (`cache`, `server-cache`, `server-cache-priv`, `nui-storage`) and clears cache folders safely without touching `game-storage`.
- **Persistent Settings**: Manages Windows autostart (`HKCU\...\Run`) and local user preferences.

## Build from Source
Requirements: Windows 10/11 (x64), .NET 11 SDK (`net11.0-windows`).

```powershell
dotnet publish WpfApp1\WpfApp1.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:IncludeAllContentForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true
```

## Code Signing Policy
Free code signing provided by [SignPath.io](https://signpath.io/), certificate by [SignPath Foundation](https://signpath.org/).

- **Committers & Reviewers**: Kazim Duran ([`@xBeiros`](https://github.com/xBeiros))
- **Approvers**: Kazim Duran ([`@xBeiros`](https://github.com/xBeiros))
- **Build & Signing Process**: Signed binaries are built exclusively via reproducible GitHub Actions workflows from this public repository ([`.github/workflows/release.yml`](.github/workflows/release.yml)).
- **Scope**: Only release binaries of `ZodiacSix-Launcher.exe` produced directly from tagged releases in this repository are submitted for signing.

## Privacy Policy
ZODIAC SIX Launcher does **not** collect, store, or transmit any personal data, telemetry, or analytics to third parties.

- On startup, the application performs a read-only HTTPS GET request to `https://zodiac.duran.li/launcher/version.json` solely to check for launcher updates.
- User preferences (such as minimize-on-launch and last cache-clear timestamp) are stored strictly locally in `%AppData%\ZodiacSixLauncher\settings.json`.
- No user data is transferred to SignPath or any external tracking service.

## License
This project is licensed under the [MIT License](LICENSE).
