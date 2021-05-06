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
using System.Net;
using System.Net.Sockets;
using System.Threading;
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
            using var handle = WinDivertLow.WinDivertOpen($"udp.DstPort == {port} and loopback", WinDivertConstants.WinDivertLayer.Network, 0, WinDivertConstants.WinDivertFlag.Drop | WinDivertConstants.WinDivertFlag.ReadOnly);
            using var udps = new UdpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port));
            udps.Client.ReceiveTimeout = 250;
            using var udpc = new UdpClient("127.0.0.1", port);
            _ = udpc.Send(new byte[1], 1);
            var remoteEP = new IPEndPoint(IPAddress.Any, 0);
            _ = Assert.ThrowsException<SocketException>(() => udps.Receive(ref remoteEP));
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
        public void WinDivertHelperCompileFilter_InvalidFilter_Throws()
        {
            var e = Assert.ThrowsException<WinDivertInvalidFilterException>(() => _ = WinDivertLow.WinDivertHelperCompileFilter("zero == invalid", WinDivertConstants.WinDivertLayer.Network));
            Assert.AreEqual("Filter expression contains a bad token", e.FilterErrorStr);
            Assert.AreEqual<uint>(8, e.FilterErrorPos);
        }
    }
}
