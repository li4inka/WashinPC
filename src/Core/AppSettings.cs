using System;
using Microsoft.Win32;

namespace WashinPC.Core
{
    public static class AppSettings
    {
        internal static bool UseMemoryStore;
        private static readonly System.Collections.Generic.Dictionary<string, object> Memory = new System.Collections.Generic.Dictionary<string, object>();
        private const string Root = @"Software\WashinPC";
        public static event Action<string> SettingChanged;

        public static bool DarkMode
        {
            get { return ReadBool("DarkMode", false); }
            set { WriteBool("DarkMode", value); }
        }

        public static string ThemeMode
        {
            get { return ReadString("ThemeMode", "light"); }
            set { WriteString("ThemeMode", value); }
        }

        public static string Language
        {
            get { return ReadString("Language", "ru"); }
            set { WriteString("Language", value); }
        }

        public static bool RegistryExpertMode
        {
            get { return ReadBool("RegistryExpertMode", false); }
            set { WriteBool("RegistryExpertMode", value); }
        }

        public static bool VerboseDiagnostics
        {
            get { return ReadBool("VerboseDiagnostics", false); }
            set { WriteBool("VerboseDiagnostics", value); }
        }

        private static bool ReadBool(string name, bool def)
        {
            if (UseMemoryStore) return Memory.ContainsKey(name) ? (bool)Memory[name] : def;
            RegistryKey key = null;
            try
            {
                key = Registry.CurrentUser.OpenSubKey(Root, false);
                object v = key?.GetValue(name);
                if (v == null) return def;
                return ((int)v) == 1;
            }
            finally
            {
                if (key != null) key.Close();
            }
        }

        private static void WriteBool(string name, bool value)
        {
            if (UseMemoryStore) { Memory[name] = value; SettingChanged?.Invoke(name); return; }
            RegistryKey key = null;
            try
            {
                key = Registry.CurrentUser.CreateSubKey(Root);
                key.SetValue(name, value ? 1 : 0, RegistryValueKind.DWord);
                SettingChanged?.Invoke(name);
            }
            finally
            {
                if (key != null) key.Close();
            }
        }

        private static string ReadString(string name, string def)
        {
            if (UseMemoryStore) return Memory.ContainsKey(name) ? (string)Memory[name] : def;
            RegistryKey key = null;
            try
            {
                key = Registry.CurrentUser.OpenSubKey(Root, false);
                object v = key?.GetValue(name);
                return v != null ? v.ToString() : def;
            }
            finally
            {
                if (key != null) key.Close();
            }
        }

        private static void WriteString(string name, string value)
        {
            if (UseMemoryStore) { Memory[name] = value; SettingChanged?.Invoke(name); return; }
            RegistryKey key = null;
            try
            {
                key = Registry.CurrentUser.CreateSubKey(Root);
                key.SetValue(name, value, RegistryValueKind.String);
                SettingChanged?.Invoke(name);
            }
            finally
            {
                if (key != null) key.Close();
            }
        }
    }
}
