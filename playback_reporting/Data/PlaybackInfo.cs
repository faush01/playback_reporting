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
using System.Collections.Generic;
using System.Text;

namespace playback_reporting.Data
{
    public class PlaybackInfo
    {
        public string Key { set; get;  }
        public DateTime Date { get; set; }
        
        public string UserId { get; set; }
        public string ItemId { get; set; }
        public string ItemType { get; set; }
        public string ItemName { get; set; }
        public string PlaybackMethod { get; set; }
        public string TranscodeReasons { get; set; }
        public string ClientName { get; set; }
        public string DeviceName { get; set; }
        public string RemoteAddress {  get; set; }
        public int PlaybackDuration { get; set; } = 0;
        public DateTime? LastPauseTime { get; set; }
        public int PausedDuration { get; set; } = 0;
        public bool StartupSaved { get; set; } = false;

    }
}
