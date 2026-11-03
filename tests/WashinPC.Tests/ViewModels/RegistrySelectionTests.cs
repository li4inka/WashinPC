using Microsoft.VisualStudio.TestTools.UnitTesting;
using WashinPC.Engines.Registry;
using WashinPC.Localization;
using WashinPC.ViewModels;

namespace WashinPC.Tests.ViewModels
{
    [TestClass]
    public class RegistrySelectionTests
    {
        [TestInitialize]
        public void Init() => Localizer.Initialize();

        private static RegistryIssueView Issue(bool canFix) =>
            new RegistryIssueView(new RegistryIssue
            {
                CanFix = canFix,
                CategoryKey = canFix ? "registry.cat.help_files" : "registry.cat.startup",
                DetailsKey = "op.diagnostic",
                DetailsArgs = new object[0]
            });

        [TestMethod]
        public void HeaderSelectsOnlyRepairableItemsAndTracksIndividualChanges()
        {
            var vm = new RegistryViewModel();
            var first = Issue(true);
            var second = Issue(true);
            var diagnostic = Issue(false);
            vm.Items.Add(first);
            vm.Items.Add(second);
            vm.Items.Add(diagnostic);
            Assert.AreEqual(true, vm.AllSelectedState);
            Assert.IsTrue(vm.HasFixableItems);
            Assert.IsTrue(vm.HasDiagnosticItems);

            int notifications = 0;
            vm.FixCommand.CanExecuteChanged += (s, e) => notifications++;
            first.IsSelected = false;
            Assert.IsNull(vm.AllSelectedState);
            Assert.IsTrue(vm.FixCommand.CanExecute(null));

            vm.AllSelectedState = false;
            Assert.IsFalse(second.IsSelected);
            Assert.IsFalse(vm.FixCommand.CanExecute(null));
            vm.AllSelectedState = true;
            Assert.IsTrue(first.IsSelected);
            Assert.IsTrue(second.IsSelected);
            Assert.IsFalse(diagnostic.IsSelected);
            Assert.AreEqual(true, vm.AllSelectedState);
            Assert.IsTrue(vm.FixCommand.CanExecute(null));
            Assert.IsTrue(notifications > 0);
        }

        [TestMethod]
        public void DiagnosticOnlyAndEmptyResultsHaveNoSelection()
        {
            var vm = new RegistryViewModel();
            Assert.AreEqual(false, vm.AllSelectedState);
            vm.Items.Add(Issue(false));
            vm.AllSelectedState = true;
            Assert.AreEqual(false, vm.AllSelectedState);
            Assert.IsFalse(vm.HasFixableItems);
            Assert.IsFalse(vm.FixCommand.CanExecute(null));
            vm.Items.Clear();
            Assert.AreEqual(false, vm.AllSelectedState);
            Assert.IsFalse(vm.HasDiagnosticItems);
        }

        [TestMethod]
        public void ReplacingResultsDetachesOldSelectionAndUpdatesCommand()
        {
            var vm = new RegistryViewModel();
            var old = Issue(true);
            vm.Items.Add(old);
            vm.Items.Clear();
            var current = Issue(true);
            vm.Items.Add(current);
            int notifications = 0;
            vm.FixCommand.CanExecuteChanged += (s, e) => notifications++;
            old.IsSelected = false;
            Assert.AreEqual(0, notifications);
            Assert.AreEqual(true, vm.AllSelectedState);
            vm.Items.Remove(current);
            Assert.AreEqual(false, vm.AllSelectedState);
            Assert.IsFalse(vm.FixCommand.CanExecute(null));
            Assert.IsTrue(notifications > 0);
        }
    }
}
