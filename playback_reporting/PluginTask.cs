using MediaBrowser.Controller.Configuration;
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
        private readonly IServerConfigurationManager _config;

        public string Name => "Playback History Trim";
        public string Key => "PlaybackHistoryTrimTask";
        public string Description => "Runs the report history trim task";
        public string Category => "Playback Reporting";

        public PluginTask(IActivityManager activity, ILogManager logger, IServerConfigurationManager config)
        {
            _logger = logger.GetLogger("PlaybackReporting");
            _activity = activity;
            _config = config;
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

                ReportPlaybackOptions config = _config.GetReportPlaybackOptions();

                int max_data_age = config.MaxDataAge;

                _logger.Info("MaxDataAge : " + max_data_age);

            }, cancellationToken);

        }
    }
}
