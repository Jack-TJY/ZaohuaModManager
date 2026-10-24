using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZaohuaModManager
{
    /// <summary>主菜单左下角的「MOD 管理」按钮，以及快捷键（任何时候都能打开窗口）。按住按钮可以拖动位置。</summary>
    internal class MainMenuButton
    {
        /// <summary>鼠标在按钮或窗口上：挡住游戏 UI 的射线，避免点穿（见 GraphicRaycasterPatch）。</summary>
        internal static bool MouseOver;

        readonly ManagerWindow window = new ManagerWindow();

        Font font;

        string toast;

        float toastUntil;

        bool pressing, dragged;

        Vector2 pressMouse, pressPos, dragPos;

        bool ButtonVisible => Plugin.ShowButton.Value && Game.OfficialPanel.AtMainMenu && !Game.OfficialPanel.IsOpen && !window.Visible;

        public void Update()
        {
            if (Plugin.OpenKey.Value.IsDown() && !window.CapturingKey)
            {
                Toggle();
            }
            var mouse = Input.mousePosition;
            var gui = new Vector2(mouse.x, Screen.height - mouse.y);
            bool visible;
            try
            {
                visible = ButtonVisible;
            }
            catch
            {
                visible = false;
            }
            MouseOver = (visible && (pressing || ButtonRect().Contains(gui))) || window.Contains(gui);
        }

        void Toggle()
        {
            try
            {
                if (window.Visible)
                {
                    window.Hide();
                }
                else if (Game.OfficialPanel.IsOpen)
                {
                    // 游戏自带面板开着时，快捷键先把它关掉
                    Game.OfficialPanel.Close();
                }
                else
                {
                    window.Show();
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
                toast = "打开 MOD 管理失败：" + e.Message;
                toastUntil = Time.unscaledTime + 4f;
            }
        }

        public void OnGUI()
        {
            bool visible;
            try
            {
                visible = ButtonVisible;
            }
            catch
            {
                // 游戏刚启动时 UIMgr 还没建好
                return;
            }
            if (!visible && toast == null && !window.Visible)
            {
                return;
            }
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "SimSun" }, 16);
            }
            var oldFont = GUI.skin.font;
            GUI.skin.font = font;
            try
            {
                if (visible)
                {
                    DrawButton();
                }
                window.OnGUI();
                DrawToast();
            }
            finally
            {
                GUI.skin.font = oldFont;
            }
        }

        /// <summary>按钮当前位置。配置里是负数时用默认位置：左下角。</summary>
        Rect ButtonRect()
        {
            const float w = 150, h = 36;
            float x = dragged ? dragPos.x : Plugin.ButtonX.Value < 0 ? 20 : Plugin.ButtonX.Value;
            float y = dragged ? dragPos.y : Plugin.ButtonY.Value < 0 ? Screen.height - h - 20 : Plugin.ButtonY.Value;
            return new Rect(Mathf.Clamp(x, 0, Screen.width - w), Mathf.Clamp(y, 0, Screen.height - h), w, h);
        }

        /// <summary>单击打开面板，按住拖动可以挪位置，松手后记到配置里。</summary>
        void DrawButton()
        {
            var r = ButtonRect();
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
            {
                pressing = true;
                dragged = false;
                pressMouse = e.mousePosition;
                pressPos = r.position;
                e.Use();
            }
            else if (pressing && e.type == EventType.MouseDrag)
            {
                if ((e.mousePosition - pressMouse).sqrMagnitude > 16)
                {
                    dragged = true;
                }
                if (dragged)
                {
                    dragPos = pressPos + e.mousePosition - pressMouse;
                }
                e.Use();
            }
            else if (pressing && e.type == EventType.MouseUp)
            {
                pressing = false;
                if (dragged)
                {
                    var p = ButtonRect().position;
                    dragged = false;
                    Plugin.ButtonX.Value = Mathf.RoundToInt(p.x);
                    Plugin.ButtonY.Value = Mathf.RoundToInt(p.y);
                }
                else
                {
                    Toggle();
                }
                e.Use();
            }
            GUI.Button(ButtonRect(), $"MOD 管理 ({Plugin.OpenKey.Value})");
        }

        void DrawToast()
        {
            if (toast == null)
            {
                return;
            }
            if (Time.unscaledTime > toastUntil)
            {
                toast = null;
                return;
            }
            var r = new Rect(Screen.width / 2f - 260, Screen.height - 120, 520, 40);
            GUI.Box(r, toast);
        }
    }

    [HarmonyPatch(typeof(GraphicRaycaster), nameof(GraphicRaycaster.Raycast), new[] { typeof(PointerEventData), typeof(List<RaycastResult>) })]
    internal static class GraphicRaycasterPatch
    {
        private static bool Prefix()
        {
            return !MainMenuButton.MouseOver;
        }
    }

    /// <summary>射线被挡掉后游戏会以为鼠标不在 UI 上；鼠标在按钮上时告诉游戏「在 UI 上」。</summary>
    [HarmonyPatch(typeof(EventSystem), nameof(EventSystem.IsPointerOverGameObject), new Type[0])]
    internal static class PointerOverPatch
    {
        private static void Postfix(ref bool __result)
        {
            __result |= MainMenuButton.MouseOver;
        }
    }

    [HarmonyPatch(typeof(EventSystem), nameof(EventSystem.IsPointerOverGameObject), new[] { typeof(int) })]
    internal static class PointerOverIdPatch
    {
        private static void Postfix(ref bool __result)
        {
            __result |= MainMenuButton.MouseOver;
        }
    }
}
