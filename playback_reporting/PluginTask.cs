using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace playback_reporting
{
    class PluginTask : IScheduledTask
    {
        private IActivityManager _activity;
        private ILogger _logger;

        public string Name => "Playback History Trim";
        public string Key => "PlaybackHistoryTrimTask";
        public string Description => "Runs the report history trim task";
        public string Category => "Playback Reporting";
        private static PluginConfiguration PluginConfiguration => Plugin.Instance.Configuration;

        public PluginTask(IActivityManager activity, ILogManager logger)
        {
            _logger = logger.GetLogger("PlaybackReporting");
            _activity = activity;
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
                _logger.Info("Playback History Trim");

                int max_data_age = PluginConfiguration.MaxDataAge;

                _logger.Info("MaxDataAge : " + max_data_age);

            }, cancellationToken);

        }
    }
}
