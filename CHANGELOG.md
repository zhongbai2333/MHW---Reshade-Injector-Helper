# Changelog

## 1.3.1

- 修复相对 `PresetPath` 被错误地按助手目录解析，导致每次启动重置预设。
- 现在优先保留 ReShade 最后选择且仍然存在的预设。
- 停止覆盖 ReShade 6.8 不再使用的旧 `CurrentPresetPath`。

## 1.3.0

- 游戏启动并显示注入结果后自动隐藏助手窗口，游戏退出时助手自动退出。
- 不再等待回车，也不再在助手结束时强制关闭游戏。
- 修复旧宽屏补丁在 DX12/DLSS 下显示错误后仍提示“Patch applied”的问题。
- 旧宽屏内存补丁改为默认关闭，仅在显式指定 `--enable-aspect-patch` 时运行。
- 增加 `--from-steam` 模式和启动参数生成脚本，可从 Steam“开始游戏”先运行助手。
- 修复定时存档备份的计时累计和取消响应。

## 1.2.1

- 修复 ReShade 6.8 中文界面因 `ProggyClean.ttf` 缺少中文字形而显示为问号。
- 仅对已确认崩溃的 DLSS 1.3.2.0 应用临时隔离；原版 1.1.13 和可正常启动的 1.2.14 不受影响。
- 错误日志写入失败时不再掩盖原始错误。

## 1.2.0 - Automatic discovery and official setup workflow

- Added automatic discovery through the Steam registry, `libraryfolders.vdf`, app manifest 582010, and common library paths on fixed drives.
- The file picker is now used only when every automatic candidate fails.
- Settings are saved only after a valid executable is selected, so cancelling first-time setup no longer leaves a broken INI.
- Empty, incomplete, moved, or otherwise invalid settings automatically return to discovery on the next run.
- Removed the bundled standard shader package.
- Added the official ReShade 6.8.0 installer; it opens automatically when the game has no local shader files.
- ReShade search paths now point exclusively to the game's `reshade-shaders` folders.
- The active `ReShade.ini` and fallback preset are now maintained in the game directory, matching injected ReShade's official base-path behavior.
- Kept the official `ReShade64.dll` 6.8.0 next to `inject.exe`, as required by the injector.

## 1.1.0 - ReShade 6.8.0

- Updated the bundled 64-bit ReShade DLL from 6.4.1 to 6.8.0.
- Replaced hard-coded Steam install paths with paths derived from the selected game executable.
- Added game-local and helper-local recursive shader discovery.
- Bundled the official standard ReShade shader package.
- Added shader validation and startup diagnostics.
- Added preset discovery and a `--prepare-only` repair mode.
- Made resource lookup independent of the process working directory.
- Retargeted the launcher from .NET Framework 4.8.1 to 4.8.
