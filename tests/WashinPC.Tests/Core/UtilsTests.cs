using Microsoft.VisualStudio.TestTools.UnitTesting;
using WashinPC.Core.Utils;

namespace WashinPC.Tests.Core
{
    [TestClass]
    public class UtilsTests
    {
        [TestMethod]
        public void SplitLines_RemovesEmptyLines_AndNormalizesCrlf()
        {
            var input = "line1\r\n\r\nline2\nline3\r\n";
            var lines = Utils.SplitLines(input);

            CollectionAssert.AreEqual(
                new[] { "line1", "line2", "line3" },
                lines);
        }
    }
}
