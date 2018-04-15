using System;
using System.Collections.Generic;
using System.Text;

namespace playback_reporting
{
    public class ReportPlaybackOptions
    {
        public int MaxDataAge { set; get; } = 3;
        public string BackupPath { set; get; }

    }
}
