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

using MediaBrowser.Controller.Configuration;
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

namespace playback_reporting
{
    class TaskCleanDb : IScheduledTask
    {
        private IActivityManager _activity;
        private ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;

        public string Name => "Playback Reporting Trim Db";
        public string Key => "PlaybackHistoryTrimTask";
        public string Description => "Runs the report history trim task";
        public string Category => "Playback Reporting";

        private playback_reporting.Data.IActivityRepository Repository;

        public TaskCleanDb(IActivityManager activity, ILogManager logger, IServerConfigurationManager config, IFileSystem fileSystem)
        {
            _logger = logger.GetLogger("PlaybackReporting - TaskCleanDb");
            _activity = activity;
            _config = config;
            _fileSystem = fileSystem;

            _logger.Info("TaskCleanDb Loaded");
            var repo = new ActivityRepository(_logger, _config.ApplicationPaths, _fileSystem);
            //repo.Initialize();
            Repository = repo;
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            var trigger = new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerDaily,
                TimeOfDayTicks = TimeSpan.FromHours(0).Ticks
            }; //12am
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
                else if(max_data_age == 0)
                {
                    _logger.Info("Removing all data");
                    Repository.DeleteOldData(null);
                }
                else
                {
                    DateTime del_defore = DateTime.Now.AddMonths(max_data_age * -1);
                    Repository.DeleteOldData(del_defore);
                }
            }, cancellationToken);
        }
    }
}
