﻿/*
 * NativeMethods.cs
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
using System.Runtime.InteropServices;
using System.Threading;

namespace WinDivertNAT
{
    internal static class NativeMethods
    {
        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern IntPtr WinDivertOpen(string filter, WinDivertConstants.WinDivertLayer layer, short priority, WinDivertConstants.WinDivertFlag flags);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern unsafe bool WinDivertRecvEx(IntPtr handle, void* packet, uint packetLen, uint* recvLen, ulong flags, WinDivertAddress* addr, uint* addrLen, NativeOverlapped* overlapped);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern unsafe bool WinDivertSendEx(IntPtr handle, void* packet, uint packetLen, uint* sendLen, ulong flags, WinDivertAddress* addr, uint addrLen, NativeOverlapped* overlapped);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern bool WinDivertSetParam(IntPtr handle, WinDivertConstants.WinDivertParam param, ulong value);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern bool WinDivertGetParam(IntPtr handle, WinDivertConstants.WinDivertParam param, out ulong value);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern bool WinDivertShutdown(IntPtr handle, WinDivertConstants.WinDivertShutdown how);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern bool WinDivertClose(IntPtr handle);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern unsafe bool WinDivertHelperParsePacket(void* packet, uint packetLen, WinDivertIPv4Hdr** ipv4Hdr, WinDivertIPv6Hdr** ipv6Hdr, byte* protocol, WinDivertICMPv4Hdr** icmpv4Hdr, WinDivertICMPv6Hdr** icmpv6Hdr, WinDivertTCPHdr** tcpHdr, WinDivertUDPHdr** udpHdr, void** data, uint* dataLen, void** next, uint* nextLen);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern unsafe bool WinDivertHelperParseIPv4Address(string addrStr, uint* addr);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern unsafe bool WinDivertHelperFormatIPv4Address(uint addr, byte* buffer, uint buflen);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern unsafe bool WinDivertHelperFormatIPv6Address(uint* addr, byte* buffer, uint buflen);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern ushort WinDivertHelperNtohs(ushort x);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern ushort WinDivertHelperHtons(ushort x);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern uint WinDivertHelperNtohl(uint x);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern uint WinDivertHelperHtonl(uint x);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern ulong WinDivertHelperNtohll(ulong x);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern ulong WinDivertHelperHtonll(ulong x);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern unsafe void WinDivertHelperNtohIPv6Address(uint* inAddr, uint* outAddr);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern unsafe void WinDivertHelperHtonIPv6Address(uint* inAddr, uint* outAddr);
    }

    internal static class WinDivertConstants
    {
        public enum WinDivertLayer
        {
            Network = 0,
            NetworkForward = 1,
            Flow = 2,
            Socket = 3,
            Reflect = 4,
        }

        public enum WinDivertEvent
        {
            NetworkPacket = 0,
            FlowEstablished = 1,
            FlowDeleted = 2,
            SocketBind = 3,
            SocketConnect = 4,
            SocketListen = 5,
            SocketAccept = 6,
            SocketClose = 7,
            ReflectOpen = 8,
            ReflectClose = 9,
        }

        [Flags]
        public enum WinDivertFlag : ulong
        {
            Sniff = 0x0001,
            Drop = 0x0002,
            RecvOnly = 0x0004,
            ReadOnly = RecvOnly,
            SendOnly = 0x0008,
            WriteOnly = SendOnly,
            NoInstall = 0x0010,
            Fragments = 0x0020,
        }

        public enum WinDivertParam
        {
            QueueLength = 0,
            QueueTime = 1,
            QueueSize = 2,
            VersionMajor = 3,
            VersionMinor = 4,
        }

        public enum WinDivertShutdown
        {
            Recv = 0x1,
            Send = 0x2,
            Both = 0x3,
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct WinDivertAddress
    {
        [FieldOffset(0)] public long Timestamp;
        [FieldOffset(8)] public byte Layer;
        [FieldOffset(9)] public byte Event;
        [FieldOffset(10)] public byte Flags;

        [FieldOffset(16)] public WinDivertDataNetwork Network;
        [FieldOffset(16)] public WinDivertDataFlow Flow;
        [FieldOffset(16)] public WinDivertDataSocket Socket;
        [FieldOffset(16)] public WinDivertDataReflect Reflect;
        [FieldOffset(16)] private fixed byte reserved[64];
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WinDivertDataNetwork
    {
        public uint IfIdx;
        public uint SubIfIdx;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct WinDivertDataFlow
    {
        public ulong EndpointId;
        public ulong ParentEndpointId;
        public uint ProcessId;
        public IPv6Addr LocalAddr;
        public IPv6Addr RemoteAddr;
        public ushort LocalPort;
        public ushort RemotePort;
        public byte Protocol;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct WinDivertDataSocket
    {
        public ulong EndpointId;
        public ulong ParentEndpointId;
        public uint ProcessId;
        public IPv6Addr LocalAddr;
        public IPv6Addr RemoteAddr;
        public ushort LocalPort;
        public ushort RemotePort;
        public byte Protocol;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WinDivertDataReflect
    {
        public long Timestamp;
        public uint ProcessId;
        public WinDivertConstants.WinDivertLayer Layer;
        public ulong Flags;
        public short Priority;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WinDivertIPv4Hdr
    {
        private readonly byte versionIHL;
        public byte TOS;
        public ushort Length;
        public ushort Id;
        private readonly ushort fragOff0;
        public byte TTL;
        public byte Protocol;
        public ushort Checksum;
        public IPv4Addr SrcAddr;
        public IPv4Addr DstAddr;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WinDivertIPv6Hdr
    {
        private readonly uint version0;
        public ushort Length;
        public byte NextHdr;
        public byte HopLimit;
        public IPv6Addr SrcAddr;
        public IPv6Addr DstAddr;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WinDivertICMPv4Hdr
    {
        public byte Type;
        public byte Code;
        public ushort Checksum;
        public uint Body;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WinDivertICMPv6Hdr
    {
        public byte Type;
        public byte Code;
        public ushort Checksum;
        public uint Body;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WinDivertTCPHdr
    {
        public ushort SrcPort;
        public ushort DstPort;
        public uint SeqNum;
        public uint AckNum;
        private readonly ushort hdrLength0;
        public ushort Window;
        public ushort Checksum;
        public ushort UrgPtr;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WinDivertUDPHdr
    {
        public ushort SrcPort;
        public ushort DstPort;
        public ushort Length;
        public ushort Checksum;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IPv4Addr
    {
        internal uint Addr;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct IPv6Addr
    {
        internal fixed uint Addr[4];
    }
}
