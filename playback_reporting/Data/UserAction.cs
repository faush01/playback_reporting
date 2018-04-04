using System;
using System.Collections.Generic;
using System.Text;

namespace emby_user_stats.Data
{
    public class UserAction
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string ItemId { get; set; }
        public string ItemType { get; set; }
        public string ActionType { get; set; }
        public DateTime Date { get; set; }
    }
}
