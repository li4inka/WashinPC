using Microsoft.Win32;

namespace WashinPC.Core
{
    public static class AppSettings
    {
        private const string Root = @"Software\WashinPC";

        public static bool DarkMode
        {
            get { return ReadBool("DarkMode", false); }
            set { WriteBool("DarkMode", value); }
        }

        public static string Language
        {
            get { return ReadString("Language", "ru"); }
            set { WriteString("Language", value); }
        }

        private static bool ReadBool(string name, bool def)
        {
            RegistryKey key = null;
            try
            {
                key = Registry.CurrentUser.CreateSubKey(Root);
                object v = key.GetValue(name);
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
            RegistryKey key = null;
            try
            {
                key = Registry.CurrentUser.CreateSubKey(Root);
                key.SetValue(name, value ? 1 : 0, RegistryValueKind.DWord);
            }
            finally
            {
                if (key != null) key.Close();
            }
        }

        private static string ReadString(string name, string def)
        {
            RegistryKey key = null;
            try
            {
                key = Registry.CurrentUser.CreateSubKey(Root);
                object v = key.GetValue(name);
                return v != null ? v.ToString() : def;
            }
            finally
            {
                if (key != null) key.Close();
            }
        }

        private static void WriteString(string name, string value)
        {
            RegistryKey key = null;
            try
            {
                key = Registry.CurrentUser.CreateSubKey(Root);
                key.SetValue(name, value, RegistryValueKind.String);
            }
            finally
            {
                if (key != null) key.Close();
            }
        }
    }
}
