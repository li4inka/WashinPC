using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using Microsoft.Win32;

namespace WashinPC
{
    public static class Utils
    {
        // ───────────── ADMIN ─────────────

        public static bool IsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        // ───────────── LOGGING ─────────────

        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(
                    AppBootstrap.LogFile,
                    $"{DateTime.Now:u} {message}\r\n"
                );
            }
            catch { }
        }

        // ───────────── PATH HELPERS ─────────────

        public static string ExpandPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            try { return Environment.ExpandEnvironmentVariables(path); }
            catch { return path; }
        }

        public static IEnumerable<string> ExpandWildcards(string pattern)
        {
            var result = new List<string>();
            var p = ExpandPath(pattern);

            try
            {
                string dir = Path.GetDirectoryName(p);
                if (string.IsNullOrEmpty(dir)) dir = ".";

                string file = Path.GetFileName(p);
                if (Directory.Exists(dir))
                    result.AddRange(Directory.GetFiles(dir, file));
            }
            catch { }

            return result;
        }

        // ───────────── FILE DELETE ─────────────

        public static bool SafeDeleteFile(string file, IList<string> log)
        {
            try
            {
                if (!File.Exists(file))
                    return false;

                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);

                log?.Add("Удален файл: " + file);
                Log("Deleted file: " + file);
                return true;
            }
            catch (Exception ex)
            {
                log?.Add("Ошибка удаления файла: " + file + " => " + ex.Message);
                Log("ERROR deleting file: " + file + " => " + ex);
                return false;
            }
        }

        // ───────────── DIRECTORY DELETE ─────────────

        /// <summary>
        /// Deletes directory contents (files + subdirs), but not the directory itself
        /// </summary>
        public static int SafeDeleteDirectoryContents(string dir, IList<string> log)
        {
            int count = 0;

            try
            {
                if (!Directory.Exists(dir))
                    return 0;

                foreach (var f in Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    if (SafeDeleteFile(f, log))
                        count++;
                }

                foreach (var d in Directory.GetDirectories(dir))
                {
                    count += SafeDeleteDirectory(d, log);
                }
            }
            catch (Exception ex)
            {
                Log("ERROR deleting directory contents: " + dir + " => " + ex);
            }

            return count;
        }

        /// <summary>
        /// Deletes directory and all its contents safely
        /// </summary>
        public static int SafeDeleteDirectory(string dir, IList<string> log)
        {
            int count = 0;

            try
            {
                if (!Directory.Exists(dir))
                    return 0;

                count += SafeDeleteDirectoryContents(dir, log);
                Directory.Delete(dir, true);

                log?.Add("Удален каталог: " + dir);
                Log("Deleted directory: " + dir);

                count++;
            }
            catch (Exception ex)
            {
                Log("ERROR deleting directory: " + dir + " => " + ex);

                // fallback: try to clean contents only
                count += SafeDeleteDirectoryContents(dir, log);
            }

            return count;
        }

        // ───────────── OTHER HELPERS ─────────────

        public static string GetKnownFolderPath_XP_Compatible(string rel)
        {
            try
            {
                switch (rel)
                {
                    case "Temp": return Path.GetTempPath();
                    case "AppData": return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    case "LocalAppData": return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    case "Cookies": return Environment.GetFolderPath(Environment.SpecialFolder.Cookies);
                    case "Recent": return Environment.GetFolderPath(Environment.SpecialFolder.Recent);
                    default: return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                }
            }
            catch
            {
                return "";
            }
        }

        public static string[] SplitLines(string s)
        {
            return (s ?? "")
                .Replace("\r", "")
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string ReadRegistryString(RegistryKey root, string subkey, string name)
        {
            try
            {
                using (var k = root.OpenSubKey(subkey))
                {
                    if (k == null) return null;
                    return k.GetValue(name, null) as string;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
