﻿/*
 * WinDivert.cs
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

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;

namespace WinDivertNAT
{
    public class WinDivert : IDisposable
    {
        private readonly SafeWinDivertHandle handle;

        public WinDivert(string filter, WinDivertConstants.WinDivertLayer layer, short priority, WinDivertConstants.WinDivertFlag flags)
        {
            handle = WinDivertLow.WinDivertOpen(filter, layer, priority, flags);
        }

        public WinDivert(ReadOnlySpan<byte> filter, WinDivertConstants.WinDivertLayer layer, short priority, WinDivertConstants.WinDivertFlag flags)
        {
            handle = WinDivertLow.WinDivertOpen(filter, layer, priority, flags);
        }

        public (uint recvLen, uint addrLen) RecvEx(Span<byte> packet, Span<WinDivertAddress> abuf) => WinDivertLow.WinDivertRecvEx(handle, packet, abuf);
        public uint SendEx(ReadOnlySpan<byte> packet, ReadOnlySpan<WinDivertAddress> addr) => WinDivertLow.WinDivertSendEx(handle, packet, addr);

        public ulong QueueLength
        {
            get => WinDivertLow.WinDivertGetParam(handle, WinDivertConstants.WinDivertParam.QueueLength);
            set => WinDivertLow.WinDivertSetParam(handle, WinDivertConstants.WinDivertParam.QueueLength, value);
        }

        public ulong QueueTime
        {
            get => WinDivertLow.WinDivertGetParam(handle, WinDivertConstants.WinDivertParam.QueueTime);
            set => WinDivertLow.WinDivertSetParam(handle, WinDivertConstants.WinDivertParam.QueueTime, value);
        }

        public ulong QueueSize
        {
            get => WinDivertLow.WinDivertGetParam(handle, WinDivertConstants.WinDivertParam.QueueSize);
            set => WinDivertLow.WinDivertSetParam(handle, WinDivertConstants.WinDivertParam.QueueSize, value);
        }

        public ulong VersionMajor => WinDivertLow.WinDivertGetParam(handle, WinDivertConstants.WinDivertParam.VersionMajor);
        public ulong VersionMinor => WinDivertLow.WinDivertGetParam(handle, WinDivertConstants.WinDivertParam.VersionMinor);

        public void ShutdownRecv() => WinDivertLow.WinDivertShutdown(handle, WinDivertConstants.WinDivertShutdown.Recv);
        public void ShutdownSend() => WinDivertLow.WinDivertShutdown(handle, WinDivertConstants.WinDivertShutdown.Send);
        public void Shutdown() => WinDivertLow.WinDivertShutdown(handle, WinDivertConstants.WinDivertShutdown.Both);

#pragma warning disable CA1816
        public void Dispose() => handle.Dispose();
#pragma warning restore CA1816
    }

    public static class WinDivertHelper
    {
        public static IPv4Addr ParseIPv4Address(string addrStr) => WinDivertLow.WinDivertHelperParseIPv4Address(addrStr);
        public static IPv6Addr ParseIPv6Address(string addrStr) => WinDivertLow.WinDivertHelperParseIPv6Address(addrStr);
        public static string FormatIPv4Address(IPv4Addr addr) => WinDivertLow.WinDivertHelperFormatIPv4Address(addr);
        public static string FormatIPv6Address(IPv6Addr addr) => WinDivertLow.WinDivertHelperFormatIPv6Address(addr);
        public static void CalcChecksums(Span<byte> packet, ref WinDivertAddress addr, WinDivertConstants.WinDivertChecksumFlag flags) => WinDivertLow.WinDivertHelperCalcChecksums(packet, ref addr, flags);
        public static ReadOnlyMemory<byte> CompileFilter(string filter, WinDivertConstants.WinDivertLayer layer) => WinDivertLow.WinDivertHelperCompileFilter(filter, layer);
        public static ushort Ntoh(ushort x) => NativeMethods.WinDivertHelperNtohs(x);
        public static uint Ntoh(uint x) => NativeMethods.WinDivertHelperNtohl(x);
        public static ulong Ntoh(ulong x) => NativeMethods.WinDivertHelperNtohll(x);
        public static ushort Hton(ushort x) => NativeMethods.WinDivertHelperHtons(x);
        public static uint Hton(uint x) => NativeMethods.WinDivertHelperHtonl(x);
        public static ulong Hton(ulong x) => NativeMethods.WinDivertHelperHtonll(x);
    }

    public struct WinDivertIndexedPacketParser : IEnumerable<(int, WinDivertParseResult)>
    {
        private readonly WinDivertPacketParser e;

        public WinDivertIndexedPacketParser(Memory<byte> packet)
        {
            e = new WinDivertPacketParser(packet);
        }

        public WinDivertIndexedPacketParser(WinDivertPacketParser e)
        {
            this.e = e;
        }

        public WinDivertIndexedPacketEnumerator GetEnumerator() => new(e.GetEnumerator());
        IEnumerator<(int, WinDivertParseResult)> IEnumerable<(int, WinDivertParseResult)>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct WinDivertIndexedPacketEnumerator : IEnumerator<(int, WinDivertParseResult)>
    {
        private WinDivertPacketEnumerator e;
        private int i;

        public (int, WinDivertParseResult) Current => (i, e.Current);
        object IEnumerator.Current => Current;

        internal WinDivertIndexedPacketEnumerator(WinDivertPacketEnumerator e)
        {
            this.e = e;
            i = -1;
        }

        public bool MoveNext()
        {
            var success = e.MoveNext();
            if (!success) return false;
            i++;
            return true;
        }

        public void Reset()
        {
            e.Reset();
            i = -1;
        }

        public void Dispose() => e.Dispose();
    }

    public struct WinDivertPacketParser : IEnumerable<WinDivertParseResult>
    {
        private readonly Memory<byte> packet;

        public WinDivertPacketParser(Memory<byte> packet)
        {
            this.packet = packet;
        }

        public WinDivertPacketEnumerator GetEnumerator() => new(packet);
        IEnumerator<WinDivertParseResult> IEnumerable<WinDivertParseResult>.GetEnumerator() => new WinDivertPacketEnumerator(packet);
        IEnumerator IEnumerable.GetEnumerator() => new WinDivertPacketEnumerator(packet);
    }

    public unsafe struct WinDivertPacketEnumerator : IEnumerator<WinDivertParseResult>
    {
        private readonly MemoryHandle hmem;
        private readonly Memory<byte> packet;
        private readonly byte* pPacket0;
        private byte* pPacket;
        private uint packetLen;

        private WinDivertParseResult current;
        public WinDivertParseResult Current => current;
        object IEnumerator.Current => current;

        internal WinDivertPacketEnumerator(Memory<byte> packet)
        {
            hmem = packet.Pin();
            this.packet = packet;
            pPacket0 = (byte*)hmem.Pointer;
            pPacket = pPacket0;
            packetLen = (uint)packet.Length;
            current = new WinDivertParseResult();
        }

        public bool MoveNext()
        {
            var ipv4Hdr = (WinDivertIPv4Hdr*)null;
            var ipv6Hdr = (WinDivertIPv6Hdr*)null;
            var protocol = (byte)0;
            var icmpv4Hdr = (WinDivertICMPv4Hdr*)null;
            var icmpv6Hdr = (WinDivertICMPv6Hdr*)null;
            var tcpHdr = (WinDivertTCPHdr*)null;
            var udpHdr = (WinDivertUDPHdr*)null;
            var pData = (byte*)null;
            var dataLen = (uint)0;
            var pNext = (byte*)null;
            var nextLen = (uint)0;

            var success = NativeMethods.WinDivertHelperParsePacket(pPacket, packetLen, &ipv4Hdr, &ipv6Hdr, &protocol, &icmpv4Hdr, &icmpv6Hdr, &tcpHdr, &udpHdr, (void**)&pData, &dataLen, (void**)&pNext, &nextLen);
            if (!success) return false;

            current.Packet = pNext != null
                ? packet[(int)(pPacket - pPacket0)..(int)(pNext - pPacket0)]
                : packet[(int)(pPacket - pPacket0)..(int)(pPacket + packetLen - pPacket0)];
            current.IPv4Hdr = ipv4Hdr;
            current.IPv6Hdr = ipv6Hdr;
            current.Protocol = protocol;
            current.ICMPv4Hdr = icmpv4Hdr;
            current.ICMPv6Hdr = icmpv6Hdr;
            current.TCPHdr = tcpHdr;
            current.UDPHdr = udpHdr;
            current.Data = pData != null && dataLen > 0
                ? packet[(int)(pData - pPacket0)..(int)(pData + dataLen - pPacket0)]
                : Memory<byte>.Empty;

            pPacket = pNext;
            packetLen = nextLen;
            return true;
        }

        public void Reset()
        {
            pPacket = pPacket0;
            packetLen = (uint)packet.Length;
            current = new WinDivertParseResult();
        }

        public void Dispose() => hmem.Dispose();
    }

    public unsafe struct WinDivertParseResult
    {
        public Memory<byte> Packet;
        public WinDivertIPv4Hdr* IPv4Hdr;
        public WinDivertIPv6Hdr* IPv6Hdr;
        public byte Protocol;
        public WinDivertICMPv4Hdr* ICMPv4Hdr;
        public WinDivertICMPv6Hdr* ICMPv6Hdr;
        public WinDivertTCPHdr* TCPHdr;
        public WinDivertUDPHdr* UDPHdr;
        public Memory<byte> Data;
    }
}
