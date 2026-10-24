using System;

namespace ZaohuaModManager.Game
{
    /// <summary>
    /// 游戏自带的创意工坊面板 WorkShopPanel（下载 / 我的 mod / 上传）。
    /// 主菜单上它的按钮 btnWorkShop 只对官方账号显示，这里直接调 UIMgr 打开。
    /// 面板关闭时游戏自己会重新扫描 MOD 目录并重载配置表（WorkShopPanel.HideMe）。
    /// </summary>
    internal static class OfficialPanel
    {
        public const int MyModPage = 1;

        /// <summary>在主菜单、没进存档时才能改官方 mod，存档中途换表可能坏档。</summary>
        public static bool AtMainMenu
        {
            get
            {
                var ui = MonoSingleton<UIMgr>.Instance;
                return ui != null && ui.IsShowPanel<StartPanel>() && string.IsNullOrEmpty(BsSaveDataImpl.curModId);
            }
        }

        public static bool IsOpen => MonoSingleton<UIMgr>.Instance?.IsShowPanel<WorkShopPanel>() ?? false;

        public static void Open()
        {
            // 回调在 ShowMe 之前执行，先把页签改成「我的 mod」，否则联网时默认打开「下载」页
            MonoSingleton<UIMgr>.Instance.ShowPanel<WorkShopPanel>(UI_Layer.Top, p => p.panelIndex = MyModPage);
        }

        public static void Close()
        {
            MonoSingleton<UIMgr>.Instance.HidePanel<WorkShopPanel>();
        }

        public static string Toggle()
        {
            try
            {
                if (IsOpen)
                {
                    Close();
                    return null;
                }
                if (!AtMainMenu)
                {
                    return "请回到主菜单再管理 MOD（存档中途换配置表可能坏档）";
                }
                Open();
                return IsOpen ? null : "打开 MOD 面板失败，详见 BepInEx\\LogOutput.log";
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
                return "打开 MOD 面板失败：" + e.Message;
            }
        }
    }
}
