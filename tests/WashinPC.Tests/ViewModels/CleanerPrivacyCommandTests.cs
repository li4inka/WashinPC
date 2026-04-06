using Microsoft.VisualStudio.TestTools.UnitTesting;
using WashinPC.Localization;
using WashinPC.ViewModels;
using WashinPC.ViewModels.Common;

namespace WashinPC.Tests.ViewModels
{
    [TestClass]
    public class CleanerPrivacyCommandTests
    {
        [TestInitialize]
        public void Init()
        {
            Localizer.Initialize();
        }

        [TestMethod]
        public void Cleaner_CleanCommand_RaisesCanExecuteChanged_WhenItemCheckedChanges()
        {
            var vm = new CleanerViewModel();
            var item = new SizeItemViewModel
            {
                Title = new LocalizedString("cleaner.user_temp"),
                IsChecked = false
            };

            int raised = 0;
            vm.CleanCommand.CanExecuteChanged += (s, e) => raised++;

            vm.Items.Add(item);
            item.IsChecked = true;

            Assert.IsTrue(raised > 0, "Clean command should notify UI after checkbox changes.");
            Assert.IsTrue(vm.CleanCommand.CanExecute(null));
        }

        [TestMethod]
        public void Privacy_CleanCommand_RaisesCanExecuteChanged_WhenItemCheckedChanges()
        {
            var vm = new PrivacyViewModel();
            var item = new SizeItemViewModel
            {
                Title = new LocalizedString("privacy.chrome"),
                IsChecked = false
            };

            int raised = 0;
            vm.CleanCommand.CanExecuteChanged += (s, e) => raised++;

            vm.Items.Add(item);
            item.IsChecked = true;

            Assert.IsTrue(raised > 0, "Clean command should notify UI after checkbox changes.");
            Assert.IsTrue(vm.CleanCommand.CanExecute(null));
        }
    }
}
