using System;
using System.Collections.Generic;
using System.IO;
using WashinPC.Core.Utils;

namespace WashinPC.Engines.Privacy
{
    public class PrivacyGroupResult
    {
        public string LocalizationKey;
        public List<string> Files = new List<string>();
        public long TotalBytes;
    }

    public class PrivacyRule
    {
        public string LocalizationKey;
        public Func<IEnumerable<string>> FilesProvider;
    }

    public sealed class PrivacyEngine
    {
        private readonly List<PrivacyRule> _rules = new List<PrivacyRule>();

        public PrivacyEngine()
        {
            InitRules();
        }

        private void InitRules()
        {
            _rules.Clear();

            _rules.Add(new PrivacyRule
            {
                LocalizationKey = "privacy.chrome",
                FilesProvider = () => new[]
                {
                    Environment.ExpandEnvironmentVariables(
                        "%LOCALAPPDATA%\\Google\\Chrome\\User Data\\Default\\History"),
                    Environment.ExpandEnvironmentVariables(
                        "%LOCALAPPDATA%\\Google\\Chrome\\User Data\\Default\\Cookies")
                }
            });

            _rules.Add(new PrivacyRule
            {
                LocalizationKey = "privacy.edge",
                FilesProvider = () => new[]
                {
                    Environment.ExpandEnvironmentVariables(
                        "%LOCALAPPDATA%\\Microsoft\\Edge\\User Data\\Default\\History"),
                    Environment.ExpandEnvironmentVariables(
                        "%LOCALAPPDATA%\\Microsoft\\Edge\\User Data\\Default\\Cookies")
                }
            });
        }

        public List<PrivacyGroupResult> Scan(out int totalFiles, out long totalBytes)
        {
            totalFiles = 0;
            totalBytes = 0;

            var result = new List<PrivacyGroupResult>();

            foreach (var rule in _rules)
            {
                var gr = new PrivacyGroupResult
                {
                    LocalizationKey = rule.LocalizationKey
                };

                foreach (var path in rule.FilesProvider())
                {
                    var p = Utils.ExpandPath(path);
                    if (!File.Exists(p))
                        continue;

                    gr.Files.Add(p);
                    try
                    {
                        gr.TotalBytes += new FileInfo(p).Length;
                    }
                    catch { }
                }

                if (gr.Files.Count > 0)
                {
                    result.Add(gr);
                    totalFiles += gr.Files.Count;
                    totalBytes += gr.TotalBytes;
                }
            }

            return result;
        }

        public void CleanSelected(IEnumerable<string> files)
        {
            foreach (var f in files)
            {
                try
                {
                    if (File.Exists(f))
                        File.Delete(f);
                }
                catch
                {
                    // как и в Cleaner — не падаем
                }
            }
        }
    }
}
