using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Joveler.FileMagician.Tests
{
    [TestClass]
    public class VersionTest
    {
        [TestMethod]
        public void Version()
        {
            int val = Magic.VersionInt();
            Assert.AreEqual(536, val);

            Version ver = Magic.VersionInstance();
            Assert.AreEqual(5, ver.Major);
            Assert.AreEqual(36, ver.Minor);
        }
    }
}
