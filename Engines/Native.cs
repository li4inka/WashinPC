using System;
using System.Runtime.InteropServices;

namespace WashinPC
{
    internal static class Native
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHQueryRecycleBin(
            string pszRootPath,
            ref SHQUERYRBINFO pSHQueryRBInfo);

        [Flags]
        public enum RecycleFlags : uint
        {
            SHERB_NOCONFIRMATION = 0x00000001,
            SHERB_NOPROGRESSUI = 0x00000002,
            SHERB_NOSOUND = 0x00000004
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(
            IntPtr hwnd,
            string pszRootPath,
            RecycleFlags dwFlags);

        // ================= PUBLIC API =================

        public static bool IsRecycleBinEmpty()
        {
            try
            {
                var info = new SHQUERYRBINFO
                {
                    cbSize = Marshal.SizeOf<SHQUERYRBINFO>()
                };

                int hr = SHQueryRecycleBin(null, ref info);
                if (hr != 0)
                    return true;

                return info.i64NumItems == 0;
            }
            catch
            {
                return true;
            }
        }

        public static bool EmptyRecycleBinQuiet()
        {
            try
            {
                int hr = SHEmptyRecycleBin(
                    IntPtr.Zero,
                    null,
                    RecycleFlags.SHERB_NOCONFIRMATION |
                    RecycleFlags.SHERB_NOPROGRESSUI |
                    RecycleFlags.SHERB_NOSOUND);

                return hr == 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
