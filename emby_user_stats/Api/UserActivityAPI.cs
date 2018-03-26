using emby_user_stats.Data;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace emby_user_stats.Api
{
    // http://localhost:8096/emby/user_usage_stats/4c0ea7608f3a41629a0a43a2f23fbb4c/2018-03-23/Activity

    [Route("/user_usage_stats/{UserID}/{StartDate}/Activity", "GET", Summary = "Gets activity for {USER} from {StartDate} formatted as yyyy-MM-dd")]
    public class GetUserActivity : IReturn<ReportDayUsage>
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "StartDate", Description = "UTC DateTime, Format yyyy-MM-dd", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "filter", Description = "Comma separated list of Collection Types to filter (movies,tvshows,music,musicvideos,boxsets", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]

        public string UserID { get; set; }
        public string StartDate { get; set; }
        public string filter { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/30/PlayActivity
    [Route("/user_usage_stats/{NumberOfDays}/PlayActivity", "GET", Summary = "Gets play activity for number of days")]
    public class GetUsageStats : IReturn<ReportDayUsage>
    {
        [ApiMember(Name = "NumberOfDays", Description = "Number of Days", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "GET")]
        public int NumberOfDays { get; set; }
    }

    public class UserActivityAPI : IService
    {

        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _config;
        private readonly IUserManager _userManager;

        private IUserStatsRepository Repository;

        public UserActivityAPI(ILogger logger, 
            IFileSystem fileSystem,
            IServerConfigurationManager config,
            IJsonSerializer jsonSerializer,
            IUserManager userManager)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            _config = config;
            _userManager = userManager;

            var repo = new UserStatsRepository(_logger, _config.ApplicationPaths, _fileSystem);
            repo.Initialize();
            Repository = repo;
        }

        public object Get(GetUserActivity activity)
        {
            var results = Repository.GetUsageForUser(activity.StartDate, activity.UserID);
            return results;
        }

        public object Get(GetUsageStats activity)
        {
            Dictionary<String, Dictionary<string, int>> results = Repository.GetUsageForDays(activity.NumberOfDays);

            List<Dictionary<string, object>> user_usage_data = new List<Dictionary<string, object>>();
            foreach (string user_id in results.Keys)
            {
                Dictionary<string, int> user_usage = results[user_id];

                // fill in missing dates for time period
                SortedDictionary<string, int> userUsageByDate = new SortedDictionary<string, int>();
                DateTime from_date = DateTime.Now.Subtract(new TimeSpan(activity.NumberOfDays, 0, 0, 0));
                while(from_date < DateTime.Now)
                {
                    string date_string = from_date.ToString("yyyy-MM-dd");
                    if(user_usage.ContainsKey(date_string) == false)
                    {
                        userUsageByDate.Add(date_string, 0);
                    }
                    else
                    {
                        userUsageByDate.Add(date_string, user_usage[date_string]);
                    }

                    from_date = from_date.Add(new TimeSpan(1, 0, 0, 0));
                }

                Guid user_guid = new Guid(user_id);
                MediaBrowser.Controller.Entities.User user = _userManager.GetUserById(user_guid);

                Dictionary<string, object> user_data = new Dictionary<string, object>();
                user_data.Add("user_id", user_id);
                user_data.Add("user_name", user.Name);
                user_data.Add("user_usage", userUsageByDate);

                user_usage_data.Add(user_data);
            }

            return user_usage_data;
        }

    }
}
