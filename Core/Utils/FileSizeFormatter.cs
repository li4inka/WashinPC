namespace WashinPC.Core.Utils
{
    public static class FileSizeFormatter
    {
        public static string Format(long bytes)
        {
            if (bytes < 1024)
                return bytes + " B";

            double kb = bytes / 1024.0;
            if (kb < 1024)
                return kb.ToString("0.##") + " KB";

            double mb = kb / 1024.0;
            if (mb < 1024)
                return mb.ToString("0.##") + " MB";

            double gb = mb / 1024.0;
            return gb.ToString("0.##") + " GB";
        }
    }
}
