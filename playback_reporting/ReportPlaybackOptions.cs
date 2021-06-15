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

namespace playback_reporting
{
    public class ReportPlaybackOptions
    {
        public int MaxDataAge { set; get; } = 3;
        public string BackupPath { set; get; }
        public int MaxBackupFiles { set; get; } = 5;
        public DateTime LastNewMediaCheck { set; get; } = DateTime.Now.AddDays(-1);
        public DateTime LastUserActivityCheck { set; get; } = DateTime.Now.AddDays(-1);
        public List<PlaylistDetails> ActivityPlaylists { set; get; } = new List<PlaylistDetails>();
        public HashSet<string> ColourPalette { set; get; } = new HashSet<string>();
        public List<CustomQueryDetails> CustomQueries { set; get; } = new List<CustomQueryDetails>();
    }

    public class PlaylistDetails
    {
        public string Name { set; get; }
        public string Type { set; get; }
        public int Days { set; get; }
        public int Size { set; get; }
    }

    public class CustomQueryDetails
    {
        public int Id { set; get; }
        public string Name { set; get; }
        public string Query { set; get; }
        public bool ReplaceName { set; get; }
        public int ChartType { set; get; }
        public string ChartLabelColumn { set; get; }
        public string ChartDataCloumn { set; get; }
    }
}
