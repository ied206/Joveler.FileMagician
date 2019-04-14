using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Joveler.FileMagician.Tests
{
    [TestClass]
    public class GetPathTest
    {
        [TestMethod]
        public void GetPath()
        {
            string result = Magic.GetPath(null, true);
            Assert.IsNull(result);
            // Magic.GetPath(null, false)'s value changes if MSYS2 is installed, which is required for compiling libmagic.

            result = Magic.GetPath(TestSetup.MagicFile, true);
            Assert.IsTrue(result.Equals(TestSetup.MagicFile, StringComparison.Ordinal));
            result = Magic.GetPath(TestSetup.MagicFile, false);
            Assert.IsTrue(result.Equals(TestSetup.MagicFile, StringComparison.Ordinal));
        }
    }
}
