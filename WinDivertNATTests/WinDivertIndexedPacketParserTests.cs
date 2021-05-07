/*
 * WinDivertIndexedPacketParserTests.cs
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
using System;
using System.Net;
using System.Net.Sockets;
using WinDivertNAT;

namespace WinDivertNATTests
{
    [TestClass]
    public class WinDivertIndexedPacketParserTests
    {
        private readonly int port;
        private readonly Memory<byte> recv;

        public WinDivertIndexedPacketParserTests()
        {
            port = 52149;
            var send = new Memory<byte>(new byte[] { 0, 1, 2 });
            var packet = new Memory<byte>(new byte[131072]);
            var abuf = (Span<WinDivertAddress>)stackalloc WinDivertAddress[127];
            using var divert = new WinDivert($"udp.DstPort == {port} and loopback", WinDivertConstants.WinDivertLayer.Network, 0, WinDivertConstants.WinDivertFlag.Sniff | WinDivertConstants.WinDivertFlag.RecvOnly);
            using var udps = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
            using var udpc = new UdpClient("127.0.0.1", port);
            _ = udpc.Send(send.ToArray(), 1);
            _ = udpc.Send(send.ToArray(), 2);
            _ = udpc.Send(send.ToArray(), 3);

            var recvOff = 0;
            var addrOff = 0;
            while (addrOff < 3)
            {
                var (recvLen, addrLen) = divert.RecvEx(packet.Span[recvOff..], abuf[addrOff..]);
                recvOff += (int)recvLen;
                addrOff += (int)addrLen;
            }
            recv = packet[0..recvOff];
        }

        [TestMethod]
        public void MoveNext_Call_ReturnBool()
        {
            using var enumerator = new WinDivertIndexedPacketParser(recv).GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsFalse(enumerator.MoveNext());
        }

        [TestMethod]
        public unsafe void MoveNext_Call_SetCurrent()
        {
            using var enumerator = new WinDivertIndexedPacketParser(recv).GetEnumerator();
            _ = enumerator.MoveNext();
            Assert.AreEqual(0, enumerator.Current.Item1);
            Assert.AreEqual<byte>(17, enumerator.Current.Item2.Protocol);
            _ = enumerator.MoveNext();
            Assert.AreEqual(1, enumerator.Current.Item1);
            _ = enumerator.MoveNext();
            Assert.AreEqual(2, enumerator.Current.Item1);
        }

        [TestMethod]
        public void Reset_Call_Reset()
        {
            using var enumerator = new WinDivertIndexedPacketParser(recv).GetEnumerator();
            _ = enumerator.MoveNext();
            _ = enumerator.MoveNext();
            _ = enumerator.MoveNext();
            enumerator.Reset();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(0, enumerator.Current.Item1);
            Assert.AreEqual<byte>(17, enumerator.Current.Item2.Protocol);
        }
    }
}
