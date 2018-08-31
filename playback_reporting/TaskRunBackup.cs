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
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace playback_reporting
{
    class TaskRunBackup : IScheduledTask
    {
        private IActivityManager _activity;
        private ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;

        public string Name => "Playback Reporting Run Backup";
        public string Key => "PlaybackHistoryRunBackup";
        public string Description => "Runs the report data backup";
        public string Category => "Playback Reporting";


        public TaskRunBackup(IActivityManager activity, ILogManager logger, IServerConfigurationManager config, IFileSystem fileSystem)
        {
            _logger = logger.GetLogger("PlaybackReporting - TaskCleanDb");
            _activity = activity;
            _config = config;
            _fileSystem = fileSystem;

            _logger.Info("TaskCleanDb Loaded");
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            var trigger = new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerWeekly,
                DayOfWeek = 0,
                TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
            }; //3am on Sunday
            return new[] { trigger };
        }

        public async System.Threading.Tasks.Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            await System.Threading.Tasks.Task.Run(() =>
            {

                BackupManager backup = new BackupManager(_config, _logger, _fileSystem);
                backup.SaveBackup();

            }, cancellationToken);
        }
    }
}
