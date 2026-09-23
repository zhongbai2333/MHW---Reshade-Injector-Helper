# MHW ReShade Injector Helper

[简体中文](README.zh-CN.md)

A Windows launcher for using current ReShade releases with *Monster Hunter: World*. This repository is maintained as a fork of [keegars/MHW---Reshade-Injector-Helper](https://github.com/keegars/MHW---Reshade-Injector-Helper).

## What it does

- Locates `MonsterHunterWorld.exe` from registered Steam libraries, with a file picker as fallback.
- Starts the official ReShade installer when the game has no shader effects installed.
- Loads shaders and textures from the game's own `reshade-shaders` directory.
- Repairs ReShade search paths and preserves the last valid preset.
- Starts the injector before launching the game through Steam.
- Hides the helper window after startup and exits cleanly when the game closes.
- Can be configured as the game's Steam launch command.

The portable release includes the verified official ReShade installer and `ReShade64.dll`. Shader packages are not bundled; select the packages you want in the official installer.

## Requirements

- Windows 10 or 11, 64-bit
- Steam version of *Monster Hunter: World*
- .NET Framework 4.8
- Administrator permission when requested

The legacy aspect-ratio memory patch is disabled by default. It is experimental, intended only for DX11, and is not compatible with DX12 or DLSS.

## Download

Download the latest portable archive from [GitHub Releases](https://github.com/zhongbai2333/MHW---Reshade-Injector-Helper/releases). Do not download GitHub's automatically generated source archive unless you intend to build the project yourself.

Each automated release also contains `SHA256SUMS.txt`.

## First run

1. Extract the entire portable archive to a permanent folder.
2. Run `MHW - Reshade Injector Helper.exe`.
3. The helper searches your Steam libraries for the game. If it cannot find the game, select `MonsterHunterWorld.exe` manually.
4. If the official ReShade installer opens, choose DirectX 10/11/12 and install at least one shader package.
5. Close the installer when it finishes. The helper verifies the shader installation, prepares injection, and launches the game through Steam.

Keep the helper executable, `inject.exe`, `ReShade64.dll`, the ReShade setup executable, and the dependency DLLs together in the extracted folder.

## Start automatically from Steam

1. Run the helper normally once so it can save a valid game path.
2. Run `Show Steam Launch Option.cmd`.
3. Copy the displayed command into **Steam → Monster Hunter: World → Properties → Launch Options**.
4. Launch the game from Steam as usual.

If Steam enters a launch loop after an update or after moving the helper folder, clear the custom launch option and run the helper directly again.

## Command-line options

| Option | Purpose |
| --- | --- |
| `--no-hide` | Keep the helper console visible while the game is running. |
| `--prepare-only "<game.exe>"` | Prepare ReShade without launching the game. |
| `--enable-aspect-patch` | Enable the legacy experimental DX11 aspect-ratio patch. |
| `--show-steam-option` | Print the Steam launch command. |

Example:

```text
"MHW - Reshade Injector Helper.exe" --prepare-only "D:\SteamLibrary\steamapps\common\Monster Hunter World\MonsterHunterWorld.exe"
```

## Troubleshooting

### ReShade opens but the effect list is empty

Run the helper again. When the official installer opens, install at least one shader package. Effects must exist under:

```text
<game directory>\reshade-shaders\Shaders
```

### The game path picker was cancelled

Run the helper again. A cancelled selection is not saved, so the automatic scan and picker will be available on the next launch.

### The game crashes after replacing the DLSS DLL

Restore the original or a known-compatible DLSS DLL before testing ReShade again. This game's DLSS 1.x integration is not guaranteed to work with DLLs intended for newer DLSS generations.

The helper contains a narrow safety workaround for the known-problematic `1.3.2.0` DLL when DX12 is enabled and in-game DLSS is explicitly disabled. It does not claim general compatibility for arbitrary replacement DLLs.

### A preset is not restored

Make sure the preset file still exists. Relative preset paths are resolved from the game directory, and the last valid preset is preserved.

### More details are needed

Check `ErrorLog.txt` next to the helper executable.

## Automated ReShade releases

The scheduled GitHub Actions workflow checks the official stable ReShade tags every day. When a new stable version is found, it:

1. Downloads the matching installer from `reshade.me`.
2. Verifies the installer version and ReShade Authenticode signer.
3. Extracts and verifies `ReShade64.dll`.
4. Builds the helper for Release/x64.
5. Checks required files and confirms that no shader package is bundled.
6. Publishes a versioned portable archive and SHA-256 checksum.

ReShade installers and DLLs are fetched only during release builds and are not committed to the source repository.

## Building from source

Open `MHW - Reshade Injector Helper.sln` in Visual Studio with the .NET Framework 4.8 developer tools installed. Restore the NuGet packages and build the `Release|x64` configuration.

A local build also needs matching official `ReShade_Setup_X.Y.Z.exe` and `ReShade64.dll` files in the project directory. The automated workflow prepares these files for release builds.

## Credits and licenses

- Original project: [keegars/MHW---Reshade-Injector-Helper](https://github.com/keegars/MHW---Reshade-Injector-Helper)
- ReShade: [crosire/reshade](https://github.com/crosire/reshade)
- Official shader repository: [crosire/reshade-shaders](https://github.com/crosire/reshade-shaders)

Changes first added by this fork are available under the MIT License. The upstream repository currently does not include a license grant, so the MIT License does not relicense upstream material. ReShade is distributed under BSD 3-Clause. See [NOTICE.md](NOTICE.md) and the [included license texts](LICENSES) for details.
