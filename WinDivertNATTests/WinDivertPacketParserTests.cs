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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using WinDivertNAT;

namespace WinDivertNATTests
{
    [TestClass]
    public class WinDivertPacketParserTests
    {
        private readonly int port;
        private readonly Memory<byte> recv;

        public WinDivertPacketParserTests()
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
            using var enumerator = new WinDivertPacketParser(recv).GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsFalse(enumerator.MoveNext());
        }

        [TestMethod]
        public unsafe void MoveNext_Call_SetCurrent()
        {
            using var hmem = recv.Pin();
            var parseList = new List<WinDivertParseResult>(new WinDivertPacketParser(recv));
            Assert.AreEqual(3, parseList.Count);

            var packetOff1 = parseList[0].Packet.Length;
            var packetOff2 = packetOff1 + parseList[1].Packet.Length;
            Assert.IsTrue(recv.Span[..packetOff1] == parseList[0].Packet.Span);
            Assert.IsTrue(recv.Span[packetOff1..packetOff2] == parseList[1].Packet.Span);
            Assert.IsTrue(recv.Span[packetOff2..] == parseList[2].Packet.Span);

            var localhost = IPv4Addr.Parse("127.0.0.1");
            var protocol = (byte)17;
            Assert.IsTrue(parseList[0].IPv4Hdr != null);
            Assert.IsTrue(parseList[1].IPv4Hdr != null);
            Assert.IsTrue(parseList[2].IPv4Hdr != null);
            Assert.AreEqual(WinDivertHelper.Hton((ushort)parseList[0].Packet.Length), parseList[0].IPv4Hdr->Length);
            Assert.AreEqual(WinDivertHelper.Hton((ushort)parseList[1].Packet.Length), parseList[1].IPv4Hdr->Length);
            Assert.AreEqual(WinDivertHelper.Hton((ushort)parseList[2].Packet.Length), parseList[2].IPv4Hdr->Length);
            Assert.AreEqual(protocol, parseList[0].IPv4Hdr->Protocol);
            Assert.AreEqual(protocol, parseList[1].IPv4Hdr->Protocol);
            Assert.AreEqual(protocol, parseList[2].IPv4Hdr->Protocol);
            Assert.IsTrue(localhost == parseList[0].IPv4Hdr->SrcAddr);
            Assert.IsTrue(localhost == parseList[1].IPv4Hdr->SrcAddr);
            Assert.IsTrue(localhost == parseList[2].IPv4Hdr->SrcAddr);
            Assert.IsTrue(localhost == parseList[0].IPv4Hdr->DstAddr);
            Assert.IsTrue(localhost == parseList[1].IPv4Hdr->DstAddr);
            Assert.IsTrue(localhost == parseList[2].IPv4Hdr->DstAddr);

            Assert.IsTrue(parseList[0].IPv6Hdr == null);
            Assert.IsTrue(parseList[1].IPv6Hdr == null);
            Assert.IsTrue(parseList[2].IPv6Hdr == null);

            Assert.AreEqual(protocol, parseList[0].Protocol);
            Assert.AreEqual(protocol, parseList[1].Protocol);
            Assert.AreEqual(protocol, parseList[2].Protocol);

            Assert.IsTrue(parseList[0].ICMPv4Hdr == null);
            Assert.IsTrue(parseList[1].ICMPv4Hdr == null);
            Assert.IsTrue(parseList[2].ICMPv4Hdr == null);

            Assert.IsTrue(parseList[0].ICMPv6Hdr == null);
            Assert.IsTrue(parseList[1].ICMPv6Hdr == null);
            Assert.IsTrue(parseList[2].ICMPv6Hdr == null);

            Assert.IsTrue(parseList[0].TCPHdr == null);
            Assert.IsTrue(parseList[1].TCPHdr == null);
            Assert.IsTrue(parseList[2].TCPHdr == null);

            var nport = WinDivertHelper.Hton((ushort)port);
            Assert.IsTrue(parseList[0].UDPHdr != null);
            Assert.IsTrue(parseList[1].UDPHdr != null);
            Assert.IsTrue(parseList[2].UDPHdr != null);
            Assert.AreEqual(nport, parseList[0].UDPHdr->DstPort);
            Assert.AreEqual(nport, parseList[1].UDPHdr->DstPort);
            Assert.AreEqual(nport, parseList[2].UDPHdr->DstPort);
            Assert.AreEqual(WinDivertHelper.Hton((ushort)(parseList[0].Data.Length + 8)), parseList[0].UDPHdr->Length);
            Assert.AreEqual(WinDivertHelper.Hton((ushort)(parseList[1].Data.Length + 8)), parseList[1].UDPHdr->Length);
            Assert.AreEqual(WinDivertHelper.Hton((ushort)(parseList[2].Data.Length + 8)), parseList[2].UDPHdr->Length);

            Assert.IsTrue(parseList[0].Packet.Span[^parseList[0].Data.Length..] == parseList[0].Data.Span);
            Assert.IsTrue(parseList[1].Packet.Span[^parseList[1].Data.Length..] == parseList[1].Data.Span);
            Assert.IsTrue(parseList[2].Packet.Span[^parseList[2].Data.Length..] == parseList[2].Data.Span);
        }

        [TestMethod]
        public void Reset_Call_Reset()
        {
            using var enumerator = new WinDivertPacketParser(recv).GetEnumerator();
            _ = enumerator.MoveNext();
            _ = enumerator.MoveNext();
            _ = enumerator.MoveNext();
            enumerator.Reset();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsFalse(enumerator.MoveNext());
        }
    }
}
