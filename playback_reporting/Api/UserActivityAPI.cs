using playback_reporting.Data;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace playback_reporting.Api
{

    // http://localhost:8096/emby/user_usage_stats/30/PlayActivity
    [Route("/user_usage_stats/{NumberOfDays}/PlayActivity", "GET", Summary = "Gets play activity for number of days")]
    public class GetUsageStats : IReturn<ReportDayUsage>
    {
        [ApiMember(Name = "NumberOfDays", Description = "Number of Days", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "filter", Description = "Comma separated list of media types to filter (movies,series)", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]

        public int NumberOfDays { get; set; }
        public string filter { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/4c0ea7608f3a41629a0a43a2f23fbb4c/2018-03-23/GetItems
    [Route("/user_usage_stats/{UserID}/{Date}/GetItems", "GET", Summary = "Gets activity for {USER} for {Date} formatted as yyyy-MM-dd")]
    public class GetUserReportData : IReturn<ReportDayUsage>
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "StartDate", Description = "UTC DateTime, Format yyyy-MM-dd", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "filter", Description = "Comma separated list of media types to filter (movies,series)", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]

        public string UserID { get; set; }
        public string Date { get; set; }
        public string filter { get; set; }
    }

    public class UserActivityAPI : IService
    {

        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _config;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;

        private IActivityRepository Repository;

        public UserActivityAPI(ILogger logger, 
            IFileSystem fileSystem,
            IServerConfigurationManager config,
            IJsonSerializer jsonSerializer,
            IUserManager userManager,
            ILibraryManager libraryManager)
        {
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            _config = config;
            _userManager = userManager;
            _libraryManager = libraryManager;

            var repo = new ActivityRepository(_logger, _config.ApplicationPaths, _fileSystem);
            repo.Initialize();
            Repository = repo;
        }

        public object Get(GetUserReportData report)
        {
            string[] filter_tokens = new string[0];
            if (report.filter != null)
            {
                filter_tokens = report.filter.Split(',');
            }
            List<Dictionary<string, string>> results = Repository.GetUsageForUser(report.Date, report.UserID, filter_tokens);

            List<Dictionary<string, object>> user_activity = new List<Dictionary<string, object>>();

            foreach(Dictionary<string, string> item_data in results)
            {
                Dictionary<string, object> item_info = new Dictionary<string, object>();

                item_info["Time"] = item_data["Time"];
                item_info["Id"] = item_data["Id"];
                item_info["Name"] = item_data["ItemName"];
                item_info["Type"] = item_data["Type"];
                item_info["Client"] = item_data["ClientName"];
                item_info["Method"] = item_data["PlaybackMethod"];
                item_info["Device"] = item_data["DeviceName"];
                item_info["Duration"] = item_data["PlayDuration"];

                user_activity.Add(item_info);
            }

            return user_activity;
        }

        public object Get(GetUsageStats activity)
        {
            string[] filter_tokens = new string[0];
            if (activity.filter != null)
            {
                filter_tokens = activity.filter.Split(',');
            }
            Dictionary<String, Dictionary<string, int>> results = Repository.GetUsageForDays(activity.NumberOfDays, filter_tokens);

            List<Dictionary<string, object>> user_usage_data = new List<Dictionary<string, object>>();
            foreach (string user_id in results.Keys)
            {
                Dictionary<string, int> user_usage = results[user_id];

                // fill in missing dates for time period
                SortedDictionary<string, int> userUsageByDate = new SortedDictionary<string, int>();
                DateTime from_date = DateTime.Now.AddDays(activity.NumberOfDays * -1);
                DateTime to_date = DateTime.Now.AddDays(1);
                while (from_date < to_date)
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

                    from_date = from_date.AddDays(1);
                }

                Guid user_guid = new Guid(user_id);
                MediaBrowser.Controller.Entities.User user = _userManager.GetUserById(user_guid);

                string user_name = "Not Known";
                if(user != null)
                {
                    user_name = user.Name;
                }

                Dictionary<string, object> user_data = new Dictionary<string, object>();
                user_data.Add("user_id", user_id);
                user_data.Add("user_name", user_name);
                user_data.Add("user_usage", userUsageByDate);

                user_usage_data.Add(user_data);
            }

            return user_usage_data;
        }

    }
}
