# Verification

Verified on 2026-09-22:

- Release/x64 solution rebuild completed with zero reported errors.
- Bundled `ReShade64.dll` reports product version `6.8.0` and file version `6.8.0.2158`.
- Bundled DLL SHA-256 is `B2945C29E7095491A901746B400E58DB9B1592AB092BACF2A888CE37F02D08DA`.
- The official `ReShade_Setup_6.8.0.exe` is copied into the release output.
- No `reshade-shaders` directory is embedded in the helper release.
- A simulated Steam library with app manifest 582010 was located automatically.
- The simulated game-local shader was detected without opening the installer.
- Generated `ReShade.ini`, preset, and search paths were all placed in the simulated game directory, not the helper directory.
- Invalid or empty settings are rejected before use; settings are saved only after executable selection succeeds.
- A fixture with `Font=ProggyClean.ttf` was repaired to automatic main-font selection while preserving the editor font.
- The DLSS compatibility session was verified to restore the original file after temporary isolation.
- Live game logs confirmed ReShade 6.8.0 injection, the RTX 5070 Ti Laptop GPU, and game-local shader paths.
- A Windows crash report identified swapped `nvngx_dlss.dll` 1.3.2.0 as the faulting module; reverting it allowed the game to start.
- Release 1.3.0 compiles with the auto-hide/background lifecycle and Steam wrapper mode.
- The legacy aspect-ratio patch is opt-in and reports a skip, not an error, under DX12/DLSS.
- A relative preset containing spaces and an apostrophe was resolved against the game directory and preserved.

The updated 1.3.1 executable still requires an in-game smoke test for auto-hide, Steam wrapper launch mode, and preset restoration.
