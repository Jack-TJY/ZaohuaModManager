# MOD 管理器（ZaohuaModManager）

《造化仙缘》的 BepInEx 插件：在游戏里直接管理 MOD。

- **官方 MOD**：启用/停用、调整加载顺序、查看简介和前置、打开文件夹。读写的是游戏自己的 `mod.sav`，与游戏自带的「我的 mod」页同一份数据。
- **BepInEx 插件**：列出全部插件和加载状态，启用/停用（重启游戏生效）。
- **插件设置**：直接在游戏里改各插件的 BepInEx 配置（开关、数字、下拉选项、快捷键），改完立即保存到 cfg。
- **冲突检查**：调用游戏自带的检测，列出哪两个 mod 改了同一张表的同一条数据、谁覆盖谁。
- 还能打开游戏自带但没有入口的「创意工坊」面板（订阅 / 上传）。

> 当前进度：阶段 3，功能已齐。

## 安装

1. 游戏需已装好 BepInEx 5（x64）。
2. 把 `ZaohuaModManager.dll` 放进 `游戏目录\BepInEx\plugins\`，`ZaohuaModManager.Preloader.dll` 放进 `游戏目录\BepInEx\patchers\`。
3. 进游戏，主菜单左下角会出现「MOD 管理 (F10)」按钮。

## 使用

- 点主菜单按钮或按 F10（存档里也能按）打开窗口。
- 官方 MOD：越靠上优先级越高。关闭窗口时自动重载配置表，不用重启。进了存档后只能查看（存档中途换配置表可能坏档），请回主菜单再改。
- 插件：停用 = 把 `.dll` 改名为 `.dll.disabled`。BepInEx 只在启动时加载插件，所以要重启游戏才生效。正在运行的插件文件被占用时，改名会记到 `BepInEx\config\zaohua.modmanager.pending.txt`，下次启动时由预加载器在加载插件之前完成。
- 插件设置：数字和文字填完点「确定」或回车；快捷键点一下再按想要的键（可带 Ctrl/Shift/Alt，Esc 取消）；「默认」恢复默认值。有的设置马上生效，有的要重启游戏，看插件自己的说明。
- 按住主菜单按钮可以拖动位置。

配置文件 `BepInEx\config\zaohua.modmanager.cfg`：

| 项 | 默认 | 说明 |
| --- | --- | --- |
| OpenKey | F10 | 打开/关闭 MOD 管理 |
| ShowButton | true | 主菜单是否显示按钮 |
| ButtonX / ButtonY | -1 | 按钮位置，-1 = 左下角 |

## 构建

`lib/` 下放以下 dll（不随仓库分发）：游戏 `Zaohua_Data\Managed` 里的 `Assembly-CSharp`、`UnityEngine`、`UnityEngine.CoreModule`、`UnityEngine.IMGUIModule`、`UnityEngine.InputLegacyModule`、`UnityEngine.TextRenderingModule`、`UnityEngine.UI`，以及 `BepInEx\core` 里的 `BepInEx`、`0Harmony`、`Mono.Cecil`。

```
dotnet build src -c Release
dotnet build patcher -c Release
```

## 许可

MIT
