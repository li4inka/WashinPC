using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace WashinPC.Engines.Operations
{
    internal enum PathState { Present, Missing, Unknown }
    internal static class PathSafety
    {
        internal static string Canonical(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || path.StartsWith(@"\\") || path.IndexOf(':', 2) >= 0)
                throw new IOException("Unsupported path.");
            var full = Path.GetFullPath(path);
            if (full.Length < 3 || full[1] != ':' || full[2] != '\\' || full.IndexOf('%') >= 0) throw new IOException("Unsupported path.");
            return full;
        }
        internal static bool Under(string path, string root)
        {
            try { return Canonical(path).StartsWith(Canonical(root).TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }
        internal static void NoLinks(string path)
        {
            var current = Canonical(path);
            while (!string.IsNullOrEmpty(current))
            {
                var attributes = File.GetAttributes(current);
                if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.Offline)) != 0) throw new IOException("Linked or offline path.");
                current = Path.GetDirectoryName(current);
            }
        }
        internal static PathState Probe(string path)
        {
            try
            {
                path = Canonical(Environment.ExpandEnvironmentVariables(path).Trim().Trim('"'));
                try { NoLinks(path); return PathState.Present; }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { return PathState.Unknown; }
                var parent = Path.GetDirectoryName(path);
                NoLinks(parent);
                foreach (var entry in Directory.EnumerateFileSystemEntries(parent))
                    if (string.Equals(entry, path, StringComparison.OrdinalIgnoreCase)) return PathState.Unknown;
                return PathState.Missing;
            }
            catch { return PathState.Unknown; }
        }
        internal static IEnumerable<string> Walk(string root, FileScanPlan plan, System.Threading.CancellationToken token)
        {
            var pending = new Stack<string>(); pending.Push(root);
            while (pending.Count > 0 && !token.IsCancellationRequested)
            {
                var dir = pending.Pop(); string[] files, directories;
                try { NoLinks(dir); files = Directory.GetFiles(dir); directories = Directory.GetDirectories(dir); }
                catch { plan.Notices.Add(new ItemResult { Path = dir, Status = ItemStatus.Skipped, Reason = "op.file_unavailable" }); continue; }
                foreach (var file in files) { if (token.IsCancellationRequested) yield break; yield return file; }
                foreach (var child in directories) pending.Push(child);
            }
        }
    }
    // Ancestors are opened without FILE_SHARE_DELETE: checked directories cannot be renamed during deletion.
    internal sealed class DirectoryLease : IDisposable
    {
        private readonly List<SafeFileHandle> _handles = new List<SafeFileHandle>();
        internal DirectoryLease(string path)
        {
            try
            {
                var ancestors = new Stack<string>();
                for (var p = PathSafety.Canonical(path); !string.IsNullOrEmpty(p); p = Path.GetDirectoryName(p)) ancestors.Push(p);
                while (ancestors.Count > 0)
                {
                    var handle = SafeFile.CreateFile(ancestors.Pop(), 0x81, 3, IntPtr.Zero, 3, 0x02200000, IntPtr.Zero);
                    if (handle.IsInvalid) { handle.Dispose(); throw new Win32Exception(); }
                    _handles.Add(handle);
                    SafeFile.FileInfoNative info;
                    if (!SafeFile.GetFileInformationByHandle(handle, out info) || (info.Attributes & (0x400u | 0x1000u)) != 0)
                        throw new IOException("Unsafe ancestor.");
                }
            }
            catch { Dispose(); throw; }
        }
        public void Dispose() { for (var i = _handles.Count - 1; i >= 0; i--) _handles[i].Dispose(); _handles.Clear(); }
    }
    internal sealed class SafeFile : IDisposable
    {
        private SafeFileHandle _handle;
        internal string Fingerprint { get; private set; }
        internal long Length { get; private set; }
        internal static SafeFile Open(string path, string root, bool deletion, bool allowWrites = false)
        {
            if (!PathSafety.Under(path, root)) throw new IOException("Outside allowed root.");
            PathSafety.NoLinks(path);
            var result = new SafeFile();
            try
            {
                result._handle = CreateFile(path, 0x80000000u | (deletion ? 0x10000u : 0), allowWrites ? 3u : 1u, IntPtr.Zero, 3, 0x00200000, IntPtr.Zero);
                if (result._handle.IsInvalid) throw new Win32Exception();
                FileInfoNative info;
                if (!GetFileInformationByHandle(result._handle, out info)) throw new Win32Exception();
                if (info.Links != 1 || (info.Attributes & (0x400u | 0x1000u | 0x4000u | 0x800u | 0x200u | 0x10u)) != 0)
                    throw new IOException("Special or linked file.");
                StreamInfo stream;
                var search = FindFirstStreamW(path, 0, out stream, 0);
                if (search == new IntPtr(-1)) throw new IOException("Cannot verify streams.");
                try
                {
                    do { if (stream.Name != "::$DATA") throw new IOException("Named streams are preserved."); }
                    while (FindNextStreamW(search, out stream));
                    if (Marshal.GetLastWin32Error() != 38) throw new Win32Exception();
                }
                finally { FindClose(search); }
                result.Length = ((long)info.SizeHigh << 32) | info.SizeLow;
                result.Fingerprint = string.Join(":", info.Volume, info.IndexHigh, info.IndexLow, info.WriteHigh, info.WriteLow, info.CreateHigh, info.CreateLow, info.SizeHigh, info.SizeLow, info.Attributes);
                return result;
            }
            catch { result.Dispose(); throw; }
        }
        internal bool IsSqlite()
        {
            var bytes = new byte[16]; uint read;
            return ReadFile(_handle, bytes, 16, out read, IntPtr.Zero) && read == 16 && System.Text.Encoding.ASCII.GetString(bytes) == "SQLite format 3\0";
        }
        internal void Delete()
        {
            var disposition = new Disposition { DeleteFile = true };
            if (!SetFileInformationByHandle(_handle, 4, ref disposition, (uint)Marshal.SizeOf(typeof(Disposition)))) throw new Win32Exception();
        }
        public void Dispose() { _handle?.Dispose(); }
        [StructLayout(LayoutKind.Sequential)] internal struct FileInfoNative
        {
            internal uint Attributes, CreateLow, CreateHigh, AccessLow, AccessHigh, WriteLow, WriteHigh, Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
        }
        [StructLayout(LayoutKind.Sequential)] private struct Disposition { [MarshalAs(UnmanagedType.Bool)] internal bool DeleteFile; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct StreamInfo { internal long Size; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 296)] internal string Name; }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] internal static extern SafeFileHandle CreateFile(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
        [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool GetFileInformationByHandle(SafeFileHandle handle, out FileInfoNative info);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetFileInformationByHandle(SafeFileHandle handle, int type, ref Disposition data, uint size);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool ReadFile(SafeFileHandle handle, byte[] buffer, uint count, out uint read, IntPtr overlapped);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr FindFirstStreamW(string path, int level, out StreamInfo data, uint flags);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool FindNextStreamW(IntPtr handle, out StreamInfo data);
        [DllImport("kernel32.dll")] private static extern bool FindClose(IntPtr handle);
    }
}
