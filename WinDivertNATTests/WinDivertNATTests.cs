/*
 * WinDivertNATTests.cs
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace WinDivertNATTests
{
    [TestClass]
    public class WinDivertNATTests
    {
        private const int port1 = 52149;
        private const int port2 = 52150;
        private const int port3 = 52151;

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task Run_Modify(bool log)
        {
            var logger = (TextWriter?)null;
            if (log) logger = new StringWriter();

            using var udp1 = new UdpClient(new IPEndPoint(IPAddress.Loopback, port1));
            udp1.Connect(IPAddress.Loopback, port2);

            using var udp2 = new UdpClient(new IPEndPoint(IPAddress.Loopback, port2));
            udp2.Connect(IPAddress.Loopback, port1);
            udp2.Client.ReceiveTimeout = 250;

            using var udp3 = new UdpClient(new IPEndPoint(IPAddress.Loopback, port3));
            udp3.Connect(IPAddress.Loopback, port1);
            udp3.Client.ReceiveTimeout = 250;

            var nat = new WinDivertNAT.WinDivertNAT($"udp.SrcPort == {port1} and udp.DstPort == {port2} and loopback")
            {
                UDPDstPort = port3,
                Logger = logger,
            };
            using var cancel = new CancellationTokenSource();
            using var task = Task.Run(() => nat.Run(cancel.Token));
            await Task.Delay(250);

            var remoteEP = new IPEndPoint(IPAddress.Any, 0);
            _ = udp1.Send(new byte[1], 1);
            var e = Assert.ThrowsException<SocketException>(() => udp2.Receive(ref remoteEP));
            Assert.AreEqual(SocketError.TimedOut, e.SocketErrorCode);
            _ = udp3.Receive(ref remoteEP);

            cancel.Cancel();
            _ = await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => task);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task Run_Drop(bool modify, bool log)
        {
            var outbound = (bool?)null;
            if (modify) outbound = true;
            var logger = (TextWriter?)null;
            if (log) logger = new StringWriter();

            using var udp1 = new UdpClient(new IPEndPoint(IPAddress.Loopback, port1));
            udp1.Connect(IPAddress.Loopback, port2);

            using var udp2 = new UdpClient(new IPEndPoint(IPAddress.Loopback, port2));
            udp2.Connect(IPAddress.Loopback, port1);
            udp2.Client.ReceiveTimeout = 250;

            var nat = new WinDivertNAT.WinDivertNAT($"udp.SrcPort == {port1} and udp.DstPort == {port2} and loopback")
            {
                Outbound = outbound,
                Drop = true,
                Logger = logger,
            };
            using var cancel = new CancellationTokenSource();
            using var task = Task.Run(() => nat.Run(cancel.Token));
            await Task.Delay(250);

            var remoteEP = new IPEndPoint(IPAddress.Any, 0);
            _ = udp1.Send(new byte[1], 1);
            var e = Assert.ThrowsException<SocketException>(() => udp2.Receive(ref remoteEP));
            Assert.AreEqual(SocketError.TimedOut, e.SocketErrorCode);

            cancel.Cancel();
            _ = await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => task);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task Run_NoDrop(bool modify, bool log)
        {
            var outbound = (bool?)null;
            if (modify) outbound = true;
            var logger = (TextWriter?)null;
            if (log) logger = new StringWriter();

            using var udp1 = new UdpClient(new IPEndPoint(IPAddress.Loopback, port1));
            udp1.Connect(IPAddress.Loopback, port2);

            using var udp2 = new UdpClient(new IPEndPoint(IPAddress.Loopback, port2));
            udp2.Connect(IPAddress.Loopback, port1);
            udp2.Client.ReceiveTimeout = 250;

            var nat = new WinDivertNAT.WinDivertNAT($"udp.SrcPort == {port1} and udp.DstPort == {port2} and loopback")
            {
                Outbound = outbound,
                Drop = false,
                Logger = logger,
            };
            using var cancel = new CancellationTokenSource();
            using var task = Task.Run(() => nat.Run(cancel.Token));
            await Task.Delay(250);

            var remoteEP = new IPEndPoint(IPAddress.Any, 0);
            _ = udp1.Send(new byte[1], 1);
            _ = udp2.Receive(ref remoteEP);

            cancel.Cancel();
            _ = await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => task);
        }

        [TestMethod]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        [DataRow(true, true)]
        public async Task Run_Log(bool modify, bool drop)
        {
            var outbound = (bool?)null;
            if (modify) outbound = true;
            var logger = new StringWriter();

            using var udp1 = new UdpClient(new IPEndPoint(IPAddress.Loopback, port1));
            udp1.Connect(IPAddress.Loopback, port2);

            using var udp2 = new UdpClient(new IPEndPoint(IPAddress.Loopback, port2));
            udp2.Connect(IPAddress.Loopback, port1);

            var nat = new WinDivertNAT.WinDivertNAT($"udp.SrcPort == {port1} and udp.DstPort == {port2} and loopback")
            {
                Outbound = outbound,
                Drop = drop,
                Logger = logger,
            };
            using var cancel = new CancellationTokenSource();
            using var task = Task.Run(() => nat.Run(cancel.Token));
            await Task.Delay(250);

            _ = udp1.Send(new byte[1], 1);
            await Task.Delay(250);

            cancel.Cancel();
            _ = await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => task);
            Assert.IsTrue(logger.GetStringBuilder().Length > 0);
        }

        [TestMethod]
        [DataRow(false, false, false)]
        [DataRow(false, false, true)]
        [DataRow(false, true, false)]
        [DataRow(false, true, true)]
        [DataRow(true, false, false)]
        [DataRow(true, false, true)]
        [DataRow(true, true, false)]
        [DataRow(true, true, true)]
        public async Task Run_WaitForCancellation(bool modify, bool drop, bool log)
        {
            var outbound = (bool?)null;
            if (modify) outbound = true;
            var logger = (TextWriter?)null;
            if (log) logger = new StringWriter();

            var nat = new WinDivertNAT.WinDivertNAT("false")
            {
                Outbound = outbound,
                Drop = drop,
                Logger = logger,
            };
            using var cancel = new CancellationTokenSource();
            using var task = Task.Run(() => nat.Run(cancel.Token));
            Assert.IsTrue(await Task.WhenAny(task, Task.Delay(250)) != task);
            cancel.Cancel();
            _ = await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => task);
        }
    }
}
