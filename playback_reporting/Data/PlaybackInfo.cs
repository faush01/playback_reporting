using System;
using System.Collections.Generic;
using System.Text;

namespace playback_reporting.Data
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
