﻿/*
 * WinDivertLow.cs
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

using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WinDivertNAT
{
    internal static class WinDivertLow
    {
        public static SafeWinDivertHandle WinDivertOpen(string filter, WinDivertConstants.WinDivertLayer layer, short priority, WinDivertConstants.WinDivertFlag flags)
        {
            var hraw = NativeMethods.WinDivertOpen(filter, layer, priority, flags);
            if (hraw == IntPtr.Zero || hraw == (IntPtr)(-1)) throw new Win32Exception();
            return new SafeWinDivertHandle(hraw, true);
        }

        public static unsafe (uint recvLen, uint addrLen) WinDivertRecvEx(SafeWinDivertHandle handle, Span<byte> packet, Span<WinDivertAddress> addr)
        {
            var packetLen = (uint)0;
            if (!packet.IsEmpty) packetLen = (uint)packet.Length;
            var recvLen = (uint)0;
            var addrLen = (uint)0;
            var pAddrLen = (uint*)null;
            if (!addr.IsEmpty)
            {
                addrLen = (uint)(addr.Length * sizeof(WinDivertAddress));
                pAddrLen = &addrLen;
            }

            using (var href = new SafeHandleReference(handle, (IntPtr)(-1)))
            {
                var result = false;
                fixed (void* pPacket = packet) fixed (WinDivertAddress* pAddr = addr)
                {
                    result = NativeMethods.WinDivertRecvEx(href.RawHandle, pPacket, packetLen, &recvLen, 0, pAddr, pAddrLen, null);
                }
                if (!result) throw new Win32Exception();
            }

            addrLen = (uint)(addrLen / sizeof(WinDivertAddress));
            return (recvLen, addrLen);
        }

        public static unsafe uint WinDivertSendEx(SafeWinDivertHandle handle, ReadOnlySpan<byte> packet, ReadOnlySpan<WinDivertAddress> addr)
        {
            using var href = new SafeHandleReference(handle, (IntPtr)(-1));
            var sendLen = (uint)0;
            var result = false;

            fixed (void* pPacket = packet) fixed (WinDivertAddress* pAddr = addr)
            {
                result = NativeMethods.WinDivertSendEx(href.RawHandle, pPacket, (uint)packet.Length, &sendLen, 0, pAddr, (uint)(addr.Length * sizeof(WinDivertAddress)), null);
            }
            if (!result) throw new Win32Exception();
            return sendLen;
        }

        public static void WinDivertSetParam(SafeWinDivertHandle handle, WinDivertConstants.WinDivertParam param, ulong value)
        {
            using var href = new SafeHandleReference(handle, (IntPtr)(-1));
            var result = NativeMethods.WinDivertSetParam(href.RawHandle, param, value);
            if (!result) throw new Win32Exception();
        }

        public static ulong WinDivertGetParam(SafeWinDivertHandle handle, WinDivertConstants.WinDivertParam param)
        {
            using var href = new SafeHandleReference(handle, (IntPtr)(-1));
            var result = NativeMethods.WinDivertGetParam(href.RawHandle, param, out var value);
            if (!result) throw new Win32Exception();
            return value;
        }

        public static void WinDivertShutdown(SafeWinDivertHandle handle, WinDivertConstants.WinDivertShutdown how)
        {
            using var href = new SafeHandleReference(handle, (IntPtr)(-1));
            var result = NativeMethods.WinDivertShutdown(href.RawHandle, how);
            if (!result) throw new Win32Exception();
        }

        public static unsafe IPv6Addr WinDivertHelperNtohIPv6Address(IPv6Addr addr)
        {
            var outAddr = new IPv6Addr();
            NativeMethods.WinDivertHelperNtohIPv6Address(&addr, &outAddr);
            return outAddr;
        }

        public static unsafe IPv6Addr WinDivertHelperHtonIPv6Address(IPv6Addr addr)
        {
            var outAddr = new IPv6Addr();
            NativeMethods.WinDivertHelperHtonIPv6Address(&addr, &outAddr);
            return outAddr;
        }
    }

    internal class SafeWinDivertHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeWinDivertHandle(IntPtr existingHandle, bool ownsHandle) : base(ownsHandle)
        {
            SetHandle(existingHandle);
        }

        protected override bool ReleaseHandle() => NativeMethods.WinDivertClose(handle);
    }

    internal struct SafeHandleReference : IDisposable
    {
        public readonly IntPtr RawHandle;
        private readonly SafeHandle? handle;
        private bool reference;

        public SafeHandleReference(SafeHandle? handle, IntPtr invalid)
        {
            this.handle = handle;
            reference = false;
            if (handle is null || handle.IsInvalid || handle.IsClosed)
            {
                RawHandle = invalid;
                return;
            }
            handle.DangerousAddRef(ref reference);
            RawHandle = handle.DangerousGetHandle();
        }

        public void Dispose()
        {
            if (reference)
            {
                handle?.DangerousRelease();
                reference = false;
            }
        }
    }
}
