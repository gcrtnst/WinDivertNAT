﻿/*
 * WinDivertTests.cs
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
using System.Reflection;
using WinDivertNAT;

namespace WinDivertNATTests
{
    [TestClass]
    public class WinDivertTests
    {
        private const int port1 = 52149;
        private const int port2 = 52150;

        [TestMethod]
        public void Ctor_ValidArguments()
        {
            using var sender = new UdpClient(new IPEndPoint(IPAddress.Loopback, port1));
            sender.Connect(IPAddress.Loopback, port2);

            using var receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, port2));
            receiver.Connect(IPAddress.Loopback, port1);
            receiver.Client.ReceiveTimeout = 250;

            var remoteEP = new IPEndPoint(IPAddress.Any, 0);
            _ = sender.Send(new byte[1], 1);
            _ = receiver.Receive(ref remoteEP);

            using var divert = new WinDivert($"udp.SrcPort == {port1} and udp.DstPort == {port2} and loopback", WinDivert.Layer.Network, 0, WinDivert.Flag.Drop | WinDivert.Flag.RecvOnly);
            _ = sender.Send(new byte[1], 1);
            var e = Assert.ThrowsException<SocketException>(() => receiver.Receive(ref remoteEP));
            Assert.AreEqual(SocketError.TimedOut, e.SocketErrorCode);
        }

        [TestMethod]
        [DataRow("false", WinDivert.Layer.Network, (short)32767, (WinDivert.Flag)0)]
        public void Ctor_InvalidArguments(string filter, WinDivert.Layer layer, short priority, WinDivert.Flag flag)
        {
            var fobj = WinDivert.CompileFilter(filter, layer);
            var e = Assert.ThrowsException<Win32Exception>(() => new WinDivert(fobj.Span, layer, priority, flag));
            Assert.AreEqual(87, e.NativeErrorCode);
        }

        [TestMethod]
        public void Ctor_EmptyFilter() => Assert.ThrowsException<ArgumentException>(() => new WinDivert(Span<byte>.Empty, WinDivert.Layer.Network, 0, 0));

        [TestMethod]
        [Timeout(250)]
        public void RecvEx_BufferProvided()
        {
            var packet = (Span<byte>)stackalloc byte[0xFFFF];
            var abuf = (Span<WinDivertAddress>)stackalloc WinDivertAddress[1];

            using var sender = new UdpClient(new IPEndPoint(IPAddress.Loopback, port1));
            sender.Connect(IPAddress.Loopback, port2);

            using var receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, port2));
            receiver.Connect(IPAddress.Loopback, port1);

            using var divert = new WinDivert($"udp.SrcPort == {port1} and udp.DstPort == {port2} and loopback", WinDivert.Layer.Network, 0, WinDivert.Flag.Sniff | WinDivert.Flag.RecvOnly);
            _ = sender.Send(new byte[1], 1);
            (var recvLen, var addrLen) = divert.RecvEx(packet, abuf);

            Assert.IsTrue(recvLen > 0);
            Assert.AreEqual((uint)1, addrLen);
            Assert.AreEqual((byte)0x45, packet[0]);
            Assert.IsTrue(abuf[0].Sniffed);
        }

        [TestMethod]
        [Timeout(250)]
        public void RecvEx_OnlyPacketBufferProvided()
        {
            var packet = (Span<byte>)stackalloc byte[0xFFFF];

            using var sender = new UdpClient(new IPEndPoint(IPAddress.Loopback, port1));
            sender.Connect(IPAddress.Loopback, port2);

            using var receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, port2));
            receiver.Connect(IPAddress.Loopback, port1);

            using var divert = new WinDivert($"udp.SrcPort == {port1} and udp.DstPort == {port2} and loopback", WinDivert.Layer.Network, 0, WinDivert.Flag.Sniff | WinDivert.Flag.RecvOnly);
            _ = sender.Send(new byte[1], 1);
            (var recvLen, var addrLen) = divert.RecvEx(packet, Span<WinDivertAddress>.Empty);

            Assert.IsTrue(recvLen > 0);
            Assert.AreEqual((uint)0, addrLen);
            Assert.AreEqual((byte)0x45, packet[0]);
        }

        [TestMethod]
        [Timeout(250)]
        public void RecvEx_InsufficientBufferProvided()
        {
            var packet = new Memory<byte>(new byte[1]);
            var abuf = new Memory<WinDivertAddress>(new WinDivertAddress[1]);

            using var sender = new UdpClient(new IPEndPoint(IPAddress.Loopback, port1));
            sender.Connect(IPAddress.Loopback, port2);

            using var receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, port2));
            receiver.Connect(IPAddress.Loopback, port1);

            using var divert = new WinDivert($"udp.SrcPort == {port1} and udp.DstPort == {port2} and loopback", WinDivert.Layer.Network, 0, WinDivert.Flag.Sniff | WinDivert.Flag.RecvOnly);
            _ = sender.Send(new byte[1], 1);
            var e = Assert.ThrowsException<Win32Exception>(() => divert.RecvEx(packet.Span, abuf.Span));
            Assert.AreEqual(122, e.NativeErrorCode);
        }

        [TestMethod]
        [Timeout(250)]
        public void RecvEx_OnlyAddressBufferProvided()
        {
            var abuf = (Span<WinDivertAddress>)stackalloc WinDivertAddress[1];
            using var divert = new WinDivert($"localPort == {port1} and localAddr == 127.0.0.1 and event == BIND", WinDivert.Layer.Socket, 0, WinDivert.Flag.Sniff | WinDivert.Flag.RecvOnly);
            using var sender = new UdpClient(new IPEndPoint(IPAddress.Loopback, port1));
            (var recvLen, var addrLen) = divert.RecvEx(Span<byte>.Empty, abuf);

            Assert.AreEqual((uint)0, recvLen);
            Assert.AreEqual((uint)1, addrLen);
            Assert.AreEqual(WinDivert.Event.SocketBind, abuf[0].Event);
        }

        [TestMethod]
        [Timeout(250)]
        public void RecvEx_AlreadyShutdown()
        {
            var packet = new Memory<byte>(new byte[0xFFFF]);
            var abuf = new Memory<WinDivertAddress>(new WinDivertAddress[1]);

            using var divert = new WinDivert("false", WinDivert.Layer.Network, 0, WinDivert.Flag.Sniff | WinDivert.Flag.RecvOnly);
            divert.Shutdown();
            var e = Assert.ThrowsException<Win32Exception>(() => divert.RecvEx(packet.Span, abuf.Span));
            Assert.AreEqual(232, e.NativeErrorCode);
        }

        [TestMethod]
        [Timeout(1000)]
        public void SendEx_BufferProvided()
        {
            var packet = (Span<byte>)stackalloc byte[0xFFFF];
            var abuf = (Span<WinDivertAddress>)stackalloc WinDivertAddress[1];

            using var sender = new UdpClient(new IPEndPoint(IPAddress.Loopback, port1));
            sender.Connect(IPAddress.Loopback, port2);

            using var receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, port2));
            receiver.Connect(IPAddress.Loopback, port1);
            receiver.Client.ReceiveTimeout = 250;

            using var divert = new WinDivert($"udp.SrcPort == {port1} and udp.DstPort == {port2} and loopback", WinDivert.Layer.Network, 0, 0);
            var remoteEP = new IPEndPoint(IPAddress.Any, 0);
            _ = sender.Send(new byte[1], 1);
            var e = Assert.ThrowsException<SocketException>(() => receiver.Receive(ref remoteEP));
            Assert.AreEqual(SocketError.TimedOut, e.SocketErrorCode);

            (var recvLen, var addrLen) = divert.RecvEx(packet, abuf);
            _ = divert.SendEx(packet[..(int)recvLen], abuf[..(int)addrLen]);
            _ = receiver.Receive(ref remoteEP);
        }

        [TestMethod]
        [Timeout(250)]
        public unsafe void SendEx_DeadPacketProvided()
        {
            var packet = new Memory<byte>(new byte[0xFFFF]);
            var abuf = new Memory<WinDivertAddress>(new WinDivertAddress[1]);

            using var sender = new UdpClient(new IPEndPoint(IPAddress.Loopback, port1));
            sender.Connect(IPAddress.Loopback, port2);

            using var receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, port2));
            receiver.Connect(IPAddress.Loopback, port1);

            using var divert = new WinDivert($"udp.SrcPort == {port1} and udp.DstPort == {port2} and loopback", WinDivert.Layer.Network, 0, WinDivert.Flag.Sniff);
            _ = sender.Send(new byte[1], 1);
            (var recvLen, var addrLen) = divert.RecvEx(packet.Span, abuf.Span);
            var recv = packet[..(int)recvLen];
            var addr = abuf[..(int)addrLen];
            foreach (var (i, p) in new WinDivertIndexedPacketParser(recv))
            {
                p.IPv4Hdr->TTL = 0;
                addr.Span[i].Impostor = true;
            }

            var e = Assert.ThrowsException<Win32Exception>(() => divert.SendEx(recv.Span, addr.Span));
            Assert.AreEqual(1232, e.NativeErrorCode);
        }

        [TestMethod]
        [DataRow(nameof(WinDivert.QueueLength), (ulong)4096)]
        [DataRow(nameof(WinDivert.QueueTime), (ulong)2000)]
        [DataRow(nameof(WinDivert.QueueSize), (ulong)4194304)]
        [DataRow(nameof(WinDivert.VersionMajor), (ulong)2)]
        [DataRow(nameof(WinDivert.VersionMinor), (ulong)2)]
        public void Param_Get(string name, ulong expected)
        {
            using var divert = new WinDivert("false", WinDivert.Layer.Network, 0, WinDivert.Flag.Sniff | WinDivert.Flag.RecvOnly);
            var param = divert.GetType().GetProperty(name)!;
            var actual = (ulong)param.GetValue(divert)!;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [DataRow(nameof(WinDivert.QueueLength), (ulong)32)]
        [DataRow(nameof(WinDivert.QueueLength), (ulong)16384)]
        [DataRow(nameof(WinDivert.QueueTime), (ulong)100)]
        [DataRow(nameof(WinDivert.QueueTime), (ulong)16000)]
        [DataRow(nameof(WinDivert.QueueSize), (ulong)65535)]
        [DataRow(nameof(WinDivert.QueueSize), (ulong)33554432)]
        public void Param_SetValidValue(string name, ulong input)
        {
            using var divert = new WinDivert("false", WinDivert.Layer.Network, 0, WinDivert.Flag.Sniff | WinDivert.Flag.RecvOnly);
            var param = divert.GetType().GetProperty(name)!;

            var initial = (ulong)param.GetValue(divert)!;
            Assert.AreNotEqual(input, initial);

            param.SetValue(divert, input);
            var actual = (ulong)param.GetValue(divert)!;
            Assert.AreEqual(input, actual);
        }

        [TestMethod]
        [DataRow(nameof(WinDivert.QueueLength), (ulong)0)]
        [DataRow(nameof(WinDivert.QueueTime), (ulong)0)]
        [DataRow(nameof(WinDivert.QueueSize), (ulong)0)]
        public void Param_SetInvalidValue(string name, ulong input)
        {
            using var divert = new WinDivert("false", WinDivert.Layer.Network, 0, WinDivert.Flag.Sniff | WinDivert.Flag.RecvOnly);
            var param = divert.GetType().GetProperty(name)!;
            var invokeExc = Assert.ThrowsException<TargetInvocationException>(() => param.SetValue(divert, input));
            var win32Exc = (Win32Exception)invokeExc.InnerException!;
            Assert.AreEqual(87, win32Exc.NativeErrorCode);
        }

        [TestMethod]
        public void Shutdown()
        {
            using var sender = new UdpClient(new IPEndPoint(IPAddress.Loopback, port1));
            sender.Connect(IPAddress.Loopback, port2);

            using var receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, port2));
            receiver.Connect(IPAddress.Loopback, port1);
            receiver.Client.ReceiveTimeout = 250;

            using var divert = new WinDivert($"udp.SrcPort == {port1} and udp.DstPort == {port2} and loopback", WinDivert.Layer.Network, 0, WinDivert.Flag.Drop | WinDivert.Flag.ReadOnly);
            var remoteEP = new IPEndPoint(IPAddress.Any, 0);
            _ = sender.Send(new byte[1], 1);
            var e = Assert.ThrowsException<SocketException>(() => receiver.Receive(ref remoteEP));
            Assert.AreEqual(SocketError.TimedOut, e.SocketErrorCode);

            divert.Shutdown();
            _ = sender.Send(new byte[1], 1);
            _ = receiver.Receive(ref remoteEP);
        }

        [TestMethod]
        public void CalcChecksums_ValidPacket()
        {
            var packet = (Span<byte>)stackalloc byte[28]
            {
                0x45,
                0x00,
                0x00,
                0x1c,
                0x01,
                0x23,
                0x00,
                0x00,
                0x80,
                0x11,
                0x00,
                0x00,
                0x7f,
                0x00,
                0x00,
                0x01,
                0x7f,
                0x00,
                0x00,
                0x01,
                0x00,
                0x07,
                0x00,
                0x07,
                0x00,
                0x08,
                0x00,
                0x00,
            };
            var addr = new WinDivertAddress()
            {
                Layer = WinDivert.Layer.Network,
                Event = WinDivert.Event.NetworkPacket,
                Loopback = true,
            };
            WinDivert.CalcChecksums(packet, ref addr, 0);
            Assert.AreEqual((byte)0x3b, packet[10]);
            Assert.AreEqual((byte)0xac, packet[11]);
            Assert.AreEqual((byte)0x01, packet[26]);
            Assert.AreEqual((byte)0xce, packet[27]);
            Assert.AreEqual(true, addr.IPChecksum);
            Assert.AreEqual(false, addr.TCPChecksum);
            Assert.AreEqual(true, addr.UDPChecksum);
        }

        [TestMethod]
        public void CalcChecksums_EmptyPacket()
        {
            var packet = Memory<byte>.Empty;
            var addr = new WinDivertAddress()
            {
                Layer = WinDivert.Layer.Network,
                Event = WinDivert.Event.NetworkPacket,
            };
            _ = Assert.ThrowsException<ArgumentException>(() => WinDivert.CalcChecksums(packet.Span, ref addr, 0));
        }

        [TestMethod]
        [DataRow("outbound and !loopback and (tcp.DstPort == 80 or udp.DstPort == 53)", WinDivert.Layer.Network)]
        [DataRow("inbound and tcp.Syn", WinDivert.Layer.Network)]
        [DataRow("true", WinDivert.Layer.Network)]
        [DataRow("false", WinDivert.Layer.Network)]
        public void CompileFilter_ValidFilter(string filter, WinDivert.Layer layer)
        {
            var fobj = WinDivert.CompileFilter(filter, layer);
            Assert.AreEqual(fobj.Length, fobj.Span.IndexOf((byte)0) + 1);
        }

        [TestMethod]
        [DataRow("", WinDivert.Layer.Network, "Filter expression parse error", (uint)0)]
        [DataRow("invalid", WinDivert.Layer.Network, "Filter expression contains a bad token", (uint)0)]
        [DataRow("zero == invalid", WinDivert.Layer.Network, "Filter expression contains a bad token", (uint)8)]
        public void CompileFilter_InvalidFilter(string filter, WinDivert.Layer layer, string errorStr, uint errorPos)
        {
            var e = Assert.ThrowsException<WinDivertInvalidFilterException>(() => WinDivert.CompileFilter(filter, layer));
            Assert.AreEqual(errorStr, e.FilterErrorStr);
            Assert.AreEqual(errorPos, e.FilterErrorPos);
        }
    }
}
