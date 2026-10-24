using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using Mono.Cecil;
using ZaohuaModManager.Shared;

namespace ZaohuaModManager.Game
{
    internal class PluginEntry
    {
        /// <summary>插件目录下的相对路径，不带 .disabled。</summary>
        public string RelPath;

        public string Name;

        public string Version;

        public string Guid;

        /// <summary>本次启动是否已加载。</summary>
        public bool Loaded;

        /// <summary>文件现在是不是 .dll（不是 .dll.disabled）。</summary>
        public bool OnDisk;

        /// <summary>下次启动是否加载（文件状态 + 待办）。</summary>
        public bool WillLoad;

        public bool IsSelf;

        public string Status =>
            Loaded == WillLoad ? (Loaded ? "已加载" : "已停用")
            : WillLoad ? "重启游戏后启用" : "重启游戏后停用";
    }

    /// <summary>BepInEx\plugins 下的插件：列出、启用/停用（改名，重启生效）。</summary>
    internal static class PluginList
    {
        static string PendingFile => Path.Combine(Paths.ConfigPath, "zaohua.modmanager.pending.txt");

        public static string PluginDir => Paths.PluginPath;

        public static List<PluginEntry> Scan()
        {
            var pending = PendingOps.Read(PendingFile);
            var loaded = Chainloader.PluginInfos.Values
                .Where(p => !string.IsNullOrEmpty(p.Location))
                .GroupBy(p => Path.GetFullPath(p.Location), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            string self = Path.GetFullPath(typeof(PluginList).Assembly.Location);
            var list = new List<PluginEntry>();
            foreach (var file in Directory.GetFiles(PluginDir, "*", SearchOption.AllDirectories))
            {
                bool isDll = file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);
                bool isDisabled = file.EndsWith(".dll" + PendingOps.DisabledSuffix, StringComparison.OrdinalIgnoreCase);
                if (!isDll && !isDisabled)
                {
                    continue;
                }
                string dll = isDll ? file : file.Substring(0, file.Length - PendingOps.DisabledSuffix.Length);
                string rel = dll.Substring(PluginDir.Length).TrimStart('\\', '/');
                var e = new PluginEntry { RelPath = rel, OnDisk = isDll };
                e.WillLoad = pending.TryGetValue(rel, out bool want) ? want : isDll;
                e.IsSelf = string.Equals(Path.GetFullPath(dll), self, StringComparison.OrdinalIgnoreCase);
                if (loaded.TryGetValue(Path.GetFullPath(dll), out var infos))
                {
                    e.Loaded = true;
                    e.Name = string.Join(" / ", infos.Select(i => i.Metadata.Name));
                    e.Version = string.Join(" / ", infos.Select(i => i.Metadata.Version.ToString()));
                    e.Guid = string.Join(" / ", infos.Select(i => i.Metadata.GUID));
                }
                else
                {
                    ReadMetadata(file, e);
                }
                if (e.Name == null)
                {
                    // 不是 BepInEx 插件（多半是插件带的依赖库），不列出
                    continue;
                }
                list.Add(e);
            }
            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCulture));
            return list;
        }

        /// <summary>没加载的插件（停用了或加载失败）从 dll 里读 [BepInPlugin] 拿名字和版本。</summary>
        static void ReadMetadata(string file, PluginEntry e)
        {
            try
            {
                using (var asm = AssemblyDefinition.ReadAssembly(file))
                {
                    var attrs = asm.MainModule.Types
                        .SelectMany(t => t.CustomAttributes)
                        .Where(a => a.AttributeType.FullName == typeof(BepInPlugin).FullName && a.ConstructorArguments.Count >= 3)
                        .ToList();
                    if (attrs.Count == 0)
                    {
                        return;
                    }
                    e.Guid = string.Join(" / ", attrs.Select(a => (string)a.ConstructorArguments[0].Value));
                    e.Name = string.Join(" / ", attrs.Select(a => (string)a.ConstructorArguments[1].Value));
                    e.Version = string.Join(" / ", attrs.Select(a => (string)a.ConstructorArguments[2].Value));
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"读取插件信息失败 {file}: {ex.Message}");
            }
        }

        /// <summary>改成 enable 状态。能立即改名就改，被占用就记待办，下次启动由预加载器改。</summary>
        public static void SetEnabled(PluginEntry e, bool enable)
        {
            var pending = PendingOps.Read(PendingFile);
            pending.Remove(e.RelPath);
            if (e.OnDisk != enable)
            {
                try
                {
                    PendingOps.Apply(PluginDir, e.RelPath, enable);
                    e.OnDisk = enable;
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    pending[e.RelPath] = enable;
                }
            }
            PendingOps.Write(PendingFile, pending);
            e.WillLoad = enable;
        }

        public static bool PreloaderInstalled =>
            File.Exists(Path.Combine(Paths.PatcherPluginPath, "ZaohuaModManager.Preloader.dll"));
    }
}
