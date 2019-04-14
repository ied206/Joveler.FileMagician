using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Joveler.FileMagician.Tests
{
    [TestClass]
    public class FlagsTest
    {
        [TestMethod]
        public void GetSetFlags()
        {
            using (Magic magic = Magic.Open(TestSetup.MagicFile))
            {
                MagicFlags flags = magic.GetFlags();
                Assert.AreEqual(MagicFlags.NONE, flags);

                magic.SetFlags(MagicFlags.CONTINUE);
                flags = magic.GetFlags();
                Assert.AreEqual(MagicFlags.CONTINUE, flags);
            }
        }
    }
}
