using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using UnityEngine;

namespace ZaohuaModManager
{
    /// <summary>
    /// 「插件设置」页：直接改各个已加载插件的 BepInEx 配置（就是 BepInEx\config\*.cfg 里的选项）。
    /// 改动立即写回 cfg；插件每次都读配置的会马上生效，只在启动时读的要重启游戏。
    /// </summary>
    internal class SettingsPage
    {
        List<PluginInfo> plugins;

        string selected;

        Vector2 listScroll, entryScroll;

        /// <summary>数字/文字输入框里还没提交的内容。</summary>
        readonly Dictionary<ConfigEntryBase, string> drafts = new Dictionary<ConfigEntryBase, string>();

        /// <summary>输入格式不对时的提示。</summary>
        readonly Dictionary<ConfigEntryBase, string> errors = new Dictionary<ConfigEntryBase, string>();

        /// <summary>正在等按键的快捷键选项。</summary>
        ConfigEntryBase binding;

        GUIStyle small, rich;

        public void Refresh()
        {
            plugins = Chainloader.PluginInfos.Values
                .Where(p => p.Instance != null && p.Instance.Config != null && p.Instance.Config.Count > 0)
                .OrderBy(p => p.Metadata.Name, StringComparer.CurrentCulture)
                .ToList();
            if (selected == null || plugins.All(p => p.Metadata.GUID != selected))
            {
                selected = plugins.FirstOrDefault()?.Metadata.GUID;
            }
            drafts.Clear();
            errors.Clear();
            binding = null;
        }

        public void Draw(GUIStyle small, GUIStyle rich)
        {
            this.small = small;
            this.rich = rich;
            if (plugins == null)
            {
                Refresh();
            }
            GUILayout.Label("改动立即保存到 BepInEx\\config 里的 cfg 文件。有的设置马上生效，有的要重启游戏（看插件自己的说明）。停用的插件不显示。", small);
            GUILayout.BeginHorizontal();
            listScroll = GUILayout.BeginScrollView(listScroll, GUILayout.Width(200));
            foreach (var p in plugins)
            {
                bool on = p.Metadata.GUID == selected;
                if (GUILayout.Toggle(on, p.Metadata.Name, GUI.skin.button) && !on)
                {
                    selected = p.Metadata.GUID;
                    entryScroll = Vector2.zero;
                    drafts.Clear();
                    binding = null;
                }
            }
            GUILayout.EndScrollView();
            entryScroll = GUILayout.BeginScrollView(entryScroll);
            var info = plugins.FirstOrDefault(p => p.Metadata.GUID == selected);
            if (info == null)
            {
                GUILayout.Label("没有可设置的插件。");
            }
            else
            {
                DrawConfig(info.Instance.Config);
            }
            GUILayout.EndScrollView();
            GUILayout.EndHorizontal();
        }

        void DrawConfig(ConfigFile cfg)
        {
            var entries = cfg.Select(kv => kv.Value).Where(e => e != null)
                .OrderBy(e => e.Definition.Section, StringComparer.CurrentCulture)
                .ToList();
            string section = null;
            foreach (var e in entries)
            {
                if (e.Definition.Section != section)
                {
                    section = e.Definition.Section;
                    GUILayout.Space(4);
                    GUILayout.Label($"<b>[{section}]</b>", rich);
                }
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.BeginHorizontal();
                GUILayout.Label(e.Definition.Key, rich, GUILayout.Width(160));
                try
                {
                    DrawEditor(e);
                }
                catch (Exception ex)
                {
                    GUILayout.Label("无法编辑：" + ex.Message, small);
                }
                bool isDefault = Equals(e.BoxedValue, e.DefaultValue);
                GUI.enabled = !isDefault;
                if (GUILayout.Button("默认", GUILayout.Width(50)))
                {
                    e.BoxedValue = e.DefaultValue;
                    drafts.Remove(e);
                    errors.Remove(e);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                string desc = e.Description?.Description;
                if (!string.IsNullOrEmpty(desc))
                {
                    GUILayout.Label(desc, small);
                }
                GUILayout.EndVertical();
            }
        }

        void DrawEditor(ConfigEntryBase e)
        {
            var type = e.SettingType;
            var acceptable = e.Description?.AcceptableValues;
            if (type == typeof(bool))
            {
                bool v = (bool)e.BoxedValue;
                bool nv = GUILayout.Toggle(v, v ? "开" : "关");
                if (nv != v)
                {
                    e.BoxedValue = nv;
                }
            }
            else if (type.IsEnum)
            {
                DrawChoices(e, Enum.GetValues(type).Cast<object>().ToArray());
            }
            else if (acceptable != null && acceptable.GetType().IsGenericType
                     && acceptable.GetType().GetGenericTypeDefinition() == typeof(AcceptableValueList<>))
            {
                var values = ((Array)acceptable.GetType().GetProperty("AcceptableValues").GetValue(acceptable, null)).Cast<object>().ToArray();
                DrawChoices(e, values);
            }
            else if (type == typeof(KeyboardShortcut))
            {
                DrawShortcut(e);
            }
            else
            {
                DrawText(e);
            }
            GUILayout.FlexibleSpace();
        }

        void DrawChoices(ConfigEntryBase e, object[] values)
        {
            var names = values.Select(v => v.ToString()).ToArray();
            int cur = Array.IndexOf(names, e.BoxedValue?.ToString());
            int next = GUILayout.SelectionGrid(cur, names, Math.Min(names.Length, 3));
            if (next != cur && next >= 0)
            {
                e.BoxedValue = values[next];
            }
        }

        /// <summary>点一下后按下想要的键（可带 Ctrl/Shift/Alt），Esc 取消。</summary>
        void DrawShortcut(ConfigEntryBase e)
        {
            if (binding == e)
            {
                GUILayout.Button("请按键…（Esc 取消）", GUILayout.Width(200));
                var ev = Event.current;
                if (ev.type == EventType.KeyDown && ev.keyCode != KeyCode.None)
                {
                    if (ev.keyCode != KeyCode.Escape && !IsModifier(ev.keyCode))
                    {
                        var mods = new List<KeyCode>();
                        if (ev.control) mods.Add(KeyCode.LeftControl);
                        if (ev.shift) mods.Add(KeyCode.LeftShift);
                        if (ev.alt) mods.Add(KeyCode.LeftAlt);
                        e.BoxedValue = new KeyboardShortcut(ev.keyCode, mods.ToArray());
                    }
                    if (!IsModifier(ev.keyCode))
                    {
                        binding = null;
                    }
                    ev.Use();
                }
                return;
            }
            if (GUILayout.Button(e.BoxedValue.ToString(), GUILayout.Width(200)))
            {
                binding = e;
            }
        }

        static bool IsModifier(KeyCode k) =>
            k == KeyCode.LeftControl || k == KeyCode.RightControl || k == KeyCode.LeftShift || k == KeyCode.RightShift
            || k == KeyCode.LeftAlt || k == KeyCode.RightAlt;

        /// <summary>数字和文字：输入后点「确定」或回车才写入，填错会提示。</summary>
        void DrawText(ConfigEntryBase e)
        {
            string current = TomlTypeConverter.ConvertToString(e.BoxedValue, e.SettingType);
            if (!drafts.TryGetValue(e, out string text))
            {
                text = current;
            }
            string name = "cfg_" + e.Definition.Section + "_" + e.Definition.Key;
            GUI.SetNextControlName(name);
            text = GUILayout.TextField(text, GUILayout.MinWidth(120));
            if (text != current)
            {
                drafts[e] = text;
            }
            else
            {
                drafts.Remove(e);
            }
            var range = e.Description?.AcceptableValues;
            if (range != null)
            {
                GUILayout.Label(range.ToDescriptionString().Replace("# Acceptable value range:", "范围").Replace("# Acceptable values:", "可选"), small);
            }
            bool enter = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == name;
            GUI.enabled = drafts.ContainsKey(e);
            if (GUILayout.Button("确定", GUILayout.Width(50)) || (enter && GUI.enabled))
            {
                try
                {
                    object v = TomlTypeConverter.ConvertToValue(text.Trim(), e.SettingType);
                    if (range != null && !range.IsValid(v))
                    {
                        v = range.Clamp(v);
                    }
                    e.BoxedValue = v;
                    drafts.Remove(e);
                    errors.Remove(e);
                }
                catch (Exception ex)
                {
                    errors[e] = "格式不对：" + ex.Message;
                }
                if (enter)
                {
                    Event.current.Use();
                }
            }
            GUI.enabled = true;
            if (errors.TryGetValue(e, out string err))
            {
                GUILayout.Label($"<color=#e06060>{err}</color>", rich);
            }
        }

        /// <summary>正在等快捷键时，主窗口的 F10 不应关窗。</summary>
        public bool CapturingKey => binding != null;
    }
}
