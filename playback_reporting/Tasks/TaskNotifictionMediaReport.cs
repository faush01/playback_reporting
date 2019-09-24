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
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Tasks;
using playback_reporting.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace playback_reporting.Tasks
{
    class TaskNotifictionMediaReport : IScheduledTask
    {
        private IActivityManager _activity;
        private ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IServerApplicationHost _appHost;
        private readonly INotificationManager _notificationManager;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserViewManager _userViewManager;

        private string task_name = "New Media Notification";

        public string Name => task_name;
        public string Key => "NewMediaNotification";
        public string Description => "Send new media report notification";
        public string Category => "Playback Reporting";

        public TaskNotifictionMediaReport(IActivityManager activity, 
            ILogManager logger, 
            IServerConfigurationManager config, 
            IFileSystem fileSystem, 
            IServerApplicationHost appHost,
            INotificationManager notificationManager,
            IUserManager userManager,
            ILibraryManager libraryManager,
            IUserViewManager userViewManager)
        {
            _logger = logger.GetLogger("NewMediaReportNotification - TaskNotifictionReport");
            _activity = activity;
            _config = config;
            _fileSystem = fileSystem;
            _notificationManager = notificationManager;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _userViewManager = userViewManager;
            _appHost = appHost;

            if (VersionCheck.IsVersionValid(_appHost.ApplicationVersion, _appHost.SystemUpdateLevel) == false)
            {
                _logger.Info("ERROR : Plugin not compatible with this server version");
                throw new NotImplementedException("This task is not available on this version of Emby");
            }

            _logger.Info("NewMediaReportNotification Loaded");
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            var trigger = new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerDaily,
                TimeOfDayTicks = TimeSpan.FromHours(6).Ticks
            }; //6am daily
            return new[] { trigger };
        }

        public async System.Threading.Tasks.Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (VersionCheck.IsVersionValid(_appHost.ApplicationVersion, _appHost.SystemUpdateLevel) == false)
            {
                _logger.Info("ERROR : Plugin not compatible with this server version");
                return;
            }

            string message = "New media added in the last 24 hours\r\n\r\n";

            UserViewQuery view_query = new UserViewQuery();
            view_query.IncludeExternalContent = false;
            view_query.IncludeHidden = false;
            Folder[] views = _userViewManager.GetUserViews(view_query);
            int added_count = 0;

            foreach (Folder folder in views)
            {
                _logger.Info("Checking for new items in  : " + folder.ToString());

                InternalItemsQuery query = new InternalItemsQuery();
                query.IncludeItemTypes = new string[] {"Movie", "Episode"};
                query.Parent = folder;
                query.Recursive = true;
                query.IsVirtualItem = false;
                var sort = new (string, SortOrder)[1] { ("DateCreated", SortOrder.Descending) };
                query.OrderBy = sort;

                BaseItem[] results = _libraryManager.GetItemList(query, false);
                DateTime cutoff = DateTime.Now.AddDays(-1);
                _logger.Info("Cutoff DateTime for new items : " + cutoff.ToString("yyyy-MM-dd HH:mm:ss zzz"));
                int view_added_count = 0;
                string view_message_data = folder.Name + "\r\n";

                foreach(BaseItem item in results)
                {
                    string id = item.InternalId.ToString();
                    string name = item.Name;
                    string type = item.GetType().Name;
                    _logger.Info("Recently added item : (" + id + ") - (" + item.DateCreated.DateTime.ToString("yyyy-MM-dd HH:mm:ss zzz") + ")");

                    if (item.DateCreated.DateTime < cutoff)
                    {
                        break;
                    }
                    view_added_count++;

                    _logger.Info("Adding Item : (" + id + ")");

                    if (typeof(Episode).Equals(item.GetType()))
                    {
                        Episode epp = item as Episode;
                        string series = epp.SeriesName;
                        string epp_number = string.Format("{0:D2}x{1:D2}", epp.ParentIndexNumber, epp.IndexNumber);

                        view_message_data += " - (" + type + ") " + series + " - " + epp_number + " - " + name + "\r\n";
                    }
                    else
                    {
                        view_message_data += " - (" + type + ") " + name + "\r\n";
                    }
                }

                if (view_added_count > 0)
                {
                    message += view_message_data + "\r\n";
                }

                added_count += view_added_count;
            }

            _logger.Info("Added Item Notification Message : ItemCount : " + added_count + "\r\n" + message + "\r\n");

            if (added_count > 0)
            {
                var notification = new NotificationRequest
                {
                    NotificationType = "NewMediaReportNotification",
                    Date = DateTime.UtcNow,
                    Name = "New Media Report Notification",
                    Description = message
                };
                await _notificationManager.SendNotification(notification, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
