using System.Collections.ObjectModel;

namespace WashinPC.ViewModels.Pages

{
    public class CleanerItemView
    {
        public bool IsChecked { get; set; }

        public string Name { get; set; }

        public long TotalBytes { get; set; }

        public ObservableCollection<string> Files { get; set; }

        public CleanerItemView()
        {
            IsChecked = true;
            Files = new ObservableCollection<string>();
        }
        public string SizeText
        {
            get
            {
                double size = TotalBytes;

                if (size >= 1024 * 1024 * 1024)
                    return $"{size / (1024 * 1024 * 1024):0.00} ГБ";

                if (size >= 1024 * 1024)
                    return $"{size / (1024 * 1024):0.00} МБ";

                if (size >= 1024)
                    return $"{size / 1024:0.00} КБ";

                return $"{size} Б";
            }
        }
    }
}
