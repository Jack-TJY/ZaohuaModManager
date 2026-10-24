using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;

namespace ZaohuaModManager
{
    /// <summary>哪些设置改完要重启游戏才生效。</summary>
    internal static class RestartInfo
    {
        /// <summary>
        /// 已核实只在启动时读取的设置（GUID, 分组, 键）。2026-10-24 逐个看过本项目插件的代码：
        /// - 炼丹炼器加速 TimeDivisor：启动时缩放默认耗时表，丹方耗时补丁才实时读，所以要重启才完全生效
        /// - 自动炼丹炼器、小世界加速、归墟珍宝阁灵石、MOD 管理器：都是用时才读，改了立即生效
        /// </summary>
        static readonly HashSet<(string, string, string)> Known = new HashSet<(string, string, string)>
        {
            ("zaohua.fastcrafting", "General", "TimeDivisor"),
        };

        /// <summary>说明里写了这些字的也当作要重启（其他插件可以用这个约定标出来）。</summary>
        static readonly string[] Hints = { "需重启", "需要重启", "重启游戏后生效", "重启后生效", "requires restart", "restart required" };

        public static bool NeedsRestart(PluginInfo plugin, ConfigEntryBase e)
        {
            if (Known.Contains((plugin.Metadata.GUID, e.Definition.Section, e.Definition.Key)))
            {
                return true;
            }
            string desc = e.Description?.Description;
            if (string.IsNullOrEmpty(desc))
            {
                return false;
            }
            foreach (var h in Hints)
            {
                if (desc.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
