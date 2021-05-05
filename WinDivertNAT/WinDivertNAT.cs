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
    internal class WinDivertNAT
    {
        public ReadOnlyMemory<byte> Filter;
        public bool Drop = false;
        public bool? Outbound = null;
        public uint? IfIdx = null;
        public uint? SubIfIdx = null;
        public IPv4Addr? IPv4SrcAddr = null;
        public IPv4Addr? IPv4DstAddr = null;
        public IPv6Addr? IPv6SrcAddr = null;
        public IPv6Addr? IPv6DstAddr = null;
        public ushort? TCPSrcPort = null;
        public ushort? TCPDstPort = null;
        public ushort? UDPSrcPort = null;
        public ushort? UDPDstPort = null;
        public TextWriter? Logger = null;

        private short priority = 0;
        public short Priority
        {
            get => priority;
            set
            {
                if (value is < (-30000) or > 30000) throw new ArgumentOutOfRangeException(nameof(value));
                priority = value;
            }
        }

        private ulong queueLength = 4096;
        public ulong QueueLength
        {
            get => queueLength;
            set
            {
                if (value is < 32 or > 16384) throw new ArgumentOutOfRangeException(nameof(value));
                queueLength = value;
            }
        }

        private ulong queueTime = 2000;
        public ulong QueueTime
        {
            get => queueTime;
            set
            {
                if (value is < 100 or > 16000) throw new ArgumentOutOfRangeException(nameof(value));
                queueTime = value;
            }
        }

        private ulong queueSize = 4194304;
        public ulong QueueSize
        {
            get => queueSize;
            set
            {
                if (value is < 65535 or > 33554432) throw new ArgumentOutOfRangeException(nameof(value));
                queueSize = value;
            }
        }

        private int bufLength = 255;
        public int BufLength
        {
            get => bufLength;
            set
            {
                if (value is < 1 or > 255) throw new ArgumentOutOfRangeException(nameof(value));
                bufLength = value;
            }
        }

        private int bufSize = 131072;
        public int BufSize
        {
            get => bufSize;
            set
            {
                if (value is < 65535 or > 33554432) throw new ArgumentOutOfRangeException(nameof(value));
                bufSize = value;
            }
        }

        public WinDivertNAT(string filter)
        {
            var fobj = WinDivertHelper.CompileFilter(filter, WinDivertConstants.WinDivertLayer.Network);
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
            else if (Drop && Logger is null) RunDrop(token);
            else if (Drop && Logger is not null) RunRecvOnly(token);
            else if (Logger is not null) RunSniff(token);
            else RunNothing(token);
        }

        private void RunNormal(CancellationToken token)
        {
            using var divert = new WinDivert(Filter.Span, WinDivertConstants.WinDivertLayer.Network, priority, 0)
            {
                QueueLength = queueLength,
                QueueTime = queueTime,
                QueueSize = queueSize,
            };

            var packet = new Memory<byte>(new byte[bufSize]);
            var abuf = new Memory<WinDivertAddress>(new WinDivertAddress[bufLength]);
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
                var i = -1;
                foreach (var parse in new WinDivertPacketParser(recv))
                {
                    i++;
                    Log(parse, in addr.Span[i]);
                    ModifyPacket(parse, ref addr.Span[i]);
                }
                _ = divert.SendEx(recv.Span, addr.Span);
            }
        }

        private void RunSniff(CancellationToken token)
        {
            using var divert = new WinDivert(Filter.Span, WinDivertConstants.WinDivertLayer.Network, priority, WinDivertConstants.WinDivertFlag.Sniff | WinDivertConstants.WinDivertFlag.RecvOnly)
            {
                QueueLength = queueLength,
                QueueTime = queueTime,
                QueueSize = queueSize
            };

            var packet = new Memory<byte>(new byte[bufSize]);
            var abuf = new Memory<WinDivertAddress>(new WinDivertAddress[bufLength]);
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
                var i = -1;
                foreach (var parse in new WinDivertPacketParser(recv))
                {
                    i++;
                    Log(parse, in addr.Span[i]);
                }
            }
        }

        private void RunDrop(CancellationToken token)
        {
            using var divert = new WinDivert(Filter.Span, WinDivertConstants.WinDivertLayer.Network, priority, WinDivertConstants.WinDivertFlag.Drop | WinDivertConstants.WinDivertFlag.RecvOnly);
            _ = token.WaitHandle.WaitOne();
            divert.Shutdown();
            token.ThrowIfCancellationRequested();
        }

        private void RunRecvOnly(CancellationToken token)
        {
            using var divert = new WinDivert(Filter.Span, WinDivertConstants.WinDivertLayer.Network, priority, WinDivertConstants.WinDivertFlag.RecvOnly)
            {
                QueueLength = queueLength,
                QueueTime = queueTime,
                QueueSize = queueSize,
            };

            var packet = new Memory<byte>(new byte[bufSize]);
            var abuf = new Memory<WinDivertAddress>(new WinDivertAddress[bufLength]);
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
                var i = -1;
                foreach (var parse in new WinDivertPacketParser(recv))
                {
                    i++;
                    Log(parse, in addr.Span[i]);
                }
            }
        }

        private static void RunNothing(CancellationToken token)
        {
            _ = token.WaitHandle.WaitOne();
            token.ThrowIfCancellationRequested();
        }

        private unsafe void ModifyPacket(WinDivertParseResult parse, ref WinDivertAddress addr)
        {
            if (Outbound is bool outbound) addr.Outbound = outbound;
            if (IfIdx is uint ifIdx) addr.Network.IfIdx = ifIdx;
            if (SubIfIdx is uint subIfIdx) addr.Network.SubIfIdx = subIfIdx;
            if (parse.IPv4Hdr != null)
            {
                if (IPv4SrcAddr is IPv4Addr ipv4SrcAddr) parse.IPv4Hdr->SrcAddr = ipv4SrcAddr;
                if (IPv4DstAddr is IPv4Addr ipv4DstAddr) parse.IPv4Hdr->DstAddr = ipv4DstAddr;
            }
            if (parse.IPv6Hdr != null)
            {
                if (IPv6SrcAddr is IPv6Addr ipv6SrcAddr) parse.IPv6Hdr->SrcAddr = ipv6SrcAddr;
                if (IPv6DstAddr is IPv6Addr ipv6DstAddr) parse.IPv6Hdr->DstAddr = ipv6DstAddr;
            }
            if (parse.TCPHdr != null)
            {
                if (TCPSrcPort is ushort tcpSrcPort) parse.TCPHdr->SrcPort = tcpSrcPort;
                if (TCPDstPort is ushort tcpDstPort) parse.TCPHdr->DstPort = tcpDstPort;
            }
            if (parse.UDPHdr != null)
            {
                if (UDPSrcPort is ushort udpSrcPort) parse.UDPHdr->SrcPort = udpSrcPort;
                if (UDPDstPort is ushort udpDstPort) parse.UDPHdr->DstPort = udpDstPort;
            }
            WinDivertHelper.CalcChecksums(parse.Packet.Span, ref addr, 0);
        }

        private unsafe void Log(WinDivertParseResult parse, in WinDivertAddress addr)
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
                if (parse.IPv4Hdr != null)
                {
                    l.Add($"IPv4SrcAddr={WinDivertHelper.FormatIPv4Address(parse.IPv4Hdr->SrcAddr)}");
                    l.Add($"IPv4DstAddr={WinDivertHelper.FormatIPv4Address(parse.IPv4Hdr->DstAddr)}");
                }
                if (parse.IPv6Hdr != null)
                {
                    l.Add($"IPv6SrcAddr={WinDivertHelper.FormatIPv6Address(parse.IPv6Hdr->SrcAddr)}");
                    l.Add($"IPv6DstAddr={WinDivertHelper.FormatIPv6Address(parse.IPv6Hdr->DstAddr)}");
                }
                if (parse.TCPHdr != null)
                {
                    l.Add($"TCPSrcPort={WinDivertHelper.Ntoh(parse.TCPHdr->SrcPort)}");
                    l.Add($"TCPDstPort={WinDivertHelper.Ntoh(parse.TCPHdr->DstPort)}");
                }
                if (parse.UDPHdr != null)
                {
                    l.Add($"UDPSrcPort={WinDivertHelper.Ntoh(parse.UDPHdr->SrcPort)}");
                    l.Add($"UDPDstPort={WinDivertHelper.Ntoh(parse.UDPHdr->DstPort)}");
                }
                Logger.WriteLine(string.Join(" ", l));
            }
        }
    }
}
