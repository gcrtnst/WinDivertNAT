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
        public readonly ReadOnlyMemory<byte> Filter;

        private short priority = 0;
        public short Priority
        {
            get => priority;
            init
            {
                if (value is < (-30000) or > 30000) throw new ArgumentOutOfRangeException(nameof(value));
                priority = value;
            }
        }

        private ulong queueLength = 4096;
        public ulong QueueLength
        {
            get => queueLength;
            init
            {
                if (value is < 32 or > 16384) throw new ArgumentOutOfRangeException(nameof(value));
                queueLength = value;
            }
        }

        private ulong queueTime = 2000;
        public ulong QueueTime
        {
            get => queueTime;
            init
            {
                if (value is < 100 or > 16000) throw new ArgumentOutOfRangeException(nameof(value));
                queueTime = value;
            }
        }

        private ulong queueSize = 4194304;
        public ulong QueueSize
        {
            get => queueSize;
            init
            {
                if (value is < 65535 or > 33554432) throw new ArgumentOutOfRangeException(nameof(value));
                queueSize = value;
            }
        }

        private int bufLength = 255;
        public int BufLength
        {
            get => bufLength;
            init
            {
                if (value is < 1 or > 255) throw new ArgumentOutOfRangeException(nameof(value));
                bufLength = value;
            }
        }

        private int bufSize = 131072;
        public int BufSize
        {
            get => bufSize;
            init
            {
                if (value is < 65535 or > 33554432) throw new ArgumentOutOfRangeException(nameof(value));
                bufSize = value;
            }
        }

        public bool Drop { get; init; } = false;
        public bool? Outbound { get; init; } = null;
        public uint? IfIdx { get; init; } = null;
        public uint? SubIfIdx { get; init; } = null;

        private IPv4Addr? ipv4SrcAddr = null;
        public IPv4Addr? IPv4SrcAddr
        {
            get => ipv4SrcAddr;
            init
            {
                ipv4SrcAddr = value;
                checksumFlag = ChecksumFlag();
            }
        }

        private IPv4Addr? ipv4DstAddr = null;
        public IPv4Addr? IPv4DstAddr
        {
            get => ipv4DstAddr;
            init
            {
                ipv4DstAddr = value;
                checksumFlag = ChecksumFlag();
            }
        }

        private IPv6Addr? ipv6SrcAddr = null;
        public IPv6Addr? IPv6SrcAddr
        {
            get => ipv6SrcAddr;
            init
            {
                ipv6SrcAddr = value;
                checksumFlag = ChecksumFlag();
            }
        }

        private IPv6Addr? ipv6DstAddr = null;
        public IPv6Addr? IPv6DstAddr
        {
            get => ipv6DstAddr;
            init
            {
                ipv6DstAddr = value;
                checksumFlag = ChecksumFlag();
            }
        }

        private ushort? tcpSrcPort = null;
        public ushort? TCPSrcPort
        {
            get => GetPortProperty(tcpSrcPort);
            init
            {
                tcpSrcPort = SetPortProperty(value);
                checksumFlag = ChecksumFlag();
            }
        }

        private ushort? tcpDstPort = null;
        public ushort? TCPDstPort
        {
            get => GetPortProperty(tcpDstPort);
            init
            {
                tcpDstPort = SetPortProperty(value);
                checksumFlag = ChecksumFlag();
            }
        }

        private ushort? udpSrcPort = null;
        public ushort? UDPSrcPort
        {
            get => GetPortProperty(udpSrcPort);
            init
            {
                udpSrcPort = SetPortProperty(value);
                checksumFlag = ChecksumFlag();
            }
        }

        private ushort? udpDstPort = null;
        public ushort? UDPDstPort
        {
            get => GetPortProperty(udpDstPort);
            init
            {
                udpDstPort = SetPortProperty(value);
                checksumFlag = ChecksumFlag();
            }
        }

        public TextWriter? Logger { get; init; } = null;

        private WinDivertConstants.WinDivertChecksumFlag checksumFlag;

        public WinDivertNAT(string filter)
        {
            var fobj = WinDivertHelper.CompileFilter(filter, WinDivertConstants.WinDivertLayer.Network);
            Filter = fobj;
            checksumFlag = ChecksumFlag();
        }

        public WinDivertNAT(ReadOnlyMemory<byte> filter)
        {
            Filter = filter;
            checksumFlag = ChecksumFlag();
        }

        public void Run(CancellationToken token)
        {
            var modify = Outbound.HasValue
                || IfIdx.HasValue
                || SubIfIdx.HasValue
                || ipv4SrcAddr.HasValue
                || ipv4DstAddr.HasValue
                || ipv6SrcAddr.HasValue
                || ipv6DstAddr.HasValue
                || tcpSrcPort.HasValue
                || tcpDstPort.HasValue
                || udpSrcPort.HasValue
                || udpDstPort.HasValue;

            if (modify && !Drop) RunNormal(token);
            else if (Logger is not null) RunRecvOnly(token);
            else if (Drop && Logger is null) RunDrop(token);
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
            var flags = WinDivertConstants.WinDivertFlag.RecvOnly;
            if (!Drop) flags |= WinDivertConstants.WinDivertFlag.Sniff;

            using var divert = new WinDivert(Filter.Span, WinDivertConstants.WinDivertLayer.Network, priority, flags)
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
                foreach (var (i, p) in new WinDivertIndexedPacketParser(recv)) Log(p, in addr.Span[i]);
            }
        }

        private void RunDrop(CancellationToken token)
        {
            using var divert = new WinDivert(Filter.Span, WinDivertConstants.WinDivertLayer.Network, priority, WinDivertConstants.WinDivertFlag.Drop | WinDivertConstants.WinDivertFlag.RecvOnly);
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
                if (this.ipv4SrcAddr is IPv4Addr ipv4SrcAddr) p.IPv4Hdr->SrcAddr = ipv4SrcAddr;
                if (this.ipv4DstAddr is IPv4Addr ipv4DstAddr) p.IPv4Hdr->DstAddr = ipv4DstAddr;
            }
            if (p.IPv6Hdr != null)
            {
                if (this.ipv6SrcAddr is IPv6Addr ipv6SrcAddr) p.IPv6Hdr->SrcAddr = ipv6SrcAddr;
                if (this.ipv6DstAddr is IPv6Addr ipv6DstAddr) p.IPv6Hdr->DstAddr = ipv6DstAddr;
            }
            if (p.TCPHdr != null)
            {
                if (this.tcpSrcPort is ushort tcpSrcPort) p.TCPHdr->SrcPort = tcpSrcPort;
                if (this.tcpDstPort is ushort tcpDstPort) p.TCPHdr->DstPort = tcpDstPort;
            }
            if (p.UDPHdr != null)
            {
                if (this.udpSrcPort is ushort udpSrcPort) p.UDPHdr->SrcPort = udpSrcPort;
                if (this.udpDstPort is ushort udpDstPort) p.UDPHdr->DstPort = udpDstPort;
            }
            WinDivertHelper.CalcChecksums(p.Packet.Span, ref addr, checksumFlag);
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
                    l.Add($"IPv4SrcAddr={WinDivertHelper.FormatIPv4Address(p.IPv4Hdr->SrcAddr)}");
                    l.Add($"IPv4DstAddr={WinDivertHelper.FormatIPv4Address(p.IPv4Hdr->DstAddr)}");
                }
                if (p.IPv6Hdr != null)
                {
                    l.Add($"IPv6SrcAddr={WinDivertHelper.FormatIPv6Address(p.IPv6Hdr->SrcAddr)}");
                    l.Add($"IPv6DstAddr={WinDivertHelper.FormatIPv6Address(p.IPv6Hdr->DstAddr)}");
                }
                if (p.TCPHdr != null)
                {
                    l.Add($"TCPSrcPort={WinDivertHelper.Ntoh(p.TCPHdr->SrcPort)}");
                    l.Add($"TCPDstPort={WinDivertHelper.Ntoh(p.TCPHdr->DstPort)}");
                }
                if (p.UDPHdr != null)
                {
                    l.Add($"UDPSrcPort={WinDivertHelper.Ntoh(p.UDPHdr->SrcPort)}");
                    l.Add($"UDPDstPort={WinDivertHelper.Ntoh(p.UDPHdr->DstPort)}");
                }
                Logger.WriteLine(string.Join(" ", l));
            }
        }

        private static ushort? GetPortProperty(ushort? prop)
        {
            return prop is ushort nport
                ? WinDivertHelper.Ntoh(nport)
                : null;
        }

        private static ushort? SetPortProperty(ushort? value)
        {
            return value is ushort hport
                ? WinDivertHelper.Hton(hport)
                : null;
        }

        private WinDivertConstants.WinDivertChecksumFlag ChecksumFlag()
        {
            var flag = WinDivertConstants.WinDivertChecksumFlag.NoICMPv4Checksum | WinDivertConstants.WinDivertChecksumFlag.NoICMPv6Checksum;
            if (!ipv4SrcAddr.HasValue && !ipv4DstAddr.HasValue) flag |= WinDivertConstants.WinDivertChecksumFlag.NoIPv4Checksum;
            if (!tcpSrcPort.HasValue && !tcpDstPort.HasValue) flag |= WinDivertConstants.WinDivertChecksumFlag.NoTCPChecksum;
            if (!udpSrcPort.HasValue && !udpDstPort.HasValue) flag |= WinDivertConstants.WinDivertChecksumFlag.NoUDPChecksum;
            return flag;
        }
    }
}
