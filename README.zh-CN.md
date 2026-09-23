# MHW ReShade 注入助手

[English](README.md)

这是一个面向 Windows 的《Monster Hunter: World》ReShade 启动助手，基于 [keegars/MHW---Reshade-Injector-Helper](https://github.com/keegars/MHW---Reshade-Injector-Helper) 维护。

## 主要功能

- 从 Steam 注册信息和库目录自动寻找 `MonsterHunterWorld.exe`，扫描失败后再让玩家手动选择。
- 游戏目录缺少 shader 时启动官方 ReShade 安装器。
- 从游戏目录的 `reshade-shaders` 文件夹加载 shader 和 texture。
- 修正 ReShade 搜索路径，并保留上次有效的预设。
- 先启动注入器，再通过 Steam 启动游戏。
- 游戏成功启动后自动隐藏窗口，游戏退出后自动清理并关闭。
- 支持配置为 Steam 启动选项。

portable 发行包包含经过 CI 验证的官方 ReShade 安装器和 `ReShade64.dll`，但不内置 shader 包。需要哪些 shader，请在官方安装器中自行选择。

## 运行要求

- 64 位 Windows 10 或 Windows 11
- Steam 版《Monster Hunter: World》
- .NET Framework 4.8
- 在系统提示时授予管理员权限

旧版画面比例内存补丁默认关闭。该功能只适合实验性 DX11 使用，不支持 DX12 或 DLSS。

## 下载

请从 [GitHub Releases](https://github.com/zhongbai2333/MHW---Reshade-Injector-Helper/releases) 下载最新的 portable 压缩包。除非准备自行编译，否则不要下载 GitHub 自动生成的 Source code 压缩包。

自动发布的版本同时提供 `SHA256SUMS.txt`。

## 第一次使用

1. 将 portable 压缩包完整解压到一个固定目录。
2. 运行 `MHW - Reshade Injector Helper.exe`。
3. 程序会扫描 Steam 库。找不到游戏时，再手动选择 `MonsterHunterWorld.exe`。
4. 如果官方 ReShade 安装器弹出，请选择 DirectX 10/11/12，并至少安装一个 shader 包。
5. 安装完成后关闭安装器。助手会检查 shader、准备注入并通过 Steam 启动游戏。

请将助手 EXE、`inject.exe`、`ReShade64.dll`、ReShade 安装器和依赖 DLL 保持在同一个解压目录中。

## 通过 Steam 自动启动

1. 先直接运行一次助手，保存有效的游戏路径。
2. 运行 `Show Steam Launch Option.cmd`。
3. 将显示的命令复制到 **Steam → 怪物猎人：世界 → 属性 → 启动选项**。
4. 以后正常从 Steam 启动游戏即可。

如果 Steam 更新后出现启动循环，或者助手目录已经移动，请先清空自定义启动选项，再直接运行一次助手。

## 命令行参数

| 参数 | 作用 |
| --- | --- |
| `--no-hide` | 游戏运行期间不隐藏助手窗口。 |
| `--prepare-only "<game.exe>"` | 只准备 ReShade，不启动游戏。 |
| `--enable-aspect-patch` | 启用旧版实验性 DX11 画面比例补丁。 |
| `--show-steam-option` | 显示 Steam 启动命令。 |

示例：

```text
"MHW - Reshade Injector Helper.exe" --prepare-only "D:\SteamLibrary\steamapps\common\Monster Hunter World\MonsterHunterWorld.exe"
```

## 常见问题

### ReShade 能打开，但效果列表为空

重新运行助手，并在官方安装器中至少选择一个 shader 包。效果文件必须位于：

```text
<游戏目录>\reshade-shaders\Shaders
```

### 第一次选择游戏时点了取消

直接重新运行即可。取消选择不会保存无效配置，下次仍会重新扫描并允许手动选择。

### 中文界面全部显示为问号

旧配置可能将 ReShade 主界面字体固定为不包含中文字形的 `ProggyClean.ttf`。助手会清理这一旧设置，让 ReShade 重新选择系统中支持中文的字体。shader 名称和代码编辑器字体不会因此改变。

### 替换 DLSS DLL 后游戏崩溃

先恢复游戏原版或已经确认兼容的 DLSS DLL，再测试 ReShade。该游戏使用较旧的 DLSS 1.x 集成，面向新一代 DLSS 的替换 DLL 不一定兼容。

当游戏使用 DX12、游戏内 DLSS 明确关闭，并检测到已知有问题的 `1.3.2.0` DLL 时，助手会进行一次针对性的临时保护。这个处理不代表其他替换版本一定兼容。

### 没有恢复上次的预设

确认预设文件仍然存在。相对路径会以游戏目录为基准解析，仍然有效的上次预设会被保留。

### 需要查看详细错误

检查助手目录中的 `ErrorLog.txt`。

## ReShade 自动更新发行

GitHub Actions 每天检查 ReShade 官方稳定版标签。发现新版本后会自动：

1. 从 `reshade.me` 下载对应的官方安装器。
2. 验证安装器版本和 ReShade 数字签名。
3. 提取并验证 `ReShade64.dll`。
4. 编译 Release/x64。
5. 检查发行包必需文件，并确认没有捆绑 shader。
6. 发布版本化 portable 压缩包和 SHA-256 校验文件。

ReShade 安装器和 DLL 只在发行构建时下载，不提交到源码仓库。

## 从源码编译

使用安装了 .NET Framework 4.8 开发工具的 Visual Studio 打开 `MHW - Reshade Injector Helper.sln`，恢复 NuGet 包，然后编译 `Release|x64`。

本地编译还需要把版本匹配的官方 `ReShade_Setup_X.Y.Z.exe` 和 `ReShade64.dll` 放入项目目录。自动发布工作流会在 CI 中完成这一步。

## 来源与许可证

- 原项目：[keegars/MHW---Reshade-Injector-Helper](https://github.com/keegars/MHW---Reshade-Injector-Helper)
- ReShade：[crosire/reshade](https://github.com/crosire/reshade)
- 官方 shader 仓库：[crosire/reshade-shaders](https://github.com/crosire/reshade-shaders)

本 fork 首次新增的修改采用 MIT 许可证。由于上游仓库目前没有许可证声明，MIT 不会重新授权上游内容。ReShade 使用 BSD 3-Clause。详情请查看 [NOTICE.md](NOTICE.md) 和 [许可证目录](LICENSES)。
