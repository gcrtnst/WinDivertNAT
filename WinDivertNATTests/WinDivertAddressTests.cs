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

        [TestMethod]
        public void Loopback_GetSet_NoSideEffects()
        {
            var addr = new WinDivertAddress
            {
                Loopback = true,
            };
            Assert.AreEqual(false, addr.Sniffed);
            Assert.AreEqual(false, addr.Outbound);
            Assert.AreEqual(false, addr.Impostor);
            Assert.AreEqual(false, addr.IPv6);
            Assert.AreEqual(false, addr.IPChecksum);
            Assert.AreEqual(false, addr.TCPChecksum);
            Assert.AreEqual(false, addr.UDPChecksum);
        }
    }
}
