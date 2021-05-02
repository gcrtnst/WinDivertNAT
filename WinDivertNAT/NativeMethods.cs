/*
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
        public static extern unsafe bool WinDivertRecvEx(IntPtr handle, byte[]? packet, uint packetLen, uint* recvLen, ulong flags, WinDivertAddress[]? addr, uint* addrLen, NativeOverlapped* overlapped);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern unsafe bool WinDivertSendEx(IntPtr handle, byte[] packet, uint packetLen, uint* sendLen, ulong flags, WinDivertAddress[] addr, uint addrLen, NativeOverlapped* overlapped);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern bool WinDivertSetParam(IntPtr handle, WinDivertConstants.WinDivertParam param, ulong value);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern bool WinDivertGetParam(IntPtr handle, WinDivertConstants.WinDivertParam param, out ulong value);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern bool WinDivertShutdown(IntPtr handle, WinDivertConstants.WinDivertShutdown how);

        [DllImport("WinDivert.dll", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, PreserveSig = true, SetLastError = true)]
        public static extern bool WinDivertClose(IntPtr handle);
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

        [FieldOffset(16)] public WinDivertDataNetwork Netrowk;
        [FieldOffset(16)] public WinDivertDataFlow Flow;
        [FieldOffset(16)] public WinDivertDataSocket Socket;
        [FieldOffset(16)] public WinDivertDataReflect Reflect;
        [FieldOffset(16)] private fixed byte reserved[64];
    }

    internal struct WinDivertDataNetwork
    {
#pragma warning disable CS0649
        public uint IfIdx;
        public uint SubIfIdx;
#pragma warning restore CS0649
    }

    internal unsafe struct WinDivertDataFlow
    {
#pragma warning disable CS0649
        public ulong EndpointId;
        public ulong ParentEndpointId;
        public uint ProcessId;
        public fixed uint LocalAddr[4];
        public fixed uint RemoteAddr[4];
        public ushort LocalPort;
        public ushort RemotePort;
        public byte Protocol;
#pragma warning restore CS0649
    }

    internal unsafe struct WinDivertDataSocket
    {
#pragma warning disable CS0649
        public ulong EndpointId;
        public ulong ParentEndpointId;
        public uint ProcessId;
        public fixed uint LocalAddr[4];
        public fixed uint RemoteAddr[4];
        public ushort LocalPort;
        public ushort RemotePort;
        public byte Protocol;
#pragma warning restore CS0649
    }

    internal struct WinDivertDataReflect
    {
#pragma warning disable CS0649
        public long Timestamp;
        public uint ProcessId;
        public WinDivertConstants.WinDivertLayer Layer;
        public ulong Flags;
        public short Priority;
#pragma warning restore CS0649
    }
}
