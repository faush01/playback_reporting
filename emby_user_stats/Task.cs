using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace emby_user_stats
{
    class Task : IScheduledTask
    {
        private IActivityManager _activity;
        private ILogger _logger;

        public string Name => "User Stats Trim";
        public string Key => "UserUsageStatsTask";
        public string Description => "Runs the user usage stats trim task";
        public string Category => "Statistics";

        public Task(IActivityManager activity, ILogManager logger)
        {
            _logger = logger.GetLogger("UserUsageStats");
            _activity = activity;
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            throw new NotImplementedException();
        }

        public async System.Threading.Tasks.Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {

            await System.Threading.Tasks.Task.Run(() =>
            {
                _logger.Info("User Activity Task Run");

            }, cancellationToken);

        }
    }
}
