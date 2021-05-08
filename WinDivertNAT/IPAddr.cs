/*
 * IPAddr.cs
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

namespace WinDivertNAT
{
    [StructLayout(LayoutKind.Sequential)]
    public struct IPv4Addr : IEquatable<IPv4Addr>
    {
        internal uint Raw;

        public static IPv4Addr Parse(string addrStr) => WinDivertLow.WinDivertHelperParseIPv4Address(addrStr);
        public override string ToString() => WinDivertLow.WinDivertHelperFormatIPv4Address(this);

        public static bool operator ==(IPv4Addr left, IPv4Addr right) => left.Equals(right);
        public static bool operator !=(IPv4Addr left, IPv4Addr right) => !left.Equals(right);

        public bool Equals(IPv4Addr addr) => Raw == addr.Raw;

        public override bool Equals(object? obj)
        {
            if (obj is IPv4Addr ipv4Addr) return Equals(ipv4Addr);
            return base.Equals(obj);
        }

        public override int GetHashCode() => base.GetHashCode();
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct IPv6Addr : IEquatable<IPv6Addr>
    {
        internal fixed uint Raw[4];

        public static IPv6Addr Parse(string addrStr) => WinDivertLow.WinDivertHelperParseIPv6Address(addrStr);
        public override string ToString() => WinDivertLow.WinDivertHelperFormatIPv6Address(this);

        public static bool operator ==(IPv6Addr left, IPv6Addr right) => left.Equals(right);
        public static bool operator !=(IPv6Addr left, IPv6Addr right) => !left.Equals(right);

        public bool Equals(IPv6Addr addr)
        {
            return Raw[0] == addr.Raw[0]
                && Raw[1] == addr.Raw[1]
                && Raw[2] == addr.Raw[2]
                && Raw[3] == addr.Raw[3];
        }

        public override bool Equals(object? obj)
        {
            if (obj is IPv6Addr ipv6Addr) return Equals(ipv6Addr);
            return base.Equals(obj);
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
