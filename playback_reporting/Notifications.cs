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

using Emby.Notifications;
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

        public List<NotificationTypeInfo> GetNotificationTypes(string language)
        {
            var knownTypes = new List<NotificationTypeInfo>
            {
                new NotificationTypeInfo
                {
                     Id = "51fa5550-15e6-493e-8e76-21a544d0dde1",
                     Name = "User Activity Report",
                     CategoryId = "bb04c31d-4752-4470-93b9-a7e7f659e1da",
                     CategoryName = "Playback Reporting"
                },
                new NotificationTypeInfo
                {
                     Id = "80a89810-e7d7-4c41-8c46-d1ef6040b6f9",
                     Name = "New Media Report",
                     CategoryId = "bb04c31d-4752-4470-93b9-a7e7f659e1da",
                     CategoryName = "Playback Reporting"
                }
            };
            return knownTypes;
        }

    }
}
