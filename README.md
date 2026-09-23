# MHW ReShade Injector Helper 6.8.0

这是基于 [keegars/MHW---Reshade-Injector-Helper](https://github.com/keegars/MHW---Reshade-Injector-Helper) 的兼容性更新版，用于《Monster Hunter: World》15.10.00 后无法从游戏目录直接加载 DXGI/D3D12 代理 DLL 的情况。

## 新版工作流程

1. 助手先自动寻找 `MonsterHunterWorld.exe`。
2. 如果游戏目录没有 `reshade-shaders\Shaders`，助手会启动随包附带的官方 `ReShade_Setup_6.8.0.exe`。
3. 官方安装器负责把 shader、texture、preset 和图形 API 代理安装到游戏目录。
4. 助手修正游戏目录中 `ReShade.ini` 的 shader/texture 绝对路径。
5. `inject.exe` 使用助手旁边附带的官方 `ReShade64.dll` 6.8.0 注入游戏，然后通过 Steam 启动游戏。
6. 检测到游戏进程并留出 5 秒显示注入结果后，助手窗口自动隐藏；游戏退出时助手自动完成清理并退出，不会再结束游戏进程。

如果需要观察完整控制台，可给助手增加 `--no-hide` 参数。

## DX12 与替换版 DLSS DLL 的崩溃保护

如果助手检测到已确认会在本机崩溃的 DLSS `1.3.2.0`，并且游戏使用 DX12、游戏设置中的 `NVIDIA DLSS` 明确为 `Off`，会在本次运行期间把游戏目录中的 `nvngx_dlss.dll` 临时改名，退出助手时自动恢复。这样不会覆盖或删除玩家替换的文件。原版 `1.1.13` 和已验证能够启动的 `1.2.14` 均不会执行该处理，以便玩家在游戏内启用 DLSS。

- DLSS 为 `On` 时，助手绝不会移动该 DLL。
- 助手若被强制结束，下次运行会识别 `.reshade-helper-disabled` 临时文件并继续保护或恢复。
- 若要使用 DLSS，请先确认所换 DLL 与《怪物猎人：世界》的旧版 NGX/DLSS 接口兼容；NVIDIA 只明确保证替换 DLL 对 DLSS 2.x SDK 的兼容性，并不覆盖这款游戏的 DLSS 1.x 集成。

## 中文界面显示为问号

官方安装器生成的旧配置可能把主界面字体固定为 `ProggyClean.ttf`。该字体没有中文字形，而 ReShade 6.8 会根据 Windows 语言自动显示中文，因此界面文字会变成问号。助手会把这一旧的主界面字体设置恢复为自动选择，让 ReShade 使用系统的微软雅黑或微软正黑体；shader 名称和代码编辑器字体不受影响。

## 自动恢复上次使用的预设

ReShade 会把最后选择的预设写入游戏目录的 `ReShade.ini`，通常是类似 `PresetPath=.\My Preset.ini` 的相对路径。助手会以游戏目录为基准解析该路径并原样保留选择，不再在每次启动时重置为空的 `ReShadePreset.ini`。预设文件名包含空格或撇号也受支持。

发行包不再内置标准 shader；所有 shader/texture 都由 ReShade 官方安装器下载并安装。`ReShade64.dll` 必须继续与 `inject.exe` 放在一起，因为官方安装器在游戏目录中使用的是 `dxgi.dll` 等代理名称，而注入器需要明确的 `ReShade64.dll`。

## 自动寻找游戏的位置

首次启动会按以下顺序扫描：

- 当前用户与本机注册表中的 Steam 安装目录。
- Steam 的 `libraryfolders.vdf` 中记录的全部库目录。
- 各固定磁盘根目录下常见的 `Steam`、`SteamLibrary`、`Games\Steam`、`Games\SteamLibrary`。
- 对应库中的 `appmanifest_582010.acf` 和标准游戏目录。

扫描全部失败后才会弹出文件选择框。检测到游戏后会在控制台显示完整路径。

## 取消选择后可以再次尝试

配置现在只会在找到有效游戏 EXE 后保存。第一次手动选择时即使点击取消，也不会再留下损坏的空 INI。旧版本已经留下的空配置、字段残缺配置或游戏路径失效配置，也会在下次启动时自动重新进入扫描/选择流程。

## 使用方法

1. 解压完整压缩包，不要只复制 EXE。
2. 双击 `MHW - Reshade Injector Helper.exe` 并接受管理员权限提示。
3. 等待自动扫描；扫描失败时再手动选择 `MonsterHunterWorld.exe`。
4. 如果官方 ReShade 安装器自动出现：选择游戏使用的 DirectX 10/11/12，并至少选择一个 shader 包，完成后关闭安装器。
5. 助手检测到游戏目录中的 `.fx` 文件后，会继续启动注入器和 Steam 游戏。
6. 进入游戏打开 ReShade；效果会从游戏目录的 `reshade-shaders` 文件夹加载。

## 从 Steam 的“开始游戏”自动运行助手

无需制作游戏 MOD。ReShade 需要在 DX12 图形设备创建前注入，因此使用 Steam 启动参数比游戏加载后的 MOD 更可靠：

1. 把本工具解压到一个不会再移动的固定目录，并先正常运行一次完成配置。
2. 双击 `Show Steam Launch Option.cmd`，复制窗口显示的完整启动参数。
3. 在 Steam 库中右键《Monster Hunter: World》→“属性”→“启动选项”，粘贴该参数。
4. 以后直接点击 Steam 的“开始游戏”即可；Steam 会先运行助手，助手准备注入后再启动真正的游戏。

若 Steam 客户端或游戏更新后出现启动循环，请先清空该启动选项，再直接运行助手。Steam 模式也会在游戏启动后自动隐藏窗口。

## 16:10、超宽屏与旧比例补丁

原项目包含一个通过修改游戏进程内存强制分辨率和 HUD 比例的旧补丁。它不属于 ReShade，仅支持 DX11，并且不支持 DX12/DLSS。新版默认完全关闭该功能，避免在正常注入后显示误导性错误或改写过期内存地址。

确实需要在 DX11 下实验时，可添加 `--enable-aspect-patch` 参数；使用风险由玩家自行承担。对于 DX12 + DLSS，建议使用游戏官方支持的 2560×1440 或 3840×2160 全屏分辨率。

不要删除或移动：

- `ReShade64.dll`
- `ReShade_Setup_6.8.0.exe`
- `inject.exe`
- EXE 旁边的依赖 DLL

## 从旧版迁移

建议将新版解压到独立文件夹。旧 preset 可以继续留在游戏目录。若旧助手记录了错误路径，无需手动清理；新版会检测路径失效并重新扫描。也可以删除助手 EXE 同名的 `.ini`，强制重新初始化。

## 仅准备环境，不启动游戏

```text
"MHW - Reshade Injector Helper.exe" --prepare-only "D:\SteamLibrary\steamapps\common\Monster Hunter World\MonsterHunterWorld.exe"
```

该模式仍会在缺少 shader 时启动官方安装器，但准备完成后不会启动游戏。

## 来源

- 原项目：[keegars/MHW---Reshade-Injector-Helper](https://github.com/keegars/MHW---Reshade-Injector-Helper)
- ReShade 6.8.0：[crosire/reshade v6.8.0](https://github.com/crosire/reshade/releases/tag/v6.8.0)
- 官方 shader 索引：[crosire/reshade-shaders](https://github.com/crosire/reshade-shaders)

ReShade、安装器和 shader 仍由各自原作者按其许可证发布。


## 自动跟随 ReShade 更新

GitHub Actions 每天检查 ReShade 官方稳定版标签。发现新版本后，会从 reshade.me 下载官方安装器，验证安装器和 ReShade64.dll 的版本与数字签名，编译 x64 Release，执行包完整性检查，生成 SHA-256 并创建 reshade-vX.Y.Z Release。源码仓库不提交 ReShade 安装器或 DLL，也不内置 shader 包。

## 许可证说明

本 fork 新增的修改以 MIT 许可证发布。上游仓库目前没有声明许可证，所以 MIT 不会重授权上游文件；详情见 NOTICE.md。ReShade 使用 BSD 3-Clause，完整文本在 LICENSES/ReShade-BSD-3-Clause.txt。
