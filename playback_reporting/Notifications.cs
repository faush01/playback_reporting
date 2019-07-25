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
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace playback_reporting
{
    public class MyNotifications : INotificationTypeFactory
    {
        private readonly IServerApplicationHost _appHost;

        public MyNotifications(IServerApplicationHost appHost)
        {
            _appHost = appHost;
        }

        public IEnumerable<NotificationTypeInfo> GetNotificationTypes()
        {
            if (VersionCheck.IsVersionValid(_appHost.ApplicationVersion, _appHost.SystemUpdateLevel) == false)
            {
                return new List<NotificationTypeInfo>();
            }

            var knownTypes = new List<NotificationTypeInfo>
            {
                new NotificationTypeInfo
                {
                     Type = "UserActivityReportNotification",
                     Name = "User Activity Report",
                     Category = "Playback Reporting",
                     Enabled = true,
                     IsBasedOnUserEvent = false
                },
                new NotificationTypeInfo
                {
                     Type = "NewMediaReportNotification",
                     Name = "New Media Report",
                     Category = "Playback Reporting",
                     Enabled = true,
                     IsBasedOnUserEvent = false
                }
            };
            return knownTypes;
        }
    }
}
