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

namespace playback_reporting.Api
{
    // http://localhost:8096/emby/user_usage_stats/import_backup
    [Route("/user_usage_stats/import_backup", "POST", Summary = "Post a backup for importing")]
    public class ImportBackup : IRequiresRequestStream, IReturnVoid
    {
        public Stream RequestStream { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/load_backup
    [Route("/user_usage_stats/load_backup", "GET", Summary = "Loads a backup from a file")]
    public class LoadBackup : IReturn<String>
    {
    }

    // http://localhost:8096/emby/user_usage_stats/save_backup
    [Route("/user_usage_stats/save_backup", "GET", Summary = "Saves a backup of the playback report data to the backup path")]
    public class SaveBackup : IReturn<ReportDayUsage>
    {
    }

    // http://localhost:8096/emby/user_usage_stats/30/PlayActivity
    [Route("/user_usage_stats/{NumberOfDays}/PlayActivity", "GET", Summary = "Gets play activity for number of days")]
    public class GetUsageStats : IReturn<ReportDayUsage>
    {
        [ApiMember(Name = "NumberOfDays", Description = "Number of Days", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "filter", Description = "Comma separated list of media types to filter (movies,series)", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        [ApiMember(Name = "data_type", Description = "Data type to return (count,time)", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]

        public int NumberOfDays { get; set; }
        public string filter { get; set; }
        public string data_type { get; set; }
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

    // http://localhost:8096/emby/user_usage_stats/30/HourlyReport
    [Route("/user_usage_stats/{NumberOfDays}/HourlyReport", "GET", Summary = "Gets a report of the averall activoty per hour")]
    public class GetHourlyReport : IReturn<ReportDayUsage>
    {
        [ApiMember(Name = "NumberOfDays", Description = "Number of Days", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "GET")]

        public int NumberOfDays { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/90/ItemType/BreakdownReport
    [Route("/user_usage_stats/{NumberOfDays}/{BreakdownType}/BreakdownReport", "GET", Summary = "Gets a breakdown of a usage metric")]
    public class GetBreakdownReport : IReturn<ReportDayUsage>
    {
        [ApiMember(Name = "NumberOfDays", Description = "Number of Days", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "GET")]
        [ApiMember(Name = "BreakdownType", Description = "Breakdown type", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public int NumberOfDays { get; set; }
        public string BreakdownType { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/90/DurationHistogramReport
    [Route("/user_usage_stats/{NumberOfDays}/DurationHistogramReport", "GET", Summary = "Gets duration histogram")]
    public class GetDurationHistogramReport : IReturn<ReportDayUsage>
    {
        [ApiMember(Name = "NumberOfDays", Description = "Number of Days", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "GET")]
        public int NumberOfDays { get; set; }
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

        public IRequest Request { get; set; }

        public object Get(GetUserReportData report)
        {
            string[] filter_tokens = new string[0];
            if (report.filter != null)
            {
                filter_tokens = report.filter.Split(',');
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

        public object Get(LoadBackup load_baclup)
        {
            ReportPlaybackOptions config = _config.GetReportPlaybackOptions();
            FileInfo fi = new FileInfo(config.BackupPath);
            if (fi.Exists == false)
            {
                return new List<string>() { "Backup file does not exist" };
            }

            int count = 0;
            try
            {
                string load_data = "";
                using (StreamReader sr = new StreamReader(new FileStream(config.BackupPath, FileMode.Open)))
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
        public object Get(SaveBackup save_baclup)
        {
            ReportPlaybackOptions config = _config.GetReportPlaybackOptions();

            if (string.IsNullOrEmpty(config.BackupPath))
            {
                return new List<string>() { "No backup path set" };
            }

            string raw_data = Repository.ExportRawData();

            DirectoryInfo fi = new DirectoryInfo(config.BackupPath);
            _logger.Info("Backup Path : " + config.BackupPath + " attributes : " + fi.Attributes + " exists : " + fi.Exists);
            if ((fi.Attributes & FileAttributes.Directory) == FileAttributes.Directory && fi.Exists)
            {
                string backup_file = Path.Combine(config.BackupPath, "PlaybackReportingBackup.tsv");
                config.BackupPath = backup_file;
                _logger.Info("Appending backup file name : " + config.BackupPath);
                _config.SaveReportPlaybackOptions(config);
            }

            try
            {
                System.IO.File.WriteAllText(config.BackupPath, raw_data);
            }
            catch (Exception e)
            {
                return new List<string>() { e.Message };
            }

            return new List<string>() { "Backup saved" };
        }

        public object Get(GetUsageStats activity)
        {
            string[] filter_tokens = new string[0];
            if (activity.filter != null)
            {
                filter_tokens = activity.filter.Split(',');
            }
            Dictionary<String, Dictionary<string, int>> results = Repository.GetUsageForDays(activity.NumberOfDays, filter_tokens, activity.data_type);

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

                Guid user_guid = new Guid(user_id);
                MediaBrowser.Controller.Entities.User user = _userManager.GetUserById(user_guid);

                string user_name = "Not Known";
                if (user != null)
                {
                    user_name = user.Name;
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
            SortedDictionary<string, int> report = Repository.GetHourlyUsageReport(request.NumberOfDays);

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
            List<Dictionary<string, object>> report = Repository.GetBreakdownReport(request.NumberOfDays, request.BreakdownType);

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
            SortedDictionary<int, int> report = Repository.GetDurationHistogram(request.NumberOfDays);

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
    }
}
