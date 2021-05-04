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
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace WinDivertNAT
{
    internal static class WinDivertLow
    {
        public static SafeWinDivertHandle WinDivertOpen(string filter, WinDivertConstants.WinDivertLayer layer, short priority, WinDivertConstants.WinDivertFlag flags)
        {
            var fobj = WinDivertHelperCompileFilter(filter, layer);
            return WinDivertOpen(fobj.Span, layer, priority, flags);
        }

        public static unsafe SafeWinDivertHandle WinDivertOpen(ReadOnlySpan<byte> filter, WinDivertConstants.WinDivertLayer layer, short priority, WinDivertConstants.WinDivertFlag flags)
        {
            var hraw = (IntPtr)(-1);
            fixed (byte* pFilter = filter) hraw = NativeMethods.WinDivertOpen(pFilter, layer, priority, flags);
            if (hraw == (IntPtr)(-1)) ThrowLastWin32Error();
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
                var success = false;
                fixed (void* pPacket = packet) fixed (WinDivertAddress* pAddr = addr)
                {
                    success = NativeMethods.WinDivertRecvEx(href.RawHandle, pPacket, packetLen, &recvLen, 0, pAddr, pAddrLen, null);
                }
                if (!success) ThrowLastWin32Error();
            }

            addrLen = (uint)(addrLen / sizeof(WinDivertAddress));
            return (recvLen, addrLen);
        }

        public static unsafe uint WinDivertSendEx(SafeWinDivertHandle handle, ReadOnlySpan<byte> packet, ReadOnlySpan<WinDivertAddress> addr)
        {
            using var href = new SafeHandleReference(handle, (IntPtr)(-1));
            var sendLen = (uint)0;
            var success = false;

            fixed (void* pPacket = packet) fixed (WinDivertAddress* pAddr = addr)
            {
                success = NativeMethods.WinDivertSendEx(href.RawHandle, pPacket, (uint)packet.Length, &sendLen, 0, pAddr, (uint)(addr.Length * sizeof(WinDivertAddress)), null);
            }
            if (!success) ThrowLastWin32Error();
            return sendLen;
        }

        public static void WinDivertSetParam(SafeWinDivertHandle handle, WinDivertConstants.WinDivertParam param, ulong value)
        {
            using var href = new SafeHandleReference(handle, (IntPtr)(-1));
            var success = NativeMethods.WinDivertSetParam(href.RawHandle, param, value);
            if (!success) ThrowLastWin32Error();
        }

        public static ulong WinDivertGetParam(SafeWinDivertHandle handle, WinDivertConstants.WinDivertParam param)
        {
            using var href = new SafeHandleReference(handle, (IntPtr)(-1));
            var success = NativeMethods.WinDivertGetParam(href.RawHandle, param, out var value);
            if (!success) ThrowLastWin32Error();
            return value;
        }

        public static void WinDivertShutdown(SafeWinDivertHandle handle, WinDivertConstants.WinDivertShutdown how)
        {
            using var href = new SafeHandleReference(handle, (IntPtr)(-1));
            var success = NativeMethods.WinDivertShutdown(href.RawHandle, how);
            if (!success) ThrowLastWin32Error();
        }

        public static unsafe IPv4Addr WinDivertHelperParseIPv4Address(string addrStr)
        {
            var addr = new IPv4Addr();
            var success = NativeMethods.WinDivertHelperParseIPv4Address(addrStr, &addr.Raw);
            if (!success) ThrowLastWin32Error();

            addr.Raw = NativeMethods.WinDivertHelperHtonl(addr.Raw);
            return addr;
        }

        public static unsafe IPv6Addr WinDivertHelperParseIPv6Address(string addrStr)
        {
            var addr = new IPv6Addr();
            var success = NativeMethods.WinDivertHelperParseIPv6Address(addrStr, addr.Raw);
            if (!success) ThrowLastWin32Error();

            NativeMethods.WinDivertHelperHtonIPv6Address(addr.Raw, addr.Raw);
            return addr;
        }

        public static unsafe string WinDivertHelperFormatIPv4Address(IPv4Addr addr)
        {
            addr.Raw = NativeMethods.WinDivertHelperNtohl(addr.Raw);

            var buffer = (Span<byte>)stackalloc byte[32];
            var success = false;
            fixed (byte* pBuffer = buffer) success = NativeMethods.WinDivertHelperFormatIPv4Address(addr.Raw, pBuffer, (uint)buffer.Length);
            if (!success) ThrowLastWin32Error();

            var strlen = buffer.IndexOf((byte)0);
            return Encoding.ASCII.GetString(buffer[0..strlen]);
        }

        public static unsafe string WinDivertHelperFormatIPv6Address(IPv6Addr addr)
        {
            NativeMethods.WinDivertHelperNtohIPv6Address(addr.Raw, addr.Raw);

            var buffer = (Span<byte>)stackalloc byte[64];
            var success = false;
            fixed (byte* pBuffer = buffer) success = NativeMethods.WinDivertHelperFormatIPv6Address(addr.Raw, pBuffer, (uint)buffer.Length);
            if (!success) ThrowLastWin32Error();

            var strlen = buffer.IndexOf((byte)0);
            return Encoding.ASCII.GetString(buffer[0..strlen]);
        }

        public static unsafe void WinDivertHelperCalcChecksums(Span<byte> packet, ref WinDivertAddress addr, WinDivertConstants.WinDivertChecksumFlag flags)
        {
            var success = false;
            fixed (void* pPacket = packet) fixed (WinDivertAddress* pAddr = &addr)
            {
                success = NativeMethods.WinDivertHelperCalcChecksums(pPacket, (uint)packet.Length, pAddr, flags);
            }
            if (!success) ThrowLastWin32Error();
        }

        public static unsafe ReadOnlyMemory<byte> WinDivertHelperCompileFilter(string filter, WinDivertConstants.WinDivertLayer layer)
        {
            var fobj = (new byte[256 * 24]).AsMemory();
            var pErrorStr = (byte*)null;
            var errorPos = (uint)0;
            var success = false;
            fixed (byte* pFobj = fobj.Span) success = NativeMethods.WinDivertHelperCompileFilter(filter, layer, pFobj, (uint)fobj.Length, &pErrorStr, &errorPos);
            if (!success)
            {
                var errorLen = 0;
                while (*(pErrorStr + errorLen) != 0) errorLen++;
                var errorStr = Encoding.ASCII.GetString(pErrorStr, errorLen);
                throw new WinDivertInvalidFilterException(errorStr, errorPos, nameof(filter));
            }
            return fobj;
        }

        private static void ThrowLastWin32Error()
        {
            var code = Marshal.GetLastWin32Error();
            if (code == 2) throw new FileNotFoundException();
            if (code == 87) throw new ArgumentException();
            throw new Win32Exception(code);
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

    internal class WinDivertInvalidFilterException : ArgumentException
    {
        public string FilterErrorStr;
        public uint FilterErrorPos;

        public WinDivertInvalidFilterException(string errorStr, uint errorPos, string? paramName) : base(errorStr, paramName)
        {
            FilterErrorStr = errorStr;
            FilterErrorPos = errorPos;
        }
    }
}
