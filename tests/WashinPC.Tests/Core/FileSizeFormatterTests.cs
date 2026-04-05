using Microsoft.VisualStudio.TestTools.UnitTesting;
using WashinPC.Core.Utils;

namespace WashinPC.Tests.Core
{
    [TestClass]
    public class FileSizeFormatterTests
    {
        [TestMethod]
        public void Format_Bytes_UsesByteUnit()
        {
            Assert.AreEqual("999 B", FileSizeFormatter.Format(999));
        }

        [TestMethod]
        public void Format_Kilobytes_UsesKbUnit()
        {
            Assert.AreEqual("1 KB", FileSizeFormatter.Format(1024));
        }

        [TestMethod]
        public void Format_Megabytes_UsesMbUnit()
        {
            Assert.AreEqual("1 MB", FileSizeFormatter.Format(1024 * 1024));
        }
    }
}
