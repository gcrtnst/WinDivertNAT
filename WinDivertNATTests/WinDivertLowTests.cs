/*
 * WinDivertLowTests.cs
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
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using WinDivertNAT;

namespace WinDivertNATTests
{
    [TestClass]
    public class WinDivertLowTests
    {
        [TestMethod]
        public void WinDivertOpen_ValidArguments_DropPacket()
        {
            const int port = 52149;
            var remoteEP = new IPEndPoint(IPAddress.Any, 0);
            using var udps = new UdpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port));
            udps.Client.ReceiveTimeout = 250;
            using var udpc = new UdpClient("127.0.0.1", port);

            _ = udpc.Send(new byte[1], 1);
            _ = udps.Receive(ref remoteEP);

            using var handle = WinDivertLow.WinDivertOpen($"udp.DstPort == {port} and loopback", WinDivertConstants.WinDivertLayer.Network, 0, WinDivertConstants.WinDivertFlag.Drop | WinDivertConstants.WinDivertFlag.ReadOnly);
            _ = udpc.Send(new byte[1], 1);
            var e = Assert.ThrowsException<SocketException>(() => udps.Receive(ref remoteEP));
            Assert.AreEqual(SocketError.TimedOut, e.SocketErrorCode);
        }

        [TestMethod]
        public void WinDivertOpen_InvalidArguments_Throw()
        {
            var filter = WinDivertLow.WinDivertHelperCompileFilter("zero", WinDivertConstants.WinDivertLayer.Network);
            _ = Assert.ThrowsException<ArgumentException>(() => WinDivertLow.WinDivertOpen(filter.Span, WinDivertConstants.WinDivertLayer.Network, 32767, 0));
        }

        [TestMethod]
        public void WinDivertRecvEx_BufferProvided_RecvPacket()
        {
            var packet = (Span<byte>)stackalloc byte[131072];
            var abuf = (Span<WinDivertAddress>)stackalloc WinDivertAddress[127];
            using var handle = WinDivertLow.WinDivertOpen("true", WinDivertConstants.WinDivertLayer.Network, 0, WinDivertConstants.WinDivertFlag.Sniff | WinDivertConstants.WinDivertFlag.RecvOnly);
            var (recvLen, addrLen) = WinDivertLow.WinDivertRecvEx(handle, packet, abuf);
            Assert.IsTrue(recvLen > 0);
            Assert.IsTrue(packet[0] != 0);
            Assert.IsTrue(addrLen > 0);
            Assert.IsTrue(abuf[0].Sniffed);
        }

        [TestMethod]
        public void WinDivertRecvEx_NoAddressBuffer_RecvPacket()
        {
            var packet = (Span<byte>)stackalloc byte[131072];
            using var handle = WinDivertLow.WinDivertOpen("true", WinDivertConstants.WinDivertLayer.Network, 0, WinDivertConstants.WinDivertFlag.Sniff | WinDivertConstants.WinDivertFlag.RecvOnly);
            var (recvLen, addrLen) = WinDivertLow.WinDivertRecvEx(handle, packet, Span<WinDivertAddress>.Empty);
            Assert.IsTrue(recvLen > 0);
            Assert.IsTrue(packet[0] != 0);
            Assert.AreEqual<uint>(0, addrLen);
        }

        [TestMethod]
        public void WinDivertRecvEx_InsufficientBuffer_Throw()
        {
            var packet = new Memory<byte>(new byte[1]);
            using var handle = WinDivertLow.WinDivertOpen("true", WinDivertConstants.WinDivertLayer.Network, 0, WinDivertConstants.WinDivertFlag.Sniff | WinDivertConstants.WinDivertFlag.RecvOnly);
            var e = Assert.ThrowsException<Win32Exception>(() => WinDivertLow.WinDivertRecvEx(handle, packet.Span, Span<WinDivertAddress>.Empty));
            Assert.AreEqual(122, e.NativeErrorCode);
        }

        [TestMethod]
        public void WinDivertSendEx_BufferProvided_SendPacket()
        {
            const int port = 52149;
            var packet = (Span<byte>)stackalloc byte[131072];
            var abuf = (Span<WinDivertAddress>)stackalloc WinDivertAddress[127];
            using var handle = WinDivertLow.WinDivertOpen($"udp.DstPort == {port} and loopback", WinDivertConstants.WinDivertLayer.Network, 0, 0);
            using var udps = new UdpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port));
            udps.Client.ReceiveTimeout = 250;
            using var udpc = new UdpClient("127.0.0.1", port);
            _ = udpc.Send(new byte[1], 1);
            var (recvLen, addrLen) = WinDivertLow.WinDivertRecvEx(handle, packet, abuf);
            _ = WinDivertLow.WinDivertSendEx(handle, packet[0..(int)recvLen], abuf[0..(int)addrLen]);
            var remoteEP = new IPEndPoint(IPAddress.Any, 0);
            _ = udps.Receive(ref remoteEP);
        }

        [TestMethod]
        public unsafe void WinDivertSendEx_DeadPacket_Throw()
        {
            var packet = new Memory<byte>(new byte[131072]);
            var abuf = new Memory<WinDivertAddress>(new WinDivertAddress[127]);
            using var handle = WinDivertLow.WinDivertOpen("true", WinDivertConstants.WinDivertLayer.Network, 0, WinDivertConstants.WinDivertFlag.Sniff);
            var (recvLen, addrLen) = WinDivertLow.WinDivertRecvEx(handle, packet.Span, abuf.Span);
            var recv = packet[0..(int)recvLen];
            var addr = abuf[0..(int)addrLen];
            foreach (var parse in new WinDivertPacketParser(packet))
            {
                if (parse.IPv4Hdr != null) parse.IPv4Hdr->TTL = 0;
                if (parse.IPv6Hdr != null) parse.IPv6Hdr->HopLimit = 0;
            }
            foreach (ref var a in addr.Span) a.Impostor = true;
            var e = Assert.ThrowsException<Win32Exception>(() => WinDivertLow.WinDivertSendEx(handle, recv.Span, addr.Span));
            Assert.AreEqual(1232, e.NativeErrorCode);
        }

        [TestMethod]
        public void WinDivertHelperParseIPv4Address_ValidAddress_RoundTrip()
        {
            var input = "127.0.0.1";
            var addr = WinDivertLow.WinDivertHelperParseIPv4Address(input);
            var output = WinDivertLow.WinDivertHelperFormatIPv4Address(addr);
            Assert.AreEqual(input, output);
        }

        [TestMethod]
        public void WinDivertHelperParseIPv6Address_ValidAddress_RoundTrip()
        {
            var input = "2001:db8:85a3::8a2e:370:7334";
            var addr = WinDivertLow.WinDivertHelperParseIPv6Address(input);
            var output = WinDivertLow.WinDivertHelperFormatIPv6Address(addr);
            Assert.AreEqual(input, output);
        }

        [TestMethod]
        public void WinDivertHelperCompileFilter_ValidFilter_NoException()
        {
            var filter = WinDivertLow.WinDivertHelperCompileFilter("false", WinDivertConstants.WinDivertLayer.Network);
            using var handle = WinDivertLow.WinDivertOpen(filter.Span, WinDivertConstants.WinDivertLayer.Network, 0, WinDivertConstants.WinDivertFlag.Sniff | WinDivertConstants.WinDivertFlag.RecvOnly);
        }

        [TestMethod]
        public void WinDivertHelperCompileFilter_InvalidFilter_Throw()
        {
            var e = Assert.ThrowsException<WinDivertInvalidFilterException>(() => _ = WinDivertLow.WinDivertHelperCompileFilter("zero == invalid", WinDivertConstants.WinDivertLayer.Network));
            Assert.AreEqual("Filter expression contains a bad token", e.FilterErrorStr);
            Assert.AreEqual<uint>(8, e.FilterErrorPos);
        }
    }
}
