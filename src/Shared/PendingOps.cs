using System;
using System.Collections.Generic;
using System.IO;

namespace ZaohuaModManager.Shared
{
    /// <summary>
    /// 插件的启用/停用 = 把 xxx.dll 改名为 xxx.dll.disabled（或反过来）。
    /// 游戏运行时已加载的 dll 被占用改不了名，就记到待办文件里，下次启动时由预加载器（patchers 里那个 dll）在 BepInEx 加载插件之前改名。
    /// 这个文件同时编进插件和预加载器。
    /// </summary>
    internal static class PendingOps
    {
        public const string DisabledSuffix = ".disabled";

        /// <summary>待办：插件目录下的相对路径（不带 .disabled） → 是否启用。</summary>
        public static Dictionary<string, bool> Read(string file)
        {
            var ops = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(file))
            {
                return ops;
            }
            foreach (var line in File.ReadAllLines(file))
            {
                int tab = line.IndexOf('\t');
                if (tab <= 0)
                {
                    continue;
                }
                ops[line.Substring(tab + 1).Trim()] = line.Substring(0, tab) == "enable";
            }
            return ops;
        }

        public static void Write(string file, Dictionary<string, bool> ops)
        {
            if (ops.Count == 0)
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
                return;
            }
            var lines = new List<string>();
            foreach (var kv in ops)
            {
                lines.Add((kv.Value ? "enable" : "disable") + "\t" + kv.Key);
            }
            File.WriteAllLines(file, lines.ToArray());
        }

        /// <summary>立即改名。dll 被占用时抛 IOException / UnauthorizedAccessException。</summary>
        public static void Apply(string pluginDir, string relPath, bool enable)
        {
            string dll = Path.Combine(pluginDir, relPath);
            string from = enable ? dll + DisabledSuffix : dll;
            string to = enable ? dll : dll + DisabledSuffix;
            if (!File.Exists(from))
            {
                return;
            }
            if (File.Exists(to))
            {
                File.Delete(to);
            }
            File.Move(from, to);
        }
    }
}
