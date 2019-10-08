using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Joveler.FileMagician.Tests
{
    [TestClass]
    public class VersionTest
    {
        [TestMethod]
        public void Version()
        {
            int val = Magic.VersionInt();
            Assert.AreEqual(537, val);

            Version ver = Magic.VersionInstance();
            Assert.AreEqual(5, ver.Major);
            Assert.AreEqual(37, ver.Minor);
        }
    }
}
