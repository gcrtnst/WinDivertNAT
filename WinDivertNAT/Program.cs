/*
 * Program.cs
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

using Mono.Options;
using System;
using System.Collections.Generic;
using System.Threading;

namespace WinDivertNAT
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            var filter = "";
            var priority = (short)0;
            var queueLength = (ulong)4096;
            var queueTime = (ulong)2000;
            var queueSize = (ulong)4194304;
            var bufLength = 1;
            var bufSize = 40 + 0xFFFF;
            var drop = false;
            var outbound = (bool?)null;
            var ifIdx = (uint?)null;
            var subIfIdx = (uint?)null;
            var ipv4SrcAddr = (IPv4Addr?)null;
            var ipv4DstAddr = (IPv4Addr?)null;
            var ipv6SrcAddr = (IPv6Addr?)null;
            var ipv6DstAddr = (IPv6Addr?)null;
            var tcpSrcPort = (ushort?)null;
            var tcpDstPort = (ushort?)null;
            var udpSrcPort = (ushort?)null;
            var udpDstPort = (ushort?)null;
            var log = false;
            var help = false;
            var p = new OptionSet()
            {
                { "f|filter=", (string v) => filter = v },
                { "p|priority=", (short v) => priority = OptionBoundsCheck(v, (short)-30000, (short)30000, "--priority") },
                { "queue-length=", (ulong v) => queueLength = OptionBoundsCheck(v, (ulong)32, (ulong)16384, "--queue-length") },
                { "queue-time=", (ulong v) => queueTime = OptionBoundsCheck(v, (ulong)100, (ulong)16000, "--queue-time") },
                { "queue-size=", (ulong v) => queueSize = OptionBoundsCheck(v, (ulong)(40 + 0xFFFF), (ulong)33554432, "--queue-size") },
                { "buf-length=", (int v) => bufLength = OptionBoundsCheck(v, 1, 0xFF, "--buf-length") },
                { "buf-size=", (int v) => bufSize = OptionBoundsCheck(v, 40 + 0xFFFF, 33554432, "--buf-size") },
                { "drop", (string v) => drop = v is not null },
                { "outbound", (string v) => outbound = v is not null },
                { "ifidx=", (uint v) => ifIdx = v },
                { "subifidx=", (uint v) => subIfIdx = v },
                { "ipv4-src-addr=", (string v) => ipv4SrcAddr = OptionIPv4Addr(v, "--ipv4-src-addr") },
                { "ipv4-dst-addr=", (string v) => ipv4DstAddr = OptionIPv4Addr(v, "--ipv4-dst-addr") },
                { "ipv6-src-addr=", (string v) => ipv6SrcAddr = OptionIPv6Addr(v, "--ipv6-src-addr") },
                { "ipv6-dst-addr=", (string v) => ipv6DstAddr = OptionIPv6Addr(v, "--ipv6-dst-addr") },
                { "tcp-src-port=", (ushort v) => tcpSrcPort = v },
                { "tcp-dst-port=", (ushort v) => tcpDstPort = v },
                { "udp-src-port=", (ushort v) => udpSrcPort = v },
                { "udp-dst-port=", (ushort v) => udpDstPort = v },
                { "l|log", (string v) => log = v is not null },
                { "h|?|help", (string v) => help = v is not null },
            };

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Error.WriteLine($"{AppDomain.CurrentDomain.FriendlyName}: {e.Message}");
                return 1;
            }
            if (help)
            {
                Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [OPTIONS]+");
                Console.WriteLine();
                Console.WriteLine("Options:");
                p.WriteOptionDescriptions(Console.Out);
                return 0;
            }
            if (filter == "")
            {
                Console.Error.WriteLine($"{AppDomain.CurrentDomain.FriendlyName}: Option --filter is required.");
                return 1;
            }
            if (extra.Count > 0)
            {
                Console.Error.WriteLine($"{AppDomain.CurrentDomain.FriendlyName}: Unrecognized argument '{extra[0]}'.");
                return 1;
            }

            var nat = new WinDivertNAT(filter)
            {
                Priority = priority,
                QueueLength = queueLength,
                QueueTime = queueTime,
                QueueSize = queueSize,
                BufLength = bufLength,
                BufSize = bufSize,
                Drop = drop,
                Outbound = outbound,
                IfIdx = ifIdx,
                SubIfIdx = subIfIdx,
                IPv4SrcAddr = ipv4SrcAddr,
                IPv4DstAddr = ipv4DstAddr,
                IPv6SrcAddr = ipv6SrcAddr,
                IPv6DstAddr = ipv6DstAddr,
                TCPSrcPort = tcpSrcPort,
                TCPDstPort = tcpDstPort,
                UDPSrcPort = udpSrcPort,
                UDPDstPort = udpDstPort,
                Logger = log ? Console.Out : null,
            };
            using var cancel = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                cancel.Cancel();
                e.Cancel = true;
            };
            try
            {
                nat.Run(cancel.Token);
            }
            catch (OperationCanceledException) { }
            return 0;
        }

        private static T OptionBoundsCheck<T>(T v, T min, T max, string optionName) where T : IComparable<T>
        {
            if (v.CompareTo(min) < 0 || v.CompareTo(max) > 0) throw new OptionException($"Value must be between {min} and {max} for option '{optionName}'.", optionName);
            return v;
        }

        private static IPv4Addr OptionIPv4Addr(string v, string optionName)
        {
            IPv4Addr ipv4Addr;
            try
            {
                ipv4Addr = IPv4Addr.Parse(v);
            }
            catch (ArgumentException e)
            {
                throw new OptionException($"Invalid IPv4 address '{v}' for option '{optionName}'.", optionName, e);
            }
            return ipv4Addr;
        }

        private static IPv6Addr OptionIPv6Addr(string v, string optionName)
        {
            IPv6Addr ipv6Addr;
            try
            {
                ipv6Addr = IPv6Addr.Parse(v);
            }
            catch (ArgumentException e)
            {
                throw new OptionException($"Invalid IPv6 address '{v}' for option '{optionName}'.", optionName, e);
            }
            return ipv6Addr;
        }
    }
}
