using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinDivertNAT;

namespace WinDivertNATTests
{
    [TestClass]
    public class WinDivertAddressTests
    {
        [TestMethod]
        public void Loopback_GetSet_Succeeds()
        {
            var addr = new WinDivertAddress();
            Assert.AreEqual(false, addr.Loopback);
            addr.Loopback = true;
            Assert.AreEqual(true, addr.Loopback);
            addr.Loopback = false;
            Assert.AreEqual(false, addr.Loopback);
        }
    }
}
