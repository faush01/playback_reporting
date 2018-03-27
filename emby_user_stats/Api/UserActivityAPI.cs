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

    // http://localhost:8096/emby/user_usage_stats/30/PlayActivity
    [Route("/user_usage_stats/{NumberOfDays}/PlayActivity", "GET", Summary = "Gets play activity for number of days")]
    public class GetUsageStats : IReturn<ReportDayUsage>
    {
        [ApiMember(Name = "NumberOfDays", Description = "Number of Days", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "GET")]
        public int NumberOfDays { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/4c0ea7608f3a41629a0a43a2f23fbb4c/2018-03-23/GetItems
    [Route("/user_usage_stats/{UserID}/{Date}/GetItems", "GET", Summary = "Gets activity for {USER} for {Date} formatted as yyyy-MM-dd")]
    public class GetUserReportData : IReturn<ReportDayUsage>
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "StartDate", Description = "UTC DateTime, Format yyyy-MM-dd", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "filter", Description = "Comma separated list of Collection Types to filter (movies,tvshows,music,musicvideos,boxsets", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]

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

        private IUserStatsRepository Repository;

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

            var repo = new UserStatsRepository(_logger, _config.ApplicationPaths, _fileSystem);
            repo.Initialize();
            Repository = repo;
        }

        public object Get(GetUserReportData report)
        {
            List<Dictionary<string, string>> results = Repository.GetUsageForUser(report.Date, report.UserID);

            List<Dictionary<string, object>> user_activity = new List<Dictionary<string, object>>();

            foreach(Dictionary<string, string> item_data in results)
            {
                Dictionary<string, object> item_info = new Dictionary<string, object>();
                string item_id = item_data["Id"];
                Guid item_giud = new Guid(item_id);
                MediaBrowser.Controller.Entities.BaseItem item = _libraryManager.GetItemById(item_giud);

                if (item != null)
                {
                    item_info["Name"] = item.Name;
                    item_info["Id"] = item.Id;

                }
                else
                {
                    item_info["Name"] = "Not Known";
                }

                item_info["Type"] = item_data["Type"];
                item_info["Time"] = item_data["Time"];

                user_activity.Add(item_info);
            }

            return user_activity;
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
