/*
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

namespace WinDivertNAT
{
    internal class WinDivert : IDisposable
    {
        private readonly SafeWinDivertHandle handle;

        public WinDivert(string filter, WinDivertConstants.WinDivertLayer layer, short priority, WinDivertConstants.WinDivertFlag flags)
        {
            handle = WinDivertLow.WinDivertOpen(filter, layer, priority, flags);
        }

        public void RecvEx(byte[]? packet, out uint recvLen, WinDivertAddress[]? addr, out uint addrLen) => WinDivertLow.WinDivertRecvEx(handle, packet, out recvLen, addr, out addrLen);
        public void SendEx(byte[] packet, out uint sendLen, WinDivertAddress[] addr) => WinDivertLow.WinDivertSendEx(handle, packet, out sendLen, addr);

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
        public void Dispose() => handle.Dispose();
    }
}
