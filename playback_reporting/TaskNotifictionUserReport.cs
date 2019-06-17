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
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Tasks;
using playback_reporting.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace playback_reporting
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
            if (VersionCheck.IsVersionValid(_appHost.ApplicationVersion, _appHost.SystemUpdateLevel) == false)
            {
                task_name = task_name + " (disabled)";
                _logger.Info("ERROR : Plugin not compatible with this server version");
            }

            _logger.Info("UserActivityReportNotification Loaded");
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            var trigger = new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerDaily,
                TimeOfDayTicks = TimeSpan.FromHours(0).Ticks
            }; //12am daily
            return new[] { trigger };
        }

        public async System.Threading.Tasks.Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (VersionCheck.IsVersionValid(_appHost.ApplicationVersion, _appHost.SystemUpdateLevel) == false)
            {
                _logger.Info("ERROR : Plugin not compatible with this server version");
                return;
            }

            Dictionary<string, string> user_map = new Dictionary<string, string>();
            foreach (var user in _userManager.Users)
            {
                user_map.Add(user.Id.ToString("N"), user.Name);
            }

            Data.IActivityRepository repository = new ActivityRepository(_logger, _config.ApplicationPaths, _fileSystem);

            string sql = "";
            sql += "SELECT UserId, ItemType, ItemName, SUM(PlayDuration - PauseDuration) AS PlayTime ";
            sql += "FROM PlaybackActivity ";
            sql += "WHERE DateCreated > datetime('now', '-1 day') ";
            sql += "GROUP BY UserId, ItemType, ItemName";

            List<string> cols = new List<string>();
            List<List<Object>> results = new List<List<object>>();
            repository.RunCustomQuery(sql, cols, results);

            string message = "User activity in the last 24 hours\r\n";

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
                    string user_name = user_id;
                    if (user_map.ContainsKey(user_id))
                    {
                        user_name = user_map[user_id];
                    }
                    message += "\r\n" + user_name + "\r\n";
                    last_user = user_id;
                }
                item_count++;
                message += " - (" + item_type + ") " + item_name + " (" + play_time_string + ")\r\n";
            }

            if (item_count > 0)
            {
                var notification = new NotificationRequest
                {
                    NotificationType = "UserActivityReportNotification",
                    Date = DateTime.UtcNow,
                    Name = "User Activity Report Notification",
                    Description = message
                };
                await _notificationManager.SendNotification(notification, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
