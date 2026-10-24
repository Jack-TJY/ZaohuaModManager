using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using ZaohuaModManager.Game;

namespace ZaohuaModManager
{
    /// <summary>MOD 管理窗口：官方 mod / 插件 / 插件设置 / 冲突检查 四页。</summary>
    internal class ManagerWindow
    {
        const int WindowId = 0x5A4D4D;

        static readonly string[] Tabs = { "官方 MOD", "BepInEx 插件", "插件设置", "冲突检查" };

        public bool Visible { get; private set; }

        Rect rect;

        int tab;

        Vector2 scroll;

        string status;

        List<PluginEntry> plugins;

        List<ConflictRow> conflicts;

        string selectedMod;

        GUIStyle small, wrap, header, rich;

        readonly SettingsPage settings = new SettingsPage();

        public bool CapturingKey => Visible && settings.CapturingKey;

        public bool Contains(Vector2 guiPos) => Visible && rect.Contains(guiPos);

        public void Show()
        {
            Visible = true;
            if (rect.width == 0)
            {
                float w = Mathf.Min(780, Screen.width - 40), h = Mathf.Min(580, Screen.height - 40);
                rect = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);
            }
            status = null;
            conflicts = null;
            OfficialMods.Dirty = false;
            try
            {
                OfficialMods.Rescan();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
            RefreshPlugins();
            settings.Refresh();
        }

        public void Hide()
        {
            Visible = false;
            try
            {
                OfficialMods.ApplyIfDirty();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        void RefreshPlugins()
        {
            try
            {
                plugins = PluginList.Scan();
            }
            catch (Exception e)
            {
                plugins = new List<PluginEntry>();
                status = "读取插件列表失败：" + e.Message;
                Plugin.Log.LogError(e);
            }
        }

        public void OnGUI()
        {
            if (!Visible)
            {
                return;
            }
            if (small == null)
            {
                small = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
                small.normal.textColor = new Color(0.75f, 0.75f, 0.75f);
                wrap = new GUIStyle(GUI.skin.label) { wordWrap = true };
                header = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, richText = true };
                rich = new GUIStyle(GUI.skin.label) { richText = true };
                small.richText = wrap.richText = true;
            }
            rect = GUI.Window(WindowId, rect, Draw, "MOD 管理");
        }

        void Draw(int id)
        {
            GUILayout.BeginHorizontal();
            int newTab = GUILayout.Toolbar(tab, Tabs, GUILayout.Height(30));
            if (newTab != tab)
            {
                tab = newTab;
                scroll = Vector2.zero;
            }
            if (GUILayout.Button("关闭", GUILayout.Width(70), GUILayout.Height(30)))
            {
                Hide();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
            try
            {
                switch (tab)
                {
                    case 0:
                        DrawOfficial();
                        break;
                    case 1:
                        DrawPlugins();
                        break;
                    case 2:
                        settings.Draw(small, rich);
                        break;
                    case 3:
                        DrawConflicts();
                        break;
                }
            }
            catch (Exception e)
            {
                // IMGUI 布局里抛异常会把整个窗口画乱，这里吞掉并显示
                GUILayout.Label("出错：" + e.Message, wrap);
                Plugin.Log.LogError(e);
            }
            if (!string.IsNullOrEmpty(status))
            {
                GUILayout.Label(status, wrap);
            }
            GUI.DragWindow(new Rect(0, 0, 10000, 22));
        }

        void DrawOfficial()
        {
            bool canEdit = OfficialPanel.AtMainMenu;
            GUILayout.Label(canEdit
                ? "勾选启用；越靠上优先级越高（同一条数据以靠上的为准）。关闭窗口时自动重载配置，不用重启游戏。"
                : "已进入存档，只能查看。请回到主菜单再修改（存档中途换配置表可能坏档）。", small);
            var mods = OfficialMods.Visible;
            scroll = GUILayout.BeginScrollView(scroll);
            if (mods.Count == 0)
            {
                GUILayout.Label("没有找到官方 mod。本地 mod 放在 " + Application.persistentDataPath + "/Mod/<名称>/", wrap);
            }
            for (int i = 0; i < mods.Count; i++)
            {
                var m = mods[i];
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUI.enabled = canEdit;
                bool on = GUILayout.Toggle(m.isEnable, "", GUILayout.Width(24));
                if (on != m.isEnable)
                {
                    OfficialMods.SetEnabled(m, on);
                }
                GUI.enabled = true;
                string tag = m.type == 1 ? "工坊" : "本地";
                if (GUILayout.Button($"{i + 1}. {m.data.name}  <size=12><color=#aaaaaa>[{tag}] {m.data.author}</color></size>", rich))
                {
                    selectedMod = selectedMod == m.Id ? null : m.Id;
                }
                GUILayout.FlexibleSpace();
                GUI.enabled = canEdit && i > 0;
                if (GUILayout.Button("↑", GUILayout.Width(32)))
                {
                    OfficialMods.Move(m, up: true);
                }
                GUI.enabled = canEdit && i < mods.Count - 1;
                if (GUILayout.Button("↓", GUILayout.Width(32)))
                {
                    OfficialMods.Move(m, up: false);
                }
                GUI.enabled = Directory.Exists(m.folderPath);
                if (GUILayout.Button("文件夹", GUILayout.Width(64)))
                {
                    OpenFolder(m.folderPath);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                if (selectedMod == m.Id)
                {
                    GUILayout.Label(m.data.description ?? "", small);
                    if (m.data.dependency != null && m.data.dependency.Count > 0)
                    {
                        GUILayout.Label("前置：" + string.Join("，", m.data.dependency.Select(d => d.name)), small);
                    }
                    if (!string.IsNullOrEmpty(m.data.changeLog))
                    {
                        GUILayout.Label("更新：" + m.data.changeLog, small);
                    }
                }
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            GUI.enabled = canEdit;
            if (GUILayout.Button("打开游戏自带的创意工坊面板（订阅 / 上传）", GUILayout.Height(28)))
            {
                Hide();
                OfficialPanel.Open();
            }
            GUI.enabled = true;
            if (GUILayout.Button("打开 MOD 目录", GUILayout.Width(130), GUILayout.Height(28)))
            {
                OpenFolder(Application.persistentDataPath + "/Mod");
            }
            GUILayout.EndHorizontal();
        }

        void DrawPlugins()
        {
            GUILayout.Label("启用/停用要重启游戏才生效（BepInEx 只在启动时加载插件）。停用 = 把 .dll 改名为 .dll.disabled。", small);
            if (!PluginList.PreloaderInstalled)
            {
                GUILayout.Label("<color=#e0a040>没有找到 BepInEx\\patchers\\ZaohuaModManager.Preloader.dll：正在运行的插件文件被占用时，停用会失败。</color>", small);
            }
            scroll = GUILayout.BeginScrollView(scroll);
            foreach (var p in plugins)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUI.enabled = !p.IsSelf;
                bool on = GUILayout.Toggle(p.WillLoad, "", GUILayout.Width(24));
                if (on != p.WillLoad)
                {
                    try
                    {
                        PluginList.SetEnabled(p, on);
                    }
                    catch (Exception e)
                    {
                        status = "修改失败：" + e.Message;
                        Plugin.Log.LogError(e);
                    }
                }
                GUI.enabled = true;
                GUILayout.BeginVertical();
                GUILayout.Label($"{p.Name}  <size=12><color=#aaaaaa>v{p.Version}</color></size>" + (p.IsSelf ? "  <size=12><color=#aaaaaa>（本插件）</color></size>" : ""), rich);
                GUILayout.Label($"{p.RelPath}   {p.Guid}", small);
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                string color = p.Loaded == p.WillLoad ? (p.Loaded ? "#62af47" : "#999999") : "#e0a040";
                GUILayout.Label($"<color={color}>{p.Status}</color>", rich, GUILayout.Width(120));
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新", GUILayout.Height(28)))
            {
                RefreshPlugins();
            }
            if (GUILayout.Button("打开插件文件夹", GUILayout.Height(28)))
            {
                OpenFolder(PluginList.PluginDir);
            }
            GUILayout.EndHorizontal();
        }

        void DrawConflicts()
        {
            GUILayout.Label("检查已启用的官方 mod 是否改了同一张表的同一条数据。按当前顺序，靠上的 mod 生效。", small);
            if (GUILayout.Button("检查冲突", GUILayout.Height(28)))
            {
                try
                {
                    conflicts = OfficialMods.CheckConflicts();
                }
                catch (Exception e)
                {
                    status = "检查失败：" + e.Message;
                    Plugin.Log.LogError(e);
                }
            }
            if (conflicts == null)
            {
                return;
            }
            scroll = GUILayout.BeginScrollView(scroll);
            if (conflicts.Count == 0)
            {
                GUILayout.Label("<color=#62af47>没有检测到冲突。</color>", rich);
            }
            foreach (var c in conflicts)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"【{c.Winner}】覆盖了【{c.Loser}】", header);
                foreach (var (table, keys) in c.Tables)
                {
                    string shown = string.Join("，", keys.Take(12));
                    if (keys.Count > 12)
                    {
                        shown += $" 等 {keys.Count} 条";
                    }
                    GUILayout.Label($"{table}：{shown}", small);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
        }

        static void OpenFolder(string path)
        {
            try
            {
                Process.Start("explorer.exe", path.Replace("/", "\\"));
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }
    }
}
