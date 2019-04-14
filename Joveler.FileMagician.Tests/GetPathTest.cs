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
            // Magic.GetPath(null, true)'s value depends on OS.
            // Magic.GetPath(null, false)'s value changes if MSYS2 is installed, which is required for compiling libmagic.
            string result = Magic.GetPath(TestSetup.MagicFile, true);
            Assert.IsTrue(result.Equals(TestSetup.MagicFile, StringComparison.Ordinal));
            result = Magic.GetPath(TestSetup.MagicFile, false);
            Assert.IsTrue(result.Equals(TestSetup.MagicFile, StringComparison.Ordinal));
        }
    }
}
