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
        private const int port1 = 52149;
        private const int port2 = 52150;
        private readonly Memory<byte> recv;

        public WinDivertIndexedPacketParserTests()
        {
            var send = new byte[] { 0, 1, 2 };
            var packet = new Memory<byte>(new byte[0xFF * 3]);
            var abuf = (Span<WinDivertAddress>)stackalloc WinDivertAddress[3];

            using var sender = new UdpClient(new IPEndPoint(IPAddress.Loopback, port1));
            sender.Connect(IPAddress.Loopback, port2);

            using var receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, port2));
            receiver.Connect(IPAddress.Loopback, port1);

            using var divert = new WinDivert($"udp.SrcPort == {port1} and udp.DstPort == {port2} and loopback", WinDivert.Layer.Network, 0, WinDivert.Flag.Sniff | WinDivert.Flag.RecvOnly);
            _ = sender.Send(send, 1);
            _ = sender.Send(send, 2);
            _ = sender.Send(send, 3);

            var recvOff = 0;
            var addrOff = 0;
            while (addrOff < 3)
            {
                var (recvLen, addrLen) = divert.RecvEx(packet.Span[recvOff..], abuf[addrOff..]);
                recvOff += (int)recvLen;
                addrOff += (int)addrLen;
            }
            recv = packet[..recvOff];
        }

        [TestMethod]
        public void MoveNext()
        {
            using var enumerator = new WinDivertIndexedPacketParser(recv).GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(0, enumerator.Current.Item1);
            Assert.AreEqual<byte>(17, enumerator.Current.Item2.Protocol);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(1, enumerator.Current.Item1);
            Assert.AreEqual<byte>(17, enumerator.Current.Item2.Protocol);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(2, enumerator.Current.Item1);
            Assert.AreEqual<byte>(17, enumerator.Current.Item2.Protocol);
            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsFalse(enumerator.MoveNext());
        }

        [TestMethod]
        public void Reset()
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
