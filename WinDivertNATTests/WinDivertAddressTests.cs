/*
 * WinDivertAddressTests.cs
 * Copyright gcrtnst
 *
 * This file is part of WinDivertNAT.
 *
 * WinDivertNAT is free software: you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by the
 * Free Software Foundation, either version 3 of the License, or (at your
 * option) any later version.
 *
 * WinDivertNAT is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with WinDivertNAT.  If not, see <http://www.gnu.org/licenses/>.
 *
 * WinDivertNAT is free software; you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by the Free
 * Software Foundation; either version 2 of the License, or (at your option)
 * any later version.
 * 
 * WinDivertNAT is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
 * for more details.
 * 
 * You should have received a copy of the GNU General Public License along
 * with WinDivertNAT; if not, write to the Free Software Foundation, Inc., 51
 * Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
 */

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
