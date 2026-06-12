# gmsb-codec-flac

FLAC playback codec plugin for
[Game Master Sound Board](https://github.com/DevinSanders/game-master-soundboard).

Adds `.flac` support via [BunLabs.NAudio.Flac](https://www.nuget.org/packages/BunLabs.NAudio.Flac).
Pure-managed — no native libFLAC binaries needed.

## Install

**Paid plugin.** The source is open here for reference, but the pre-built
binary is distributed pay-what-you-want on itch.io:

**→ https://dsand64.itch.io/gmsb-codec-flac**

Download the `.zip` from that page and drop it onto **Settings → Plugin
Manager** in Game Master Sound Board. Restart when prompted, then enable it under **Settings → Plugins**.

## Build

Requires .NET 10 SDK. `SoundBoard.PluginApi` is restored from NuGet
automatically — no sibling checkout needed.

```powershell
dotnet build src/FlacCodecPlugin.csproj
pwsh scripts/package.ps1
```

## Manifest

| Field     | Value                       |
|-----------|-----------------------------|
| publisher | `github.DevinSanders`       |
| id        | `codec.flac`                |
| entryDll  | `FlacCodecPlugin.dll`       |

## License

Released under the [MIT License](LICENSE).

Third-party components used by this plugin:

- BunLabs.NAudio.Flac (MS-PL) for pure-managed FLAC decoding.