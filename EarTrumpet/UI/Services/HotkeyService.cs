using EarTrumpet.Interop.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EarTrumpet.UI.Services
{
    class HotkeyService
    {
        public enum HotkeyKind
        {
            PlaybackFlyout,
            RecordingFlyout,
        }

        public static event Action<HotkeyKind> Pressed;

        private static KeyboardHook s_hook;
        private static Dictionary<HotkeyKind, Tuple<int, SettingsService.HotkeyData>> s_hotkeys = new Dictionary<HotkeyKind, Tuple<int, SettingsService.HotkeyData>>();

        public static void Bind(HotkeyKind kind, SettingsService.HotkeyData hotkey)
        {
            Trace.WriteLine($"HotkeyService Register {kind} {hotkey}");

            if (hotkey.Key == System.Windows.Forms.Keys.None)
            {
                return;
            }

            if (s_hook == null)
            {
                s_hook = new KeyboardHook();
                s_hook.KeyPressed += Hotkey_KeyPressed;
            }

            if (s_hotkeys.ContainsKey(kind))
            {
                s_hook.Remove(s_hotkeys[kind].Item1);
                s_hotkeys.Remove(kind);
            }

            try
            {
                var id = s_hook.Add(hotkey.Key, hotkey.Modifiers);
                s_hotkeys.Add(kind, new Tuple<int, SettingsService.HotkeyData>(id, hotkey));
            }
            catch (Exception ex)
            {
                AppTrace.LogWarning(ex);
            }
        }

        private static void Hotkey_KeyPressed(object sender, KeyboardHook.KeyPressedEventArgs e)
        {
            Trace.WriteLine($"HotkeyService Hotkey_KeyPressed");

            foreach(var hotkey in s_hotkeys)
            {
                if (hotkey.Value.Item2.Key == e.Key &&
                    hotkey.Value.Item2.Modifiers == e.Modifiers)
                {
                    Pressed?.Invoke(hotkey.Key);
                    break;
                }
            }
        }

        public static void Unbind(HotkeyKind kind)
        {
            Trace.WriteLine($"HotkeyService Unbind {kind}");

            if (s_hotkeys.ContainsKey(kind))
            {
                s_hook.Remove(s_hotkeys[kind].Item1);
                s_hotkeys.Remove(kind);
            }
        }
    }
}
