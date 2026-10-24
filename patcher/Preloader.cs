using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using Mono.Cecil;
using ZaohuaModManager.Shared;

namespace ZaohuaModManager.Preloader
{
    /// <summary>
    /// BepInEx 预加载器：在加载插件之前，把 MOD 管理器里改过但当时被占用的插件改名（启用/停用）。
    /// 不修改任何游戏程序集。
    /// </summary>
    public static class Preloader
    {
        public static IEnumerable<string> TargetDLLs { get; } = new string[0];

        public static void Patch(AssemblyDefinition assembly)
        {
        }

        public static void Initialize()
        {
            var log = Logger.CreateLogSource("MOD 管理器");
            string file = Path.Combine(Paths.ConfigPath, "zaohua.modmanager.pending.txt");
            try
            {
                var ops = PendingOps.Read(file);
                var left = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in ops)
                {
                    try
                    {
                        PendingOps.Apply(Paths.PluginPath, kv.Key, kv.Value);
                        log.LogInfo($"{(kv.Value ? "启用" : "停用")}插件 {kv.Key}");
                    }
                    catch (Exception e)
                    {
                        log.LogWarning($"改名失败 {kv.Key}: {e.Message}");
                        left[kv.Key] = kv.Value;
                    }
                }
                PendingOps.Write(file, left);
            }
            catch (Exception e)
            {
                log.LogError("读取待办失败: " + e);
            }
        }
    }
}
