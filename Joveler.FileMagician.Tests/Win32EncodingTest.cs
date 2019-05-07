using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Joveler.FileMagician.Tests
{
    [TestClass]
    public class Win32EncodingTest
    {
        [TestMethod]
        public void IsActiveCodePageCompatible()
        {
            // Assumption : No known non-unicode encoding support Korean, non-ASCII latin, Chinese characters at once.
            const string compatStr = "123";
            const string incompatStr = "ᄒᆞᆫ글ḀḘ韓國";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            { // Call WideCharToMultiByte to test if a string is compatible with active code page
                Assert.IsTrue(Win32Encoding.IsActiveCodePageCompatible(compatStr));
                Assert.IsFalse(Win32Encoding.IsActiveCodePageCompatible(incompatStr));
            }
            else
            { // Always report true in non-Windows platforms
                Assert.IsTrue(Win32Encoding.IsActiveCodePageCompatible(compatStr));
                Assert.IsTrue(Win32Encoding.IsActiveCodePageCompatible(incompatStr));
            }
        }
    }
}
