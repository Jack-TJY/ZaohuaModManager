# MOD 管理器（ZaohuaModManager）

《造化仙缘》的 BepInEx 插件：在游戏里直接管理 MOD。

游戏其实自带了一个「创意工坊」面板（下载 / 我的 mod / 上传），能启用、停用和调整官方数据 mod 的加载顺序，但主菜单上的入口只对官方账号显示。本插件在主菜单左下角加了「MOD 管理」按钮把它接通。

> 当前进度：阶段 1（接通游戏自带面板）。BepInEx 插件管理、冲突检查、插件设置页在后续阶段加入。

## 安装

1. 游戏需已装好 BepInEx 5（x64）。
2. 把 `ZaohuaModManager.dll` 放进 `游戏目录\BepInEx\plugins\`。
3. 进游戏，主菜单左下角会出现「MOD 管理 (F10)」按钮。

## 使用

- 在主菜单点按钮或按 F10，打开「我的 mod」页：勾选启用、上下移动调整加载顺序（越靠下优先级越高）。
- 关闭面板时游戏会重新扫描 MOD 目录并重载配置，不用重启。
- 进了存档后不能修改官方 mod（存档中途换配置表可能坏档），请回主菜单再改。
- 按住按钮可以拖动位置。

配置文件 `BepInEx\config\zaohua.modmanager.cfg`：

| 项 | 默认 | 说明 |
| --- | --- | --- |
| OpenKey | F10 | 打开/关闭 MOD 管理 |
| ShowButton | true | 主菜单是否显示按钮 |
| ButtonX / ButtonY | -1 | 按钮位置，-1 = 左下角 |

## 构建

`lib/` 下放以下 dll（不随仓库分发）：游戏 `Zaohua_Data\Managed` 里的 `Assembly-CSharp`、`UnityEngine`、`UnityEngine.CoreModule`、`UnityEngine.IMGUIModule`、`UnityEngine.InputLegacyModule`、`UnityEngine.TextRenderingModule`、`UnityEngine.UI`，以及 `BepInEx\core` 里的 `BepInEx`、`0Harmony`。

```
dotnet build src -c Release
```

## 许可

MIT
