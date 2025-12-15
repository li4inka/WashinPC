using System;
using System.Runtime.InteropServices;

namespace WashinPC
{
    internal static class Native
    {
        [Flags]
        public enum RecycleFlags : uint
        {
            SHERB_NOCONFIRMATION = 0x00000001,
            SHERB_NOPROGRESSUI   = 0x00000002,
            SHERB_NOSOUND        = 0x00000004
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, RecycleFlags dwFlags);

        public static bool EmptyRecycleBinQuiet()
        {
            try
            {
                int hr = SHEmptyRecycleBin(IntPtr.Zero, null,
                    RecycleFlags.SHERB_NOCONFIRMATION | RecycleFlags.SHERB_NOPROGRESSUI | RecycleFlags.SHERB_NOSOUND);
                return hr == 0;
            }
            catch { return false; }
        }
    }
}