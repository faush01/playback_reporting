using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace playback_reporting.Data
{
    public class ItemChildStats
    {
        public Dictionary<User, int> Stats { get; set; } = new Dictionary<User, int>();
        public int Total { get; set; }
    }
}
