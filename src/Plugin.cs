using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ZaohuaModManager
{
    // 游戏内管理 MOD：主菜单「MOD 管理」按钮 / F10 打开窗口，管官方数据 mod（启用、顺序、冲突）和 BepInEx 插件（启用/停用、改设置）。
    [BepInPlugin("zaohua.modmanager", "MOD 管理器", "0.4.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        internal static ConfigEntry<KeyboardShortcut> OpenKey;

        internal static ConfigEntry<bool> ShowButton;

        internal static ConfigEntry<int> ButtonX, ButtonY;

        internal static ConfigEntry<bool> ShowRestartTip;

        private void Awake()
        {
            Log = Logger;
            // F5/F8 是游戏的快速存读档，F9 是自动炼丹炼器
            OpenKey = Config.Bind("通用", "OpenKey", new KeyboardShortcut(KeyCode.F10), "打开/关闭 MOD 管理（不要设成 F5/F8/F9）");
            ShowButton = Config.Bind("通用", "ShowButton", true, "主菜单是否显示「MOD 管理」按钮；false 时只能用快捷键");
            ButtonX = Config.Bind("通用", "ButtonX", -1, "主菜单按钮的位置（像素，左上角为原点）；在游戏里按住按钮拖动会自动改这里，-1 = 默认位置");
            ButtonY = Config.Bind("通用", "ButtonY", -1, "同上，纵坐标");
            ShowRestartTip = Config.Bind("通用", "ShowRestartTip", true, "改了要重启游戏才生效的设置（或启用/停用插件）后是否弹窗提示；弹窗里勾「不再提示」会把这里改成 false");
            new Harmony("zaohua.modmanager").PatchAll();
            // BepInEx 自带的管理物体在这个游戏里会被销毁，导致插件的 Update/OnGUI 不跑，所以自己建一个常驻物体
            var go = new GameObject("ZaohuaModManager");
            go.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
            Log.LogInfo("MOD 管理器已加载，主菜单左下角有「MOD 管理」按钮，快捷键 " + OpenKey.Value);
        }
    }

    internal class Runner : MonoBehaviour
    {
        private readonly MainMenuButton button = new MainMenuButton();

        private void Update()
        {
            button.Update();
        }

        private void OnGUI()
        {
            button.OnGUI();
        }
    }
}
