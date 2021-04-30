﻿/*
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

        public void SetParam(WinDivertConstants.WinDivertParam param, ulong value) => WinDivertLow.WinDivertSetParam(handle, param, value);
        public ulong GetParam(WinDivertConstants.WinDivertParam param) => WinDivertLow.WinDivertGetParam(handle, param);
        public void Shutdown(WinDivertConstants.WinDivertShutdown how) => WinDivertLow.WinDivertShutdown(handle, how);
        public void Dispose() => handle.Dispose();
    }
}
