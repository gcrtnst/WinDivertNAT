/*
 * WinDivertNAT.cs
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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;

namespace WinDivertNAT
{
    public class WinDivertNAT
    {
        public readonly ReadOnlyMemory<byte> Filter;
        public short Priority = 0;
        public ulong QueueLength = 4096;
        public ulong QueueTime = 2000;
        public ulong QueueSize = 4194304;
        public int BufLength = 1;
        public int BufSize = 40 + 0xFFFF;

        public bool Drop { get; init; } = false;
        public bool? Outbound { get; init; } = null;
        public uint? IfIdx { get; init; } = null;
        public uint? SubIfIdx { get; init; } = null;
        public NetworkIPv4Addr? IPv4SrcAddr { get; init; } = null;
        public NetworkIPv4Addr? IPv4DstAddr { get; init; } = null;
        public NetworkIPv6Addr? IPv6SrcAddr { get; init; } = null;
        public NetworkIPv6Addr? IPv6DstAddr { get; init; } = null;
        public NetworkUInt16? TCPSrcPort { get; init; } = null;
        public NetworkUInt16? TCPDstPort { get; init; } = null;
        public NetworkUInt16? UDPSrcPort { get; init; } = null;
        public NetworkUInt16? UDPDstPort { get; init; } = null;
        public TextWriter? Logger { get; init; } = null;

        public WinDivertNAT(string filter)
        {
            var fobj = WinDivert.CompileFilter(filter, WinDivert.Layer.Network);
            Filter = fobj;
        }

        public WinDivertNAT(ReadOnlyMemory<byte> filter)
        {
            Filter = filter;
        }

        public void Run(CancellationToken token)
        {
            var modify = Outbound.HasValue
                || IfIdx.HasValue
                || SubIfIdx.HasValue
                || IPv4SrcAddr.HasValue
                || IPv4DstAddr.HasValue
                || IPv6SrcAddr.HasValue
                || IPv6DstAddr.HasValue
                || TCPSrcPort.HasValue
                || TCPDstPort.HasValue
                || UDPSrcPort.HasValue
                || UDPDstPort.HasValue;

            if (modify && !Drop) RunNormal(token);
            else if (Logger is not null) RunRecvOnly(token);
            else if (Drop && Logger is null) RunDrop(token);
            else RunNothing(token);
        }

        private void RunNormal(CancellationToken token)
        {
            using var divert = new WinDivert(Filter.Span, WinDivert.Layer.Network, Priority, 0)
            {
                QueueLength = QueueLength,
                QueueTime = QueueTime,
                QueueSize = QueueSize,
            };

            var packet = new Memory<byte>(new byte[BufSize]);
            var abuf = new Memory<WinDivertAddress>(new WinDivertAddress[BufLength]);
            using var reg = token.Register(() => divert.ShutdownRecv());
            while (true)
            {
                var recvLen = (uint)0;
                var addrLen = (uint)0;
                try
                {
                    (recvLen, addrLen) = divert.RecvEx(packet.Span, abuf.Span);
                }
                catch (Win32Exception e) when (e.NativeErrorCode == 232)
                {
                    divert.ShutdownSend();
                    token.ThrowIfCancellationRequested();
                    return;
                }

                var recv = packet[0..(int)recvLen];
                var addr = abuf[0..(int)addrLen];
                foreach (var (i, p) in new WinDivertIndexedPacketParser(recv))
                {
                    Log(p, in addr.Span[i]);
                    ModifyPacket(p, ref addr.Span[i]);
                }
                _ = divert.SendEx(recv.Span, addr.Span);
            }
        }

        private void RunRecvOnly(CancellationToken token)
        {
            var flags = WinDivert.Flag.RecvOnly;
            if (!Drop) flags |= WinDivert.Flag.Sniff;

            using var divert = new WinDivert(Filter.Span, WinDivert.Layer.Network, Priority, flags)
            {
                QueueLength = QueueLength,
                QueueTime = QueueTime,
                QueueSize = QueueSize,
            };

            var packet = new Memory<byte>(new byte[BufSize]);
            var abuf = new Memory<WinDivertAddress>(new WinDivertAddress[BufLength]);
            using var reg = token.Register(() => divert.Shutdown());
            while (true)
            {
                var recvLen = (uint)0;
                var addrLen = (uint)0;
                try
                {
                    (recvLen, addrLen) = divert.RecvEx(packet.Span, abuf.Span);
                }
                catch (Win32Exception e) when (e.NativeErrorCode == 232)
                {
                    token.ThrowIfCancellationRequested();
                    return;
                }

                var recv = packet[0..(int)recvLen];
                var addr = abuf[0..(int)addrLen];
                foreach (var (i, p) in new WinDivertIndexedPacketParser(recv)) Log(p, in addr.Span[i]);
            }
        }

        private void RunDrop(CancellationToken token)
        {
            using var divert = new WinDivert(Filter.Span, WinDivert.Layer.Network, Priority, WinDivert.Flag.Drop | WinDivert.Flag.RecvOnly);
            _ = token.WaitHandle.WaitOne();
            divert.Shutdown();
            token.ThrowIfCancellationRequested();
        }

        private static void RunNothing(CancellationToken token)
        {
            _ = token.WaitHandle.WaitOne();
            token.ThrowIfCancellationRequested();
        }

        private unsafe void ModifyPacket(WinDivertParseResult p, ref WinDivertAddress addr)
        {
            if (Outbound is bool outbound) addr.Outbound = outbound;
            if (IfIdx is uint ifIdx) addr.Network.IfIdx = ifIdx;
            if (SubIfIdx is uint subIfIdx) addr.Network.SubIfIdx = subIfIdx;
            if (p.IPv4Hdr != null)
            {
                if (IPv4SrcAddr is NetworkIPv4Addr ipv4SrcAddr) p.IPv4Hdr->SrcAddr = ipv4SrcAddr;
                if (IPv4DstAddr is NetworkIPv4Addr ipv4DstAddr) p.IPv4Hdr->DstAddr = ipv4DstAddr;
            }
            if (p.IPv6Hdr != null)
            {
                if (IPv6SrcAddr is NetworkIPv6Addr ipv6SrcAddr) p.IPv6Hdr->SrcAddr = ipv6SrcAddr;
                if (IPv6DstAddr is NetworkIPv6Addr ipv6DstAddr) p.IPv6Hdr->DstAddr = ipv6DstAddr;
            }
            if (p.TCPHdr != null)
            {
                if (TCPSrcPort is NetworkUInt16 tcpSrcPort) p.TCPHdr->SrcPort = tcpSrcPort;
                if (TCPDstPort is NetworkUInt16 tcpDstPort) p.TCPHdr->DstPort = tcpDstPort;
            }
            if (p.UDPHdr != null)
            {
                if (UDPSrcPort is NetworkUInt16 udpSrcPort) p.UDPHdr->SrcPort = udpSrcPort;
                if (UDPDstPort is NetworkUInt16 udpDstPort) p.UDPHdr->DstPort = udpDstPort;
            }
            WinDivert.CalcChecksums(p.Packet.Span, ref addr, 0);
        }

        private unsafe void Log(WinDivertParseResult p, in WinDivertAddress addr)
        {
            if (Logger is not null)
            {
                var l = new List<string>
                {
                    $"Time={DateTime.UtcNow:O}",
                    $"Outbound={addr.Outbound}",
                    $"IfIdx={addr.Network.IfIdx}",
                    $"SubIfIdx={addr.Network.SubIfIdx}",
                };
                if (p.IPv4Hdr != null)
                {
                    l.Add($"IPv4SrcAddr={p.IPv4Hdr->SrcAddr}");
                    l.Add($"IPv4DstAddr={p.IPv4Hdr->DstAddr}");
                }
                if (p.IPv6Hdr != null)
                {
                    l.Add($"IPv6SrcAddr={p.IPv6Hdr->SrcAddr}");
                    l.Add($"IPv6DstAddr={p.IPv6Hdr->DstAddr}");
                }
                if (p.TCPHdr != null)
                {
                    l.Add($"TCPSrcPort={p.TCPHdr->SrcPort}");
                    l.Add($"TCPDstPort={p.TCPHdr->DstPort}");
                }
                if (p.UDPHdr != null)
                {
                    l.Add($"UDPSrcPort={p.UDPHdr->SrcPort}");
                    l.Add($"UDPDstPort={p.UDPHdr->DstPort}");
                }
                Logger.WriteLine(string.Join(" ", l));
            }
        }
    }
}
