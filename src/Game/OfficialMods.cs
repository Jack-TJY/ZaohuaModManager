using System.Collections.Generic;
using System.Linq;

namespace ZaohuaModManager.Game
{
    internal class ConflictRow
    {
        /// <summary>先加载、被覆盖的 mod。</summary>
        public string Loser;

        /// <summary>后加载、生效的 mod。</summary>
        public string Winner;

        /// <summary>表名 → 冲突的主键。</summary>
        public List<(string table, List<string> keys)> Tables = new List<(string, List<string>)>();
    }

    /// <summary>
    /// 官方数据 mod（MOD 目录和已订阅的创意工坊 mod），读写游戏自己的 BsSaveDataImpl.ModStoList / mod.sav，
    /// 与游戏自带面板「我的 mod」页是同一份数据。列表越靠上优先级越高（GetModLoadingOrder 从下往上加载，后加载的覆盖先加载的）。
    /// </summary>
    internal static class OfficialMods
    {
        /// <summary>本次打开窗口后改过启用或顺序，关窗时要重载配置表。</summary>
        public static bool Dirty;

        public static List<TbModSto> All
        {
            get
            {
                if (BsSaveDataImpl.ModStoList == null)
                {
                    Singleton<BsSaveDataImpl>.Instance.ToReadModList();
                }
                return BsSaveDataImpl.ModStoList ?? new List<TbModSto>();
            }
        }

        public static List<TbModSto> Visible => All.Where(s => !s.isHide && s.data != null).ToList();

        /// <summary>重新扫描 MOD 目录和订阅（只在主菜单做，游戏自带面板也是这样）。</summary>
        public static void Rescan()
        {
            if (OfficialPanel.AtMainMenu)
            {
                MonoSingleton<SteamWorkImpl>.Instance.CheckModList();
            }
        }

        public static void SetEnabled(TbModSto sto, bool enable)
        {
            if (sto.isEnable == enable)
            {
                return;
            }
            sto.isEnable = enable;
            Singleton<BsSaveDataImpl>.Instance.ToSaveModList();
            Dirty = true;
        }

        /// <summary>和游戏自带面板一样，与相邻的可见 mod 交换位置。</summary>
        public static void Move(TbModSto sto, bool up)
        {
            var visible = Visible;
            int i = visible.IndexOf(sto);
            int j = up ? i - 1 : i + 1;
            if (i < 0 || j < 0 || j >= visible.Count)
            {
                return;
            }
            MonoSingleton<SteamWorkImpl>.Instance.ChangeModPriority(sto, visible[j]);
            Dirty = true;
        }

        /// <summary>改动生效：重扫并重载全部配置表，与关闭游戏自带面板时一样。</summary>
        public static void ApplyIfDirty()
        {
            if (!Dirty || !OfficialPanel.AtMainMenu)
            {
                return;
            }
            Dirty = false;
            MonoSingleton<SteamWorkImpl>.Instance.CheckModList();
            Singleton<TbDataImpl>.Instance.LoadConfig();
            Plugin.Log.LogInfo("官方 mod 已改动，配置表已重载");
        }

        public static string NameOf(string id) =>
            All.FirstOrDefault(s => s.Id == id)?.data?.name ?? id;

        /// <summary>调用游戏自带（但没用上）的冲突检测，按当前启用和顺序算谁覆盖谁。</summary>
        public static List<ConflictRow> CheckConflicts()
        {
            var order = MonoSingleton<SteamWorkImpl>.Instance.GetModLoadingOrder(All);
            ConfigLoader.CheckModConflicts(Singleton<TbDataImpl>.Instance, order);
            var rows = new List<ConflictRow>();
            foreach (var s in ConfigLoader.ConflictSummaries)
            {
                var row = new ConflictRow { Loser = NameOf(s.ModA), Winner = NameOf(s.ModB) };
                foreach (var d in s.Details)
                {
                    row.Tables.Add((d.TypeName, d.Keys.Select(k => k?.ToString()).ToList()));
                }
                rows.Add(row);
            }
            return rows;
        }
    }
}
