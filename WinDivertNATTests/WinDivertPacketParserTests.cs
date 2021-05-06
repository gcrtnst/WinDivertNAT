/*
 * WinDivertPacketParserTests.cs
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
    public class WinDivertPacketParserTests
    {
        private const int port = 52149;
        private readonly Memory<byte> recv = Packet();

        [TestMethod]
        public void MoveNext_Call_ReturnBool()
        {
            using var enumerator = new WinDivertPacketParser(recv).GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsFalse(enumerator.MoveNext());
        }

        private static Memory<byte> Packet()
        {
            var send = new byte[3] { 0, 1, 2 };
            var packet = new Memory<byte>(new byte[131072]);
            var abuf = (Span<WinDivertAddress>)stackalloc WinDivertAddress[127];
            using var divert = new WinDivert($"udp.DstPort == {port} and loopback", WinDivertConstants.WinDivertLayer.Network, 0, WinDivertConstants.WinDivertFlag.Sniff | WinDivertConstants.WinDivertFlag.RecvOnly);
            using var udps = new UdpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port));
            using var udpc = new UdpClient("127.0.0.1", port);
            _ = udpc.Send(send, 1);
            _ = udpc.Send(send, 2);
            _ = udpc.Send(send, 3);

            var recvOff = 0;
            var addrOff = 0;
            while (addrOff < 3)
            {
                var (recvLen, addrLen) = divert.RecvEx(packet.Span[recvOff..], abuf[addrOff..]);
                recvOff += (int)recvLen;
                addrOff += (int)addrLen;
            }
            return packet[0..recvOff];
        }
    }
}
