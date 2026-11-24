# ZODIAC SIX Launcher

Offizieller Open-Source Windows Desktop Launcher für **ZODIAC SIX Roleplay** ([zodiac.duran.li](https://zodiac.duran.li)).

## Features
- **Single-EXE Installer & Uninstaller**: Schnelle, saubere Installation unter `%LocalAppData%\ZodiacSixLauncher` inkl. Desktop-/Startmenü-Verknüpfungen und rückstandsfreier Deinstallation über Windows „Installierte Apps“.
- **Automatischer Updater**: Prüft beim Start auf neue Releases via `https://zodiac.duran.li/launcher/version.json` inkl. SHA-256-Integritätsprüfung.
- **Live Windows-Systemcheck**: Prüft lokal auf Grand Theft Auto V, FiveM, TeamSpeak 3, SaltyChat-Plugin und Discord.
- **FiveM-Cache-Verwaltung**: Ermittelt die reale FiveM-Cache-Größe und leert die Cache-Ordner sicher (ohne `game-storage` zu berühren).
- **Persistente Einstellungen**: Windows-Autostart und Launcher-Optionen werden sauber lokal gespeichert.

## Voraussetzungen & Build
- Windows 10 / 11 (64-Bit)
- .NET 11 SDK (`net11.0-windows`)

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

- **Committers & Reviewers**: Projekt-Maintainer des Repositories [`xBeiros/launcherzs6`](https://github.com/xBeiros/launcherzs6).
- **Approvers**: Projekt-Owner (`xBeiros`).
- **Build & Signing**: Alle signierten Releases werden ausschließlich reproduzierbar über GitHub Actions aus diesem öffentlichen Repository gebaut.

## Datenschutz / Privacy Policy
Der ZODIAC SIX Launcher sendet **keine** Telemetrie-, Tracking- oder personenbezogenen Daten an Dritte. Es wird lediglich beim Start eine lesende HTTPS-Anfrage an `https://zodiac.duran.li/launcher/version.json` zur Update-Prüfung ausgeführt. Lokale Einstellungen werden ausschließlich lokal unter `%AppData%\ZodiacSixLauncher\settings.json` gespeichert.

## Lizenz
Dieses Projekt steht unter der [MIT-Lizenz](LICENSE).
