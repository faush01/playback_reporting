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
using System.IO;
using System.Linq;
using System.Globalization;

namespace playback_reporting.Api
{

    // http://localhost:8096/emby/user_usage_stats/user_activity
    [Route("/user_usage_stats/user_activity", "GET", Summary = "Gets a report of the available activity per hour")]
    public class GetUserReport : IReturn<Object>
    {
        [ApiMember(Name = "days", Description = "Number of Days", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int days { get; set; }
        [ApiMember(Name = "end_date", Description = "End date of the report in yyyy-MM-dd format", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string end_date { get; set; }
    }

    // http://localhost:8096/user_usage_stats/user_manage/add/1234-4321-1234
    [Route("/user_usage_stats/user_manage/{Action}/{Id}", "GET", Summary = "Get users")]
    public class GetUserManage : IReturn<Object>
    {
        [ApiMember(Name = "Action", Description = "action to perform", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Action { get; set; }
        [ApiMember(Name = "Id", Description = "user Id to perform the action on", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/user_list
    [Route("/user_usage_stats/user_list", "GET", Summary = "Get users")]
    public class GetUserList : IReturn<Object>
    {
    }

    // http://localhost:8096/emby/user_usage_stats/load_backup
    [Route("/user_usage_stats/type_filter_list", "GET", Summary = "Gets types filter list items")]
    public class TypeFilterList : IReturn<Object>
    {
    }

    // http://localhost:8096/emby/user_usage_stats/import_backup
    [Route("/user_usage_stats/import_backup", "POST", Summary = "Post a backup for importing")]
    public class ImportBackup : IRequiresRequestStream, IReturnVoid
    {
        public Stream RequestStream { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/submit_custom_query
    [Route("/user_usage_stats/submit_custom_query", "POST", Summary = "Submit an SQL query")]
    public class CustomQuery : IReturn<Object>
    {
        public String CustomQueryString { get; set; }
        public bool ReplaceUserId { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/load_backup
    [Route("/user_usage_stats/load_backup", "GET", Summary = "Loads a backup from a file")]
    public class LoadBackup : IReturn<Object>
    {
        [ApiMember(Name = "backupfile", Description = "File name of file to load", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string backupfile { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/save_backup
    [Route("/user_usage_stats/save_backup", "GET", Summary = "Saves a backup of the playback report data to the backup path")]
    public class SaveBackup : IReturn<Object>
    {
    }

    // http://localhost:8096/emby/user_usage_stats/PlayActivity
    [Route("/user_usage_stats/PlayActivity", "GET", Summary = "Gets play activity for number of days")]
    public class GetUsageStats : IReturn<Object>
    {
        [ApiMember(Name = "days", Description = "Number of Days", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int days { get; set; }
        [ApiMember(Name = "end_date", Description = "End date of the report in yyyy-MM-dd format", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string end_date { get; set; }
        [ApiMember(Name = "filter", Description = "Comma separated list of media types to filter (movies,series)", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string filter { get; set; }
        [ApiMember(Name = "data_type", Description = "Data type to return (count,time)", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string data_type { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/4c0ea7608f3a41629a0a43a2f23fbb4c/2018-03-23/GetItems
    [Route("/user_usage_stats/{UserID}/{Date}/GetItems", "GET", Summary = "Gets activity for {USER} for {Date} formatted as yyyy-MM-dd")]
    public class GetUserReportData : IReturn<Object>
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserID { get; set; }
        [ApiMember(Name = "Date", Description = "UTC DateTime, Format yyyy-MM-dd", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Date { get; set; }
        [ApiMember(Name = "Filter", Description = "Comma separated list of media types to filter (movies,series)", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Filter { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/HourlyReport
    [Route("/user_usage_stats/HourlyReport", "GET", Summary = "Gets a report of the available activity per hour")]
    public class GetHourlyReport : IReturn<Object>
    {
        [ApiMember(Name = "days", Description = "Number of Days", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int days { get; set; }
        [ApiMember(Name = "end_date", Description = "End date of the report in yyyy-MM-dd format", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string end_date { get; set; }
        [ApiMember(Name = "filter", Description = "Comma separated list of media types to filter (movies,series)", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string filter { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/ItemType/BreakdownReport
    [Route("/user_usage_stats/{BreakdownType}/BreakdownReport", "GET", Summary = "Gets a breakdown of a usage metric")]
    public class GetBreakdownReport : IReturn<Object>
    {
        [ApiMember(Name = "days", Description = "Number of Days", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int days { get; set; }
        [ApiMember(Name = "end_date", Description = "End date of the report in yyyy-MM-dd format", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string end_date { get; set; }
        [ApiMember(Name = "BreakdownType", Description = "Breakdown type", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string BreakdownType { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/DurationHistogramReport
    [Route("/user_usage_stats/DurationHistogramReport", "GET", Summary = "Gets duration histogram")]
    public class GetDurationHistogramReport : IReturn<Object>
    {
        [ApiMember(Name = "days", Description = "Number of Days", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int days { get; set; }
        [ApiMember(Name = "end_date", Description = "End date of the report in yyyy-MM-dd format", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string end_date { get; set; }
        [ApiMember(Name = "filter", Description = "Comma separated list of media types to filter (movies,series)", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string filter { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/TvShowsReport
    [Route("/user_usage_stats/TvShowsReport", "GET", Summary = "Gets TV Shows counts")]
    public class GetTvShowsReport : IReturn<Object>
    {
        [ApiMember(Name = "days", Description = "Number of Days", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int days { get; set; }
        [ApiMember(Name = "end_date", Description = "End date of the report in yyyy-MM-dd format", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string end_date { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/MoviesReport
    [Route("/user_usage_stats/MoviesReport", "GET", Summary = "Gets Movies counts")]
    public class GetMoviesReport : IReturn<Object>
    {
        [ApiMember(Name = "days", Description = "Number of Days", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int days { get; set; }
        [ApiMember(Name = "end_date", Description = "End date of the report in yyyy-MM-dd format", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string end_date { get; set; }
    }

    public class UserActivityAPI : IService, IRequiresRequest
    {

        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _config;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;

        private IActivityRepository Repository;

        public UserActivityAPI(ILogManager logger,
            IFileSystem fileSystem,
            IServerConfigurationManager config,
            IJsonSerializer jsonSerializer,
            IUserManager userManager,
            ILibraryManager libraryManager)
        {
            _logger = logger.GetLogger("PlaybackReporting - UserActivityAPI");
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            _config = config;
            _userManager = userManager;
            _libraryManager = libraryManager;

            _logger.Info("UserActivityAPI Loaded");
            var repo = new ActivityRepository(_logger, _config.ApplicationPaths, _fileSystem);
            //repo.Initialize();
            Repository = repo;
        }

        public IRequest Request { get; set; }

        public object Get(TypeFilterList request)
        {
            List<string> filter_list = Repository.GetTypeFilterList();
            return filter_list;
        }

        public object Get(GetUserReport request)
        {
            DateTime end_date;
            if (string.IsNullOrEmpty(request.end_date))
            {
                end_date = DateTime.Now;
            }
            else
            {
                _logger.Info("End_Date: " + request.end_date);
                end_date = DateTime.ParseExact(request.end_date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            List<Dictionary<string, object>> report = Repository.GetUserReport(request.days, end_date);

            foreach(var user_info in report)
            {
                string user_id = (string)user_info["user_id"];
                string user_name = "Not Known";
                Guid user_guid = new Guid(user_id);
                MediaBrowser.Controller.Entities.User user = _userManager.GetUserById(user_guid);
                if (user != null)
                {
                    user_name = user.Name;
                }
                user_info.Add("user_name", user_name);

                DateTime last_seen = (DateTime)user_info["latest_date"];
                TimeSpan time_ago = DateTime.Now.Subtract(last_seen);

                string last_seen_string = GetLastSeenString(time_ago);
                if (last_seen_string == "")
                {
                    last_seen_string = "just now";
                }
                user_info.Add("last_seen", last_seen_string);

                int seconds = (int)user_info["total_time"];
                TimeSpan total_time = new TimeSpan(10000000L * (long)seconds);

                string time_played = GetLastSeenString(total_time);
                if (time_played == "")
                {
                    time_played = "< 1 minute";
                }
                user_info.Add("total_play_time", time_played);

            }

            return report;
        }

        private string GetLastSeenString(TimeSpan span)
        {
            String last_seen = "";

            if (span.TotalDays > 365)
            {
                last_seen += GetTimePart((int)(span.TotalDays / 365), "year");
            }

            if ((int)(span.TotalDays % 365) > 7)
            {
                last_seen += GetTimePart((int)((span.TotalDays % 365) / 7), "week");
            }

            if ((int)(span.TotalDays % 7) > 0)
            {
                last_seen += GetTimePart((int)(span.TotalDays % 7), "day");
            }

            if (span.Hours > 0)
            {
                last_seen += GetTimePart(span.Hours, "hour");
            }

            if (span.Minutes > 0)
            {
                last_seen += GetTimePart(span.Minutes, "minute");
            }

            return last_seen;
        }

        private string GetTimePart(int value, string name)
        {
            string part = value + " " + name;
            if (value > 1)
            {
                part += "s";
            }
            return part + " ";
        }

        public object Get(GetUserManage request)
        {
            string action = request.Action;
            string id = request.Id;

            if (action == "remove_unknown")
            {
                List<string> user_id_list = new List<string>();
                foreach (var emby_user in _userManager.Users)
                {
                    user_id_list.Add(emby_user.Id.ToString("N"));
                }
                Repository.RemoveUnknownUsers(user_id_list);
            }
            else
            {
                Repository.ManageUserList(action, id);
            }

            return true;
        }

        public object Get(GetUserList request)
        {
            List<string> user_id_list = Repository.GetUserList();

            List<Dictionary<string, object>> users = new List<Dictionary<string, object>>();

            foreach (var emby_user in _userManager.Users)
            {
                Dictionary<string, object> user_info = new Dictionary<string, object>();
                user_info.Add("name", emby_user.Name);
                user_info.Add("id", emby_user.Id.ToString("N"));
                user_info.Add("in_list", user_id_list.Contains(emby_user.Id.ToString("N")));
                users.Add(user_info);
            }

            return users;
        }

        public object Get(GetUserReportData report)
        {
            string[] filter_tokens = new string[0];
            if (report.Filter != null)
            {
                filter_tokens = report.Filter.Split(',');
            }
            List<Dictionary<string, string>> results = Repository.GetUsageForUser(report.Date, report.UserID, filter_tokens);

            List<Dictionary<string, object>> user_activity = new List<Dictionary<string, object>>();

            foreach (Dictionary<string, string> item_data in results)
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
                item_info["RowId"] = item_data["RowId"];

                user_activity.Add(item_info);
            }

            return user_activity;
        }

        public void Post(ImportBackup request)
        {
            string headers = "";
            foreach (var head in Request.Headers.Keys)
            {
                headers += head + " : " + Request.Headers[head] + "\r\n";
            }
            _logger.Info("Header : " + headers);

            _logger.Info("Files Length : " + Request.Files.Length);

            _logger.Info("ContentType : " + Request.ContentType);

            Stream input_data = request.RequestStream;
            _logger.Info("Stream Info : " + input_data.CanRead);

            byte[] bytes = new byte[10000];
            int read = input_data.Read(bytes, 0, 10000);
            _logger.Info("Bytes Read : " + read);
            _logger.Info("Read : " + bytes);
        }

        public object Get(LoadBackup load_backup)
        {
            FileInfo fi = new FileInfo(load_backup.backupfile);
            if (fi.Exists == false)
            {
                return new List<string>() { "Backup file does not exist" };
            }

            int count = 0;
            try
            {
                string load_data = "";
                using (StreamReader sr = new StreamReader(new FileStream(fi.FullName, FileMode.Open)))
                {
                    load_data = sr.ReadToEnd();
                }
                count = Repository.ImportRawData(load_data);
            }
            catch (Exception e)
            {
                return new List<string>() { e.Message };
            }

            return new List<string>() { "Backup loaded " + count + " items" };
        }
        public object Get(SaveBackup save_backup)
        {
            BackupManager bum = new BackupManager(_config, _logger, _fileSystem);
            string message = bum.SaveBackup();

            return new List<string>() { message };
        }

        public object Get(GetUsageStats activity)
        {
            string[] filter_tokens = new string[0];
            if (activity.filter != null)
            {
                filter_tokens = activity.filter.Split(',');
            }

            DateTime end_date;
            if (string.IsNullOrEmpty(activity.end_date))
            {
                end_date = DateTime.Now;
            }
            else
            {
                _logger.Info("End_Date: " + activity.end_date);
                end_date = DateTime.ParseExact(activity.end_date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            
            Dictionary<String, Dictionary<string, int>> results = Repository.GetUsageForDays(activity.days, end_date, filter_tokens, activity.data_type);

            // add empty user for labels
            results.Add("labels_user", new Dictionary<string, int>());

            List<Dictionary<string, object>> user_usage_data = new List<Dictionary<string, object>>();
            foreach (string user_id in results.Keys)
            {
                Dictionary<string, int> user_usage = results[user_id];

                // fill in missing dates for time period
                SortedDictionary<string, int> userUsageByDate = new SortedDictionary<string, int>();
                DateTime from_date = end_date.AddDays((activity.days * -1) + 1);
                while (from_date <= end_date)
                {
                    string date_string = from_date.ToString("yyyy-MM-dd");
                    if (user_usage.ContainsKey(date_string) == false)
                    {
                        userUsageByDate.Add(date_string, 0);
                    }
                    else
                    {
                        userUsageByDate.Add(date_string, user_usage[date_string]);
                    }

                    from_date = from_date.AddDays(1);
                }

                string user_name = "Not Known";
                if (user_id == "labels_user")
                {
                    user_name = "labels_user";
                }
                else
                {
                    Guid user_guid = new Guid(user_id);
                    MediaBrowser.Controller.Entities.User user = _userManager.GetUserById(user_guid);
                    if (user != null)
                    {
                        user_name = user.Name;
                    }
                }

                Dictionary<string, object> user_data = new Dictionary<string, object>();
                user_data.Add("user_id", user_id);
                user_data.Add("user_name", user_name);
                user_data.Add("user_usage", userUsageByDate);

                user_usage_data.Add(user_data);
            }

            var sorted_data = user_usage_data.OrderBy(dict => (dict["user_name"] as string).ToLower());

            return sorted_data;
        }

        public object Get(GetHourlyReport request)
        {
            string[] filter_tokens = new string[0];
            if (request.filter != null)
            {
                filter_tokens = request.filter.Split(',');
            }

            DateTime end_date;
            if (string.IsNullOrEmpty(request.end_date))
            {
                end_date = DateTime.Now;
            }
            else
            {
                _logger.Info("End_Date: " + request.end_date);
                end_date = DateTime.ParseExact(request.end_date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            SortedDictionary<string, int> report = Repository.GetHourlyUsageReport(request.days, end_date, filter_tokens);

            for (int day = 0; day < 7; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    string key = day + "-" + hour.ToString("D2");
                    if(report.ContainsKey(key) == false)
                    {
                        report.Add(key, 0);
                    }
                }
            }

            return report;
        }

        public object Get(GetBreakdownReport request)
        {
            DateTime end_date;
            if (string.IsNullOrEmpty(request.end_date))
            {
                end_date = DateTime.Now;
            }
            else
            {
                _logger.Info("End_Date: " + request.end_date);
                end_date = DateTime.ParseExact(request.end_date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            List<Dictionary<string, object>> report = Repository.GetBreakdownReport(request.days, end_date, request.BreakdownType);

            if (request.BreakdownType == "UserId")
            {
                foreach (var row in report)
                {
                    string user_id = row["label"] as string;
                    Guid user_guid = new Guid(user_id);
                    MediaBrowser.Controller.Entities.User user = _userManager.GetUserById(user_guid);

                    if (user != null)
                    {
                        row["label"] = user.Name;
                    }
                    else
                    {
                        row["label"] = "unknown";
                    }
                }
            }

            return report;
        }

        public object Get(GetDurationHistogramReport request)
        {
            string[] filter_tokens = new string[0];
            if (request.filter != null)
            {
                filter_tokens = request.filter.Split(',');
            }

            DateTime end_date;
            if (string.IsNullOrEmpty(request.end_date))
            {
                end_date = DateTime.Now;
            }
            else
            {
                _logger.Info("End_Date: " + request.end_date);
                end_date = DateTime.ParseExact(request.end_date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            SortedDictionary<int, int> report = Repository.GetDurationHistogram(request.days, end_date, filter_tokens);

            // find max
            int max = -1;
            foreach (int key in report.Keys)
            {
                if (key > max)
                {
                    max = key;
                }
            }

            for(int x = 0; x < max; x++)
            {
                if(report.ContainsKey(x) == false)
                {
                    report.Add(x, 0);
                }
            }

            return report;
        }

        public object Get(GetTvShowsReport request)
        {
            DateTime end_date;
            if (string.IsNullOrEmpty(request.end_date))
            {
                end_date = DateTime.Now;
            }
            else
            {
                _logger.Info("End_Date: " + request.end_date);
                end_date = DateTime.ParseExact(request.end_date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            List<Dictionary<string, object>> report = Repository.GetTvShowReport(request.days, end_date);
            return report;
        }

        public object Get(GetMoviesReport request)
        {
            DateTime end_date;
            if (string.IsNullOrEmpty(request.end_date))
            {
                end_date = DateTime.Now;
            }
            else
            {
                _logger.Info("End_Date: " + request.end_date);
                end_date = DateTime.ParseExact(request.end_date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            List<Dictionary<string, object>> report = Repository.GetMoviesReport(request.days, end_date);
            return report;
        }

        public object Post(CustomQuery request)
        {
            _logger.Info("CustomQuery : " + request.CustomQueryString);

            Dictionary<string, object> responce = new Dictionary<string, object>();

            List<List<object>> result = new List<List<object>>();
            List<string> colums = new List<string>();
            string message = Repository.RunCustomQuery(request.CustomQueryString, colums, result);

            int index_of_user_col = colums.IndexOf("UserId");
            if (request.ReplaceUserId && index_of_user_col > -1)
            {
                colums[index_of_user_col] = "UserName";

                Dictionary<string, string> user_map = new Dictionary<string, string>();
                foreach (var user in _userManager.Users)
                {
                    user_map.Add(user.Id.ToString("N"), user.Name);
                }

                foreach(var row in result)
                {
                    String user_id = (string)row[index_of_user_col];
                    if(user_map.ContainsKey(user_id))
                    {
                        row[index_of_user_col] = user_map[user_id];
                    }
                }
            }


            /*
            List<object> row = new List<object>();
            row.Add("Shaun");
            row.Add(12);
            row.Add("Some Date");
            result.Add(row);

            colums.Add("Name");
            colums.Add("Age");
            colums.Add("Started");
            */

            responce.Add("colums", colums);
            responce.Add("results", result);
            responce.Add("message", message);
            
            return responce;
        }
    }
}
