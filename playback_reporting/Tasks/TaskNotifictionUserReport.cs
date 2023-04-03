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

using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Tasks;
using playback_reporting.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace playback_reporting.Tasks
{
    class TaskNotifictionUserReport : IScheduledTask
    {
        private IActivityManager _activity;
        private ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IServerApplicationHost _appHost;
        private readonly INotificationManager _notificationManager;
        private readonly IUserManager _userManager;

        private string task_name = "User Activity Notification";

        public string Name => task_name;
        public string Key => "UserActivityReportNotification";
        public string Description => "Send user activity report notification";
        public string Category => "Playback Reporting";

        public TaskNotifictionUserReport(IActivityManager activity, 
            ILogManager logger, 
            IServerConfigurationManager config, 
            IFileSystem fileSystem, 
            IServerApplicationHost appHost,
            INotificationManager notificationManager,
            IUserManager userManager)
        {
            _logger = logger.GetLogger("UserActivityReportNotification - TaskNotifictionReport");
            _activity = activity;
            _config = config;
            _fileSystem = fileSystem;
            _notificationManager = notificationManager;
            _userManager = userManager;
            _appHost = appHost;

            _logger.Info("UserActivityReportNotification Loaded");
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            var trigger = new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerDaily,
                TimeOfDayTicks = TimeSpan.FromMinutes(20).Ticks
            }; //12:20am daily
            return new[] { trigger };
        }

        public async System.Threading.Tasks.Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            UserQuery user_query = new UserQuery();
            Dictionary<string, string> user_map = new Dictionary<string, string>();
            foreach (User user in _userManager.GetUsers(user_query).Items)
            {
                string user_id = user.Id.ToString("N");
                string user_name = user.Name;
                if (!string.IsNullOrEmpty(user_id) && !string.IsNullOrEmpty(user_name))
                {
                    user_map.Add(user.Id.ToString("N"), user.Name);
                }
            }

            ActivityRepository repository = new ActivityRepository(_logger, _config.ApplicationPaths, _fileSystem);
            ReportPlaybackOptions config = _config.GetReportPlaybackOptions();

            DateTime last_checked = config.LastUserActivityCheck;
            string date_from = last_checked.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture);

            string sql = "";
            sql += "SELECT UserId, ItemType, ItemName, SUM(PlayDuration - PauseDuration) AS PlayTime ";
            sql += "FROM PlaybackActivity ";
            sql += "WHERE DateCreated > '" + date_from + "' "; // datetime('now', '-1 day', 'localtime') ";
            sql += "AND UserId not IN (select UserId from UserList) ";

            if (config.IgnoreSmallerThan > 0)
            {
                sql += "AND (PlayDuration - PauseDuration) > " + config.IgnoreSmallerThan + " ";
            }

            sql += "GROUP BY UserId, ItemType, ItemName";

            _logger.Info("Activity Query : " + sql);

            List<string> cols = new List<string>();
            List<List<Object>> results = new List<List<object>>();
            repository.RunCustomQuery(sql, cols, results);

            TimeSpan since_last = DateTime.Now - last_checked;
            _logger.Info("Cutoff DateTime for new items - date: " + date_from + " ago: " + since_last);

            string since_last_string = string.Format("{0}{1}{2}",
                since_last.Duration().Days > 0 ? string.Format("{0:0} day{1} ", since_last.Days, since_last.Days == 1 ? String.Empty : "s") : string.Empty,
                since_last.Duration().Hours > 0 ? string.Format("{0:0} hour{1} ", since_last.Hours, since_last.Hours == 1 ? String.Empty : "s") : string.Empty,
                since_last.Duration().Minutes > 0 ? string.Format("{0:0} minute{1} ", since_last.Minutes, since_last.Minutes == 1 ? String.Empty : "s") : string.Empty);
            if (string.IsNullOrEmpty(since_last_string))
            {
                since_last_string = "0 minutes";
            }
            string message = "User activity since last check " + since_last_string + "ago.\r\n";

            int item_count = 0;
            string last_user = "";
            foreach (List<Object> row in results)
            {
                string user_id = (string)row[0];
                string item_type = (string)row[1];
                string item_name = (string)row[2];
                int item_playtime = int.Parse((string)row[3]);
                TimeSpan play_span = TimeSpan.FromSeconds(item_playtime);
                string play_time_string = string.Format("{0:D2}:{1:D2}:{2:D2}",
                    play_span.Hours,
                    play_span.Minutes,
                    play_span.Seconds);

                if (play_span.Days > 0)
                {
                    play_time_string = string.Format("{0}.{1:D2}:{2:D2}:{3:D2}",
                        play_span.Days,
                        play_span.Hours,
                        play_span.Minutes,
                        play_span.Seconds);
                }

                if (last_user != user_id)
                {
                    string user_name = "Unknown:" + user_id;
                    if (!string.IsNullOrEmpty(user_id) && user_map.ContainsKey(user_id))
                    {
                        user_name = user_map[user_id];
                    }
                    message += "\r\n" + user_name + "\r\n";
                    last_user = user_id;
                }
                item_count++;
                message += " - (" + item_type + ") " + item_name + " (" + play_time_string + ")\r\n";
            }

            _logger.Info("User activity Message : ItemCount : " + item_count);
            //_logger.Info(message);

            if (item_count > 0)
            {
                /*
                var notification = new NotificationRequest
                {
                    NotificationType = "UserActivityReportNotification",
                    Date = DateTime.UtcNow,
                    Name = "User Activity Report Notification",
                    Description = message
                };
                await _notificationManager.SendNotification(notification, CancellationToken.None).ConfigureAwait(false);
                */

                Emby.Notifications.NotificationRequest notify_req = new Emby.Notifications.NotificationRequest();
                notify_req.Date = DateTime.UtcNow;
                notify_req.Description = message;
                notify_req.CancellationToken = CancellationToken.None;
                notify_req.EventId = "51fa5550-15e6-493e-8e76-21a544d0dde1";
                notify_req.Title = "User Activity Report Notification";

                await Task.Run(() => _notificationManager.SendNotification(notify_req));
            }

            config.LastUserActivityCheck = DateTime.Now;
            _config.SaveReportPlaybackOptions(config);
        }
    }
}
