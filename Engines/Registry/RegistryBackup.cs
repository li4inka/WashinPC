using System;
using System.Diagnostics;
using System.IO;

namespace WashinPC.Engines.Registry
{
    internal static class RegistryBackup
    {
        public static string CreateHKLMBackup()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "WashinPC_Backups");

            Directory.CreateDirectory(dir);

            var file = Path.Combine(dir, $"RegistryBackup_HKLM_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.reg");

            // reg.exe export HKLM "<file>" /y
            var psi = new ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = $"export HKLM \"{file}\" /y",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var p = Process.Start(psi))
            {
                p.WaitForExit();

                var stdout = p.StandardOutput.ReadToEnd();
                var stderr = p.StandardError.ReadToEnd();

                Utils.Log("RegistryBackup stdout: " + stdout);
                if (!string.IsNullOrWhiteSpace(stderr))
                    Utils.Log("RegistryBackup stderr: " + stderr);

                if (p.ExitCode != 0)
                    throw new Exception("reg.exe export failed, exit code: " + p.ExitCode);
            }

            return file;
        }
    }
}
