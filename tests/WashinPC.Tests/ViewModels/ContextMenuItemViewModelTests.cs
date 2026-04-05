using Microsoft.VisualStudio.TestTools.UnitTesting;
using WashinPC.Engines.ContextMenu;
using WashinPC.ViewModels;

namespace WashinPC.Tests.ViewModels
{
    [TestClass]
    public class ContextMenuItemViewModelTests
    {
        [TestMethod]
        public void Delete_And_Disable_AreMutuallyExclusive()
        {
            var vm = new ContextMenuItemViewModel(new ContextMenuEntry
            {
                Name = "test",
                Target = ContextMenuTarget.AnyFile,
                Kind = ContextMenuKind.ShellVerb,
                Scope = ContextMenuScope.CurrentUser,
                RelativeKeyPath = @"Software\Classes\*\shell\test",
                RegistryPath = @"Software\Classes\*\shell\test",
                Source = "HKCU",
                IsEnabled = true
            });

            vm.Disable = true;
            Assert.IsTrue(vm.Disable);
            Assert.IsFalse(vm.Delete);

            vm.Delete = true;
            Assert.IsTrue(vm.Delete);
            Assert.IsFalse(vm.Disable);
        }

        [TestMethod]
        public void Delete_RemainsFalse_WhenDeleteUnsupported()
        {
            var vm = new ContextMenuItemViewModel(new ContextMenuEntry
            {
                Name = "test",
                Target = ContextMenuTarget.AnyFile,
                Kind = ContextMenuKind.ShellVerb,
                Scope = ContextMenuScope.CurrentUser,
                RelativeKeyPath = null,
                RegistryPath = @"Software\Classes\*\shell\test",
                Source = "HKCU",
                IsEnabled = true
            });

            Assert.IsFalse(vm.IsDeleteSupported);
            vm.Delete = true;
            Assert.IsFalse(vm.Delete);
        }

        [TestMethod]
        public void Disable_ReflectsInitialEnabledState()
        {
            var disabledEntry = new ContextMenuEntry
            {
                Name = "disabled",
                Target = ContextMenuTarget.AnyFile,
                Kind = ContextMenuKind.ShellExHandler,
                Scope = ContextMenuScope.LocalMachine,
                RelativeKeyPath = @"Software\Classes\*\shellex\ContextMenuHandlers\test",
                RegistryPath = @"Software\Classes\*\shellex\ContextMenuHandlers\test",
                BlockedId = "{guid}",
                Source = "HKLM",
                IsEnabled = false
            };

            var vm = new ContextMenuItemViewModel(disabledEntry);
            Assert.IsTrue(vm.Disable);
            Assert.IsTrue(vm.IsToggleSupported);
        }
    }
}
