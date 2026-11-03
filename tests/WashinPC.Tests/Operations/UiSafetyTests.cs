using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using WashinPC.Engines.Operations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WashinPC.Core;
using WashinPC.Localization;
using WashinPC.Services;
using WashinPC.ViewModels;
using WashinPC.ViewModels.Common;

namespace WashinPC.Tests.Operations
{
    [TestClass]
    [DoNotParallelize]
    public class UiSafetyTests
    {
        [AssemblyInitialize] public static void IsolateSettings(TestContext context) { AppSettings.UseMemoryStore = true; }
        private sealed class OperationViewModel : BaseViewModel
        {
            internal Task Run(Func<CancellationToken, Task> action) => RunOperation(action);
        }
        [TestMethod] public async Task OperationGateBlocksOtherPageAndReleasesAfterCancellation()
        {
            var operation = new OperationViewModel(); var cleaner = new CleanerViewModel();
            var started = new TaskCompletionSource<bool>(); var release = new TaskCompletionSource<bool>();
            var task = operation.Run(async token => { started.SetResult(true); await release.Task; token.ThrowIfCancellationRequested(); });
            await started.Task;
            Assert.IsFalse(cleaner.ScanCommand.CanExecute(null));
            Assert.IsFalse(AppUiState.Instance.TryBegin());
            AppUiState.Instance.Cancel(); release.SetResult(true); await task;
            Assert.IsFalse(AppUiState.Instance.IsBusy); Assert.IsTrue(cleaner.ScanCommand.CanExecute(null));
        }
        [TestMethod] public void AllNewMessagesAreTranslatedAndNotCorrupted()
        {
            var keys = new[] { "op.cancel", "op.cancelled", "op.failed", "op.file_unavailable", "op.browser_busy", "op.changed", "op.applied", "op.backup_failed", "op.backup_or_write_failed", "op.machine_blocked", "op.diagnostic", "op.progress", "op.scan_result", "op.result", "op.backups_count", "op.delete_confirm", "op.privacy_confirm", "op.backups", "op.details", "privacy.chrome.history", "privacy.chrome.cookies", "privacy.edge.history", "privacy.edge.cookies" };
            foreach (var lang in new[] { Lang.RU, Lang.EN })
            {
                Localizer.Current = lang;
                foreach (var key in keys)
                {
                    var text = Localizer.T(key);
                    Assert.AreNotEqual(key, text); Assert.IsFalse(text.Contains("??"));
                    if (lang == Lang.RU) Assert.IsTrue(text.Any(c => c >= 'А' && c <= 'я'), key);
                }
            }
        }
        [TestMethod] public void PagesRenderAndRetainSelectionAcrossNavigationAndLanguage()
        {
            Exception failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    Application.ResourceAssembly = typeof(MainWindow).Assembly;
                    // Load the application's actual resources without scheduling its UAC/startup flow.
                    var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                    app.Resources["BooleanToVisibilityConverter"] = new BooleanToVisibilityConverter();
                    app.Resources["Font.App"] = new FontFamily("pack://application:,,,/WashinPC;component/Assets/Fonts/Inter/#Inter");
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/WashinPC;component/Styles/Theme.Light.xaml", UriKind.Relative) });
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/WashinPC;component/Styles/Controls.xaml", UriKind.Relative) });
                    var window = new MainWindow();
                    var host = (ContentControl)window.FindName("ContentHost");
                    NavigationService.Initialize(host);
                    NavigationService.Navigate(PageType.Cleaner);
                    var cleaner = (CleanerViewModel)((FrameworkElement)host.Content).DataContext;
                    var row = new SizeItemViewModel { Title = new LocalizedString("cleaner.user_temp"), TotalBytes = 1024, IsChecked = true };
                    cleaner.Items.Add(row);
                    foreach (PageType page in Enum.GetValues(typeof(PageType)))
                    {
                        NavigationService.Navigate(page);
                        foreach (var size in new[] { new Size(1280, 720), new Size(900, 560) })
                        {
                            var visual = (FrameworkElement)window.Content;
                            visual.Measure(size); visual.Arrange(new Rect(size)); visual.UpdateLayout(); Pump(); visual.UpdateLayout();
                            var bitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual);
                            var bytes = new byte[4]; bitmap.CopyPixels(new Int32Rect(20, 20, 1, 1), bytes, 4, 0); Assert.IsTrue(bytes[3] > 0);
                        }
                    }
                    Localizer.Current = Lang.RU; Localizer.Current = Lang.EN;
                    NavigationService.Navigate(PageType.Cleaner);
                    Assert.AreSame(cleaner, ((FrameworkElement)host.Content).DataContext);
                    Assert.IsTrue(row.IsChecked); Assert.IsTrue(cleaner.CleanCommand.CanExecute(null));
                    Assert.IsFalse(AppUiState.Instance.IsBusy);
                    if (Environment.GetEnvironmentVariable("WASHINPC_CAPTURE_PREVIEW") == "1") Capture(window, cleaner);
                    window.Close(); app.Shutdown();
                }
                catch (Exception ex) { failure = ex; }
            });
            thread.IsBackground = true; thread.SetApartmentState(ApartmentState.STA); thread.Start();
            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(30)), "WPF test timed out.");
            if (failure != null) throw new AssertFailedException("WPF render failed", failure);
        }
        private static void Capture(MainWindow window, CleanerViewModel cleaner)
        {
            var root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".."));
            if (!File.Exists(Path.Combine(root, "WashinPC.sln"))) throw new IOException("Capture must run in the source checkout.");
            var assets = Path.Combine(root, "docs", "assets"); Directory.CreateDirectory(assets);
            var groups = new[] { "cleaner.user_temp", "cleaner.windows_temp", "cleaner.prefetch", "cleaner.recent" };
            var sizes = new long[] { 33554432, 16777216, 2097152, 65536 };
            var plan = new FileScanPlan();
            for (var i = 0; i < groups.Length; i++) plan.Files.Add(new FileCandidate { Group = groups[i], Bytes = sizes[i], Path = "Demo data", Root = "Demo data" });
            cleaner.SetPlan(plan);
            foreach (var item in cleaner.Items) item.IsChecked = item.Title.Key != "cleaner.prefetch";
            var sidebar = (System.Windows.Controls.ListBox)window.FindName("Sidebar"); sidebar.SelectedIndex = 0;
            foreach (var lang in new[] { Lang.EN, Lang.RU })
            {
                Localizer.Current = lang;
                AppUiState.Instance.StatusText = lang == Lang.RU ? "Демонстрационные данные — файлы не удаляются." : "Demo data — no files are deleted.";
                var visual = (FrameworkElement)window.Content;
                visual.Measure(new Size(1280, 720)); visual.Arrange(new Rect(0, 0, 1280, 720)); visual.UpdateLayout(); Pump(); visual.UpdateLayout();
                var bitmap = new RenderTargetBitmap(1280, 720, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (var file = File.Create(Path.Combine(assets, "screenshot-" + lang.ToString().ToLowerInvariant() + ".png"))) encoder.Save(file);
            }
            File.Copy(Path.Combine(root, "src", "Assets", "WashinPC.png"), Path.Combine(assets, "logo.png"), true);
            // Repeated rendered cached navigation, without opening a window or changing real data.
            var samples = new List<string> { "sample,cached_navigation_ms" };
            for (var sample = 0; sample < 6; sample++)
            {
                var watch = Stopwatch.StartNew();
                for (var i = 0; i < 20; i++)
                {
                    NavigationService.Navigate(PageType.Privacy); ((FrameworkElement)window.Content).UpdateLayout();
                    NavigationService.Navigate(PageType.Cleaner); ((FrameworkElement)window.Content).UpdateLayout();
                }
                if (sample > 0) samples.Add(sample + "," + (watch.Elapsed.TotalMilliseconds / 40).ToString("F6", CultureInfo.InvariantCulture));
            }
            var measurement = Path.Combine(root, "artifacts", "measurements"); Directory.CreateDirectory(measurement);
            File.WriteAllLines(Path.Combine(measurement, "final-navigation.csv"), samples);
        }
        private static void Pump()
        {
            var frame = new System.Windows.Threading.DispatcherFrame();
            System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
            System.Windows.Threading.Dispatcher.PushFrame(frame);
        }
    }
}
