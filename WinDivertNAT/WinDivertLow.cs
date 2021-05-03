/*
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

        public static unsafe (byte[]? recv, WinDivertAddress[]? addr) WinDivertRecvEx(SafeWinDivertHandle handle, byte[]? packet, WinDivertAddress[]? addr)
        {
            var packetLen = (uint)0;
            if (packet is object) packetLen = (uint)packet.Length;

            var (recvLen, addrLen) = UseHandle(handle, (hraw) =>
            {
                var fixedRecvLen = (uint)0;
                var fixedAddrLen = (uint)0;
                var pAddrLen = (uint*)null;
                if (addr is object)
                {
                    fixedAddrLen = (uint)(addr.Length * sizeof(WinDivertAddress));
                    pAddrLen = &fixedAddrLen;
                }

                var result = false;
                fixed (void* pPacket = packet) fixed (WinDivertAddress* pAddr = addr)
                {
                    result = NativeMethods.WinDivertRecvEx(hraw, pPacket, packetLen, &fixedRecvLen, 0, pAddr, pAddrLen, null);
                }
                if (!result) throw new Win32Exception();
                return (fixedRecvLen, (uint)(fixedAddrLen / sizeof(WinDivertAddress)));
            });

            var recv = (byte[]?)null;
            if (packet is object) recv = packet[0..(int)recvLen];
            var aret = (WinDivertAddress[]?)null;
            if (addr is object) aret = addr[0..(int)addrLen];
            return (recv, aret);
        }

        public static unsafe uint WinDivertSendEx(SafeWinDivertHandle handle, byte[] packet, WinDivertAddress[] addr)
        {
            return UseHandle(handle, (hraw) =>
            {
                var fixedSendLen = (uint)0;

                var result = false;
                fixed (void* pPacket = packet) fixed (WinDivertAddress* pAddr = addr)
                {
                    result = NativeMethods.WinDivertSendEx(hraw, pPacket, (uint)packet.Length, &fixedSendLen, 0, pAddr, (uint)(addr.Length * sizeof(WinDivertAddress)), null);
                }
                if (!result) throw new Win32Exception();
                return fixedSendLen;
            });
        }

        public static void WinDivertSetParam(SafeWinDivertHandle handle, WinDivertConstants.WinDivertParam param, ulong value) => UseHandle(handle, (hraw) =>
        {
            var result = NativeMethods.WinDivertSetParam(hraw, param, value);
            if (!result) throw new Win32Exception();
        });

        public static ulong WinDivertGetParam(SafeWinDivertHandle handle, WinDivertConstants.WinDivertParam param) => UseHandle(handle, (hraw) =>
        {
            var result = NativeMethods.WinDivertGetParam(hraw, param, out var value);
            if (!result) throw new Win32Exception();
            return value;
        });

        public static void WinDivertShutdown(SafeWinDivertHandle handle, WinDivertConstants.WinDivertShutdown how) => UseHandle(handle, (hraw) =>
        {
            var result = NativeMethods.WinDivertShutdown(hraw, how);
            if (!result) throw new Win32Exception();
        });

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

        private static void UseHandle(SafeWinDivertHandle handle, Action<IntPtr> action) => UseHandle<object?>(handle, (hraw) =>
        {
            action(hraw);
            return null;
        });

        private static T UseHandle<T>(SafeWinDivertHandle handle, Func<IntPtr, T> func)
        {
            if (handle is null)
            {
                return func(IntPtr.Zero);
            }

            var addref = false;
            try
            {
                handle.DangerousAddRef(ref addref);
                var hraw = handle.DangerousGetHandle();
                return func(hraw);
            }
            finally
            {
                if (addref) handle.DangerousRelease();
            }
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
}
