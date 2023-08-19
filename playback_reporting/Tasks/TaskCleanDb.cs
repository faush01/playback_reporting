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
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;
using playback_reporting.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace playback_reporting.Tasks
{
    class TaskCleanDb : IScheduledTask
    {
        private IActivityManager _activity;
        private ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly IServerApplicationHost _appHost;

        private string task_name = "Trim Db";

        public string Name => task_name;
        public string Key => "PlaybackHistoryTrimTask";
        public string Description => "Runs the report history trim task";
        public string Category => "Playback Reporting";

        public TaskCleanDb(IActivityManager activity, ILogManager logger, IServerConfigurationManager config, IFileSystem fileSystem, IServerApplicationHost appHost)
        {
            _logger = logger.GetLogger("PlaybackReporting - TaskCleanDb");
            _activity = activity;
            _config = config;
            _fileSystem = fileSystem;
            _appHost = appHost;
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            var trigger = new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerDaily,
                TimeOfDayTicks = TimeSpan.FromMinutes(5).Ticks
            }; //12:05am
            return new[] { trigger };
        }

        public async System.Threading.Tasks.Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                _logger.Info("Playback Reporting Data Trim");

                ReportPlaybackOptions config = _config.GetReportPlaybackOptions();

                int max_data_age = config.MaxDataAge;

                _logger.Info("MaxDataAge : " + max_data_age);

                if(max_data_age == -1)
                {
                    _logger.Info("Keep data forever, not doing any data cleanup");
                    return;
                }

                ActivityRepository db_repo = new ActivityRepository(_config.ApplicationPaths.DataPath);
                if (max_data_age == 0)
                {
                    _logger.Info("Removing all data");
                    db_repo.DeleteOldData(null);
                }
                else
                {
                    DateTime del_defore = DateTime.Now.AddMonths(max_data_age * -1);
                    db_repo.DeleteOldData(del_defore);
                }
            }, cancellationToken);
        }
    }
}
