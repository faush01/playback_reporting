/*
Copyright(C) 2018

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see<http://www.gnu.org/licenses/>.
*/

using System;

namespace Jellyfin.Plugin.PlaybackReporting.Data
{
    public class PlaybackInfo
    {
        public string Id { get; set; }
        public DateTime Date { get; set; }
        public string UserId { get; set; }
        public string ItemId { get; set; }
        public string ItemType { get; set; }
        public string ItemName { get; set; }
        public string PlaybackMethod { get; set; }
        public string ClientName { get; set; }
        public string DeviceName { get; set; }
        public int PlaybackDuration { get; set; } = 0;

    }
}
