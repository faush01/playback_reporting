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

using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Logging;
using SQLitePCL.pretty;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;


namespace playback_reporting.Data
{
    public sealed class ActivityRepository
    {
        private static ActivityRepository instance = null;
        private static readonly object _padlock = new object();

        private static string[] _datetimeFormats = new string[] {
            "THHmmssK",
            "THHmmK",
            "HH:mm:ss.FFFFFFFK",
            "HH:mm:ssK",
            "HH:mmK",
            "yyyy-MM-dd HH:mm:ss.FFFFFFFK", /* NOTE: UTC default (5). */
            "yyyy-MM-dd HH:mm:ssK",
            "yyyy-MM-dd HH:mmK",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFFK",
            "yyyy-MM-ddTHH:mmK",
            "yyyy-MM-ddTHH:mm:ssK",
            "yyyyMMddHHmmssK",
            "yyyyMMddHHmmK",
            "yyyyMMddTHHmmssFFFFFFFK",
            "THHmmss",
            "THHmm",
            "HH:mm:ss.FFFFFFF",
            "HH:mm:ss",
            "HH:mm",
            "yyyy-MM-dd HH:mm:ss.FFFFFFF", /* NOTE: Non-UTC default (19). */
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
            "yyyy-MM-ddTHH:mm",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyyMMddHHmmss",
            "yyyyMMddHHmm",
            "yyyyMMddTHHmmssFFFFFFF",
            "yyyy-MM-dd",
            "yyyyMMdd",
            "yy-MM-dd"
        };
        private string _datetimeFormatUtc = _datetimeFormats[5];
        private string _datetimeFormatLocal = _datetimeFormats[19];
        
        private ILogger _logger = null;
        private IDatabaseConnection connection = null;

        public static ActivityRepository GetInstance(string db_file, ILogger log)
        {
            lock (_padlock)
            {
                if (instance == null)
                {
                    instance = new ActivityRepository(db_file, log);
                    log.Info("ActivityRepository : New Instance Created : " + instance.GetHashCode());
                }
                return instance;
            }
        }

        private ActivityRepository()
        {

        }

        private ActivityRepository(string db_path, ILogger l)
        {
            _logger = l;
            _logger.Info("ActivityRepository : Creating");
            string db_file_name = Path.Combine(db_path, "playback_reporting.db");
            connection = CreateConnection(db_file_name);
        }

        ~ActivityRepository()
        {
            _logger.Info("ActivityRepository : Clenaing up");
            if (connection != null)
            {
                connection.Close();
                _logger.Info("ActivityRepository : DB Connection Closed");
            }
        }

        public void Initialize()
        {
            InitializeInternal();
        }

        private void TryBind(IStatement statement, string name, int value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                bindParam.Bind(value);
            }
        }

        public void TryBind(IStatement statement, string name, string value)
        {
            IBindParameter bindParam;
            if (statement.BindParameters.TryGetValue(name, out bindParam))
            {
                if (value == null)
                {
                    bindParam.BindNull();
                }
                else
                {
                    bindParam.Bind(value);
                }
            }
        }

        private string GetDateTimeKindFormat(DateTimeKind kind)
        {
            return (kind == DateTimeKind.Utc) ? _datetimeFormatUtc : _datetimeFormatLocal;
        }

        public DateTime ReadDateTime(string dateText)
        {
            return DateTime.ParseExact(
                dateText, 
                _datetimeFormats,
                DateTimeFormatInfo.InvariantInfo,
                DateTimeStyles.None).ToUniversalTime();
        }

        public string ToDateTimeParamValue(DateTime dateValue)
        {
            var kind = DateTimeKind.Utc;
            if (dateValue.Kind == DateTimeKind.Unspecified) // if Unspecified force UTC
            {
                return DateTime.SpecifyKind(dateValue, kind).ToString(GetDateTimeKindFormat(kind), CultureInfo.InvariantCulture);
            }
            else
            {
                return dateValue.ToString(GetDateTimeKindFormat(dateValue.Kind), CultureInfo.InvariantCulture);
            }
        }

        private IDatabaseConnection CreateConnection(string db_file)
        {
            _logger.Info("ActivityRepository : CreateConnection : " + db_file);
            ConnectionFlags connectionFlags;

            //Logger.Info("Opening write connection");
            connectionFlags = ConnectionFlags.Create;
            connectionFlags |= ConnectionFlags.ReadWrite;
            connectionFlags |= ConnectionFlags.PrivateCache;
            connectionFlags |= ConnectionFlags.NoMutex;

            SQLiteDatabaseConnection db = SQLite3.Open(db_file, connectionFlags, null, false);

            try
            {
                var queries = new List<string>
                {
                    //"PRAGMA cache size=-10000"
                    //"PRAGMA read_uncommitted = true",
                    "PRAGMA synchronous=Normal",
                    "PRAGMA temp_store=file"
                };

                db.ExecuteAll(string.Join(";", queries.ToArray()));
            }
            catch
            {
                throw;
            }

            _logger.Info("ActivityRepository : ConnectionCreated : " + db.GetHashCode());
            return db;
        }

        private void InitializeInternal()
        {
            lock (connection)
            {
                string sql_info = "pragma table_info('PlaybackActivity')";
                List<string> cols = new List<string>();
                using (var statement = connection.PrepareStatement(sql_info))
                {
                    while(statement.MoveNext())
                    {
                        var row = statement.Current;
                        string table_schema = row.GetString(1).ToLower() + ":" + row.GetString(2).ToLower();
                        cols.Add(table_schema);
                    }
                }
                string actual_schema = string.Join("|", cols);
                string required_schema = "datecreated:datetime|userid:text|itemid:text|itemtype:text|itemname:text|playbackmethod:text|clientname:text|devicename:text|playduration:int|pauseduration:int|remoteaddress:text";
                if (required_schema != actual_schema)
                {                       
                    string new_table_name = "PlaybackActivity_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    connection.Execute("ALTER TABLE PlaybackActivity RENAME TO " + new_table_name);
                }

                // ROWID 
                connection.Execute("create table if not exists PlaybackActivity (" +
                                "DateCreated DATETIME NOT NULL, " +
                                "UserId TEXT, " +
                                "ItemId TEXT, " +
                                "ItemType TEXT, " +
                                "ItemName TEXT, " +
                                "PlaybackMethod TEXT, " +
                                "ClientName TEXT, " +
                                "DeviceName TEXT, " +
                                "PlayDuration INT, " +
                                "PauseDuration INT, " +
                                "RemoteAddress TEXT" +
                                ")");

                connection.Execute("create table if not exists UserList (UserId TEXT)");
            }
        }

        public List<KeyValuePair<string, int>> GetPlayActivityCounts(int hours)
        {
            string sql =
                "SELECT " +
                "ItemId, " +
                "DateCreated AS StartTime, " +
                "PlayDuration, " +
                "datetime(DateCreated, '+' || CAST(PlayDuration AS VARCHAR) || ' seconds') AS EndTime " + 
                "FROM PlaybackActivity " +
                "WHERE EndTime > @start_time";

            DateTime start_date_sql = DateTime.Now.AddHours(-1 * hours);

            //Dictionary<DateTime, int> actions = new Dictionary<DateTime, int>();

            Dictionary<string, KeyValuePair<DateTime, int>> actions = new Dictionary<string, KeyValuePair<DateTime, int>>();
            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql))
                {
                    TryBind(statement, "@start_time", start_date_sql.ToString("yyyy-MM-dd HH:mm:ss"));

                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        string item_id = row.GetString(0);
                        DateTime start_time = ReadDateTime(row.GetString(1)).ToLocalTime();
                        int duration = row.GetInt(2);
                        DateTime end_time = start_time.AddSeconds(duration);

                        string start_key = start_time.ToString("yyyy-MM-dd HH:mm:ss.fffff") + "-" + item_id + "-A";
                        KeyValuePair<DateTime, int> data_start = new KeyValuePair<DateTime, int>(start_time, 1);
                        string end_key = end_time.ToString("yyyy-MM-dd HH:mm:ss.fffff") + "-" + item_id + "-B";
                        KeyValuePair<DateTime, int> data_end = new KeyValuePair<DateTime, int>(end_time, 0);

                        actions.Add(start_key, data_start);
                        actions.Add(end_key, data_end);
                    }
                }
            }

            List<KeyValuePair<string, int>> results = new List<KeyValuePair<string, int>>();
            List<string> keyList = actions.Keys.ToList();
            keyList.Sort();
            int count = 0;
            foreach(string key in keyList)
            {
                KeyValuePair<DateTime, int> data = actions[key];
                if (data.Value == 1)
                {
                    count++;
                }
                else if (data.Value == 0)
                {
                    count--;
                }

                DateTime data_timestamp = data.Key;
                if (data_timestamp < start_date_sql)
                {
                    data_timestamp = start_date_sql;
                }

                results.Add(new KeyValuePair<string, int>(data_timestamp.ToString("yyyy-MM-dd HH:mm:ss"), count));
            }

            return results;
        }

        public string RunCustomQuery(string query_string, List<string> col_names, List<List<object>> results)
        {
            string message = "";
            int change_count = 0;
            lock (connection)
            {
                try
                {
                    using (var statement = connection.PrepareStatement(query_string))
                    {
                        while(statement.MoveNext())
                        {
                            int col_count = statement.Columns.Count;
                            var row = statement.Current;
                            List<object> row_date = new List<object>();
                            for(int x = 0; x < col_count; x++)
                            {
                                string cell_data = row.GetString(x);
                                row_date.Add(cell_data);
                            }
                            results.Add(row_date);
                        }

                        foreach (var col in statement.Columns)
                        {
                            col_names.Add(col.Name);
                        }

                        change_count = connection.Changes;
                    }
                }
                catch(Exception e)
                {
                    message = "Error Running Query</br>" + e.Message;
                    message += "<pre>" + e.ToString() + "</pre>";
                }
            }

            if(string.IsNullOrEmpty(message) && col_names.Count == 0 && results.Count == 0)
            {
                message = "Query executed, no data returned.";
                message += "</br>Number of rows effected : " + change_count;
            }

            return message;
        }

        public int RemoveUnknownUsers(List<string> known_user_ids)
        {
            string sql_query = "delete from PlaybackActivity " +
                               "where UserId not in ('" + string.Join("', '", known_user_ids) + "') or UserId is null or UserId = ''";

            _logger.Info("Remove Users Query : " + sql_query);
            int change_count = 0;
            lock (connection)
            {
                connection.Execute(sql_query);
                change_count = connection.Changes;
            }
            return change_count;
        }

        public void ManageUserList(string action, string id)
        {
            string sql = "";
            if(action == "add")
            {
                sql = "insert into UserList (UserId) values (@id)";
            }
            else
            {
                sql = "delete from UserList where UserId = @id";
            }
            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql))
                {
                    TryBind(statement, "@id", id);
                    statement.MoveNext();
                }
            }
        }

        public List<string> GetUserList()
        {
            List<string> user_id_list = new List<string>();
            lock (connection)
            {
                string sql_query = "select UserId from UserList";
                using (var statement = connection.PrepareStatement(sql_query))
                {
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        string type = row.GetString(0);
                        user_id_list.Add(type);
                    }
                }
            }

            return user_id_list;
        }

        public List<string> GetTypeFilterList()
        {
            List<string> filter_Type_list = new List<string>();
            lock (connection)
            {
                string sql_query = "select distinct ItemType from PlaybackActivity";
                using (var statement = connection.PrepareStatement(sql_query))
                {
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        string type = row.GetString(0);
                        filter_Type_list.Add(type);
                    }
                }
            }
            return filter_Type_list;
        }

        public int ImportRawData(string data)
        {
            int count = 0;
            lock (connection)
            {
                StringReader sr = new StringReader(data);

                string line = sr.ReadLine();
                while (line != null)
                {
                    string[] tokens = line.Split('\t');
                    if (tokens.Length < 10)
                    {
                        line = sr.ReadLine();
                        continue;
                    }

                    string date = tokens[0];
                    string user_id = tokens[1];
                    string item_id = tokens[2];
                    string item_type = tokens[3];
                    string item_name = tokens[4];
                    string play_method = tokens[5];
                    string client_name = tokens[6];
                    string device_name = tokens[7];
                    string play_duration = tokens[8];
                    string paused_duration = tokens[9];

                    string remote_address = "";
                    if (tokens.Length > 10)
                    {
                        remote_address = tokens[10];
                    }

                    string sql = "select rowid from PlaybackActivity where DateCreated = @DateCreated and UserId = @UserId and ItemId = @ItemId";
                    using (var statement = connection.PrepareStatement(sql))
                    {

                        TryBind(statement, "@DateCreated", date);
                        TryBind(statement, "@UserId", user_id);
                        TryBind(statement, "@ItemId", item_id);
                        bool found = false;
                        if (statement.MoveNext())
                        {
                            found = true;
                            break;
                        }

                        if (found == false)
                        {
                            string sql_add = "insert into PlaybackActivity " +
                                "(DateCreated, UserId, ItemId, ItemType, ItemName, PlaybackMethod, ClientName, DeviceName, PlayDuration, PauseDuration, RemoteAddress) " +
                                "values " +
                                "(@DateCreated, @UserId, @ItemId, @ItemType, @ItemName, @PlaybackMethod, @ClientName, @DeviceName, @PlayDuration, @PauseDuration, @RemoteAddress)";

                            using (var add_statment = connection.PrepareStatement(sql_add))
                            {
                                TryBind(add_statment, "@DateCreated", date);
                                TryBind(add_statment, "@UserId", user_id);
                                TryBind(add_statment, "@ItemId", item_id);
                                TryBind(add_statment, "@ItemType", item_type);
                                TryBind(add_statment, "@ItemName", item_name);
                                TryBind(add_statment, "@PlaybackMethod", play_method);
                                TryBind(add_statment, "@ClientName", client_name);
                                TryBind(add_statment, "@DeviceName", device_name);
                                TryBind(add_statment, "@PlayDuration", play_duration);
                                TryBind(add_statment, "@PauseDuration", paused_duration);
                                TryBind(add_statment, "@RemoteAddress", remote_address);
                                add_statment.MoveNext();
                            }
                            count++;
                        }
                    }

                    line = sr.ReadLine();
                }
            }
            return count;
        }

        public string ExportRawData()
        {
            StringWriter sw = new StringWriter();

            string sql_raw = "SELECT * FROM PlaybackActivity ORDER BY DateCreated";
            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql_raw))
                {                      
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        List<string> row_data = new List<string>();
                        int col_count = statement.Columns.Count;
                        for (int x = 0; x < col_count; x++)
                        {
                            row_data.Add(row.GetString(x));
                        }
                        sw.WriteLine(string.Join("\t", row_data));
                    }
                }
            }
            sw.Flush();
            return sw.ToString();
        }

        public void DeleteOldData(DateTime? del_before)
        {
            string sql = "delete from PlaybackActivity";
            if (del_before.HasValue)
            {
                DateTime date = del_before.Value;
                sql += " where DateCreated < '" + ToDateTimeParamValue(date) + "'";
            }

            lock (connection)
            {
                connection.Execute(sql);
            }
        }

        public void AddPlaybackAction(PlaybackInfo play_info)
        {
            string sql_add = "insert into PlaybackActivity " +
                "(DateCreated, UserId, ItemId, ItemType, ItemName, PlaybackMethod, ClientName, DeviceName, PlayDuration, PauseDuration, RemoteAddress) " +
                "values " +
                "(@DateCreated, @UserId, @ItemId, @ItemType, @ItemName, @PlaybackMethod, @ClientName, @DeviceName, @PlayDuration, @PauseDuration, @RemoteAddress)";

            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql_add))
                {
                    TryBind(statement, "@DateCreated", ToDateTimeParamValue(play_info.Date));
                    TryBind(statement, "@UserId", play_info.UserId);
                    TryBind(statement, "@ItemId", play_info.ItemId);
                    TryBind(statement, "@ItemType", play_info.ItemType);
                    TryBind(statement, "@ItemName", play_info.ItemName);
                    TryBind(statement, "@PlaybackMethod", play_info.PlaybackMethod);
                    TryBind(statement, "@ClientName", play_info.ClientName);
                    TryBind(statement, "@DeviceName", play_info.DeviceName);
                    TryBind(statement, "@PlayDuration", play_info.PlaybackDuration);
                    TryBind(statement, "@PauseDuration", play_info.PausedDuration);
                    TryBind(statement, "@RemoteAddress", play_info.RemoteAddress);
                    statement.MoveNext();
                }
            }
        }

        public void UpdatePlaybackAction(PlaybackInfo play_info)
        {
            string sql_add = "update PlaybackActivity set PlayDuration = @PlayDuration, PauseDuration = @PauseDuration where DateCreated = @DateCreated and UserId = @UserId and ItemId = @ItemId";
            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql_add))
                {
                    TryBind(statement, "@DateCreated", ToDateTimeParamValue(play_info.Date));
                    TryBind(statement, "@UserId", play_info.UserId);
                    TryBind(statement, "@ItemId", play_info.ItemId);
                    TryBind(statement, "@PlayDuration", play_info.PlaybackDuration);
                    TryBind(statement, "@PauseDuration", play_info.PausedDuration);
                    statement.MoveNext();
                }
            }
        }

        public List<Dictionary<string, string>> GetUsageForUser(string date, string user_id, string[] types, ReportPlaybackOptions config)
        {
            List<string> filters = new List<string>();
            foreach (string filter in types)
            {
                filters.Add("'" + filter + "'");
            }

            string sql_query = "SELECT DateCreated, ItemId, ItemType, ItemName, ClientName, PlaybackMethod, DeviceName, (PlayDuration - PauseDuration) AS PlayDuration, rowid, RemoteAddress ";
            sql_query += "FROM PlaybackActivity ";
            sql_query += "WHERE DateCreated >= @date_from AND DateCreated <= @date_to ";
            sql_query += "AND UserId = @user_id ";
            sql_query += "AND ItemType IN (" + string.Join(",", filters) + ") ";

            if(config.IgnoreSmallerThan > 0)
            {
                sql_query += "AND (PlayDuration - PauseDuration) > " + config.IgnoreSmallerThan + " ";
            }

            sql_query += "ORDER BY DateCreated";

            List<Dictionary<string, string>> items = new List<Dictionary<string, string>>();
            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql_query))
                {
                    TryBind(statement, "@date_from", date + " 00:00:00");
                    TryBind(statement, "@date_to", date + " 23:59:59");
                    TryBind(statement, "@user_id", user_id);
                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        //string item_id = row.GetString(1);

                        Dictionary<string, string> item = new Dictionary<string, string>();
                        item["Time"] = ReadDateTime(row.GetString(0)).ToLocalTime().ToString("HH:mm");
                        item["Id"] = row.GetString(1);
                        item["Type"] = row.GetString(2);
                        item["ItemName"] = row.GetString(3);
                        item["ClientName"] = row.GetString(4);
                        item["PlaybackMethod"] = row.GetString(5);
                        item["DeviceName"] = row.GetString(6);
                        item["PlayDuration"] = row.GetString(7);
                        item["RowId"] = row.GetString(8);
                        item["RemoteAddress"] = row.GetString(9);

                        items.Add(item);
                    }
                }
            }

            return items;
        }

        public Dictionary<String, Dictionary<string, int>> GetUsageForDays(
            int days, 
            DateTime end_date, 
            string[] types, 
            string data_type, 
            ReportPlaybackOptions config)
        {
            List<string> filters = new List<string>();
            foreach (string filter in types)
            {
                filters.Add("'" + filter + "'");
            }

            string sql_query = "";
            if (data_type == "count")
            {
                sql_query += "SELECT UserId, strftime('%Y-%m-%d', DateCreated) AS date, COUNT(1) AS count ";
            }
            else
            {
                sql_query += "SELECT UserId, strftime('%Y-%m-%d', DateCreated) AS date, SUM(PlayDuration - PauseDuration) AS count ";
            }
            sql_query += "FROM PlaybackActivity ";
            sql_query += "WHERE DateCreated >= @start_date AND DateCreated <= @end_date ";
            sql_query += "AND ItemType IN (" + string.Join(",", filters) + ") ";
            sql_query += "AND UserId not IN (select UserId from UserList) ";

            if(config.IgnoreSmallerThan > 0)
            {
                sql_query += "AND (PlayDuration - PauseDuration) > " + config.IgnoreSmallerThan + " ";
            }

            sql_query += "GROUP BY UserId, date ORDER BY UserId, date ASC";

            DateTime start_date = end_date.Subtract(new TimeSpan(days, 0, 0, 0));
            Dictionary<String, Dictionary<string, int>> usage = new Dictionary<String, Dictionary<string, int>>();

            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql_query))
                {
                    TryBind(statement, "@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                    TryBind(statement, "@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));

                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        string user_id = row.GetString(0);
                        if (string.IsNullOrEmpty(user_id))
                        {
                            user_id = "unknown";
                        }

                        Dictionary<string, int> uu = null;
                        if (usage.ContainsKey(user_id))
                        {
                            uu = usage[user_id];
                        }
                        else
                        {
                            uu = new Dictionary<string, int>();
                            usage.Add(user_id, uu);
                        }
                        string date_string = row.GetString(1);
                        int count_int = row.GetInt(2);
                        uu.Add(date_string, count_int);
                    }
                }
            }

            return usage;
        }

        public SortedDictionary<string, int> GetHourlyUsageReport(string user_id, int days, DateTime end_date, string[] types, ReportPlaybackOptions config)
        {
            List<string> filters = new List<string>();
            foreach (string filter in types)
            {
                filters.Add("'" + filter + "'");
            }

            SortedDictionary<string, int> report_data = new SortedDictionary<string, int>();

            DateTime start_date = end_date.Subtract(new TimeSpan(days, 0, 0, 0));

            string sql = "SELECT DateCreated, (PlayDuration - PauseDuration) as Duration ";
            sql += "FROM PlaybackActivity ";
            sql += "WHERE DateCreated >= @start_date AND DateCreated <= @end_date ";
            sql += "AND UserId not IN (select UserId from UserList) ";
            sql += "AND ItemType IN (" + string.Join(",", filters) + ")";

            if (!string.IsNullOrEmpty(user_id))
            {
                sql += " AND UserId = @user_id";
            }

            if (config.IgnoreSmallerThan > 0)
            {
                sql += " AND (PlayDuration - PauseDuration) > " + config.IgnoreSmallerThan;
            }

            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql))
                {
                    TryBind(statement, "@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                    TryBind(statement, "@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));
                    TryBind(statement, "@user_id", user_id);

                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        DateTime date = ReadDateTime(row.GetString(0)).ToLocalTime();
                        int duration = row.GetInt(1);

                        int seconds_left_in_hour = 3600 - ((date.Minute * 60) + date.Second);
                        while (duration > 0)
                        {
                            string hour_id = (int)date.DayOfWeek + "-" + date.ToString("HH");
                            if (duration > seconds_left_in_hour)
                            {
                                AddTimeToHours(report_data, hour_id, seconds_left_in_hour);
                            }
                            else
                            {
                                AddTimeToHours(report_data, hour_id, duration);
                            }

                            duration = duration - seconds_left_in_hour;
                            seconds_left_in_hour = 3600;
                            date = date.AddHours(1);
                        }
                    }
                }
            }

            return report_data;
        }

        private void AddTimeToHours(SortedDictionary<string, int> report_data, string key, int count)
        {
            if (report_data.ContainsKey(key))
            {
                report_data[key] += count;
            }
            else
            {
                report_data.Add(key, count);
            }
        }

        public List<Dictionary<string, object>> GetBreakdownReport(string user_id, int days, DateTime end_date, string type, ReportPlaybackOptions config)
        {
            // UserId ItemType PlaybackMethod ClientName DeviceName

            List<Dictionary<string, object>> report = new List<Dictionary<string, object>>();

            DateTime start_date = end_date.Subtract(new TimeSpan(days, 0, 0, 0));
            Dictionary<String, Dictionary<string, int>> usage = new Dictionary<String, Dictionary<string, int>>();

            string sql = "SELECT " + type + ", COUNT(1) AS PlayCount, SUM(PlayDuration - PauseDuration) AS Seconds ";
            sql += "FROM PlaybackActivity ";
            sql += "WHERE DateCreated >= @start_date AND DateCreated <= @end_date ";
            sql += "AND UserId not IN (select UserId from UserList) ";

            if (!string.IsNullOrEmpty(user_id))
            {
                sql += "AND UserId = @user_id ";
            }

            if (config.IgnoreSmallerThan > 0)
            {
                sql += "AND (PlayDuration - PauseDuration) > " + config.IgnoreSmallerThan + " ";
            }

            sql += "GROUP BY " + type;

            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql))
                {
                    TryBind(statement, "@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                    TryBind(statement, "@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));
                    TryBind(statement, "@user_id", user_id);

                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        string item_label = row.GetString(0);
                        int action_count = row.GetInt(1);
                        int seconds_sum = row.GetInt(2);

                        Dictionary<string, object> row_data = new Dictionary<string, object>();
                        row_data.Add("label", item_label);
                        row_data.Add("count", action_count);
                        row_data.Add("time", seconds_sum);
                        report.Add(row_data);
                    }
                }
            }

            return report;
        }

        public List<Dictionary<string, object>> GetTvShowReport(string user_id, int days, DateTime end_date, ReportPlaybackOptions config)
        {
            List<Dictionary<string, object>> report = new List<Dictionary<string, object>>();

            DateTime start_date = end_date.Subtract(new TimeSpan(days, 0, 0, 0));
            Dictionary<String, Dictionary<string, int>> usage = new Dictionary<String, Dictionary<string, int>>();

            string sql = "";
            sql += "SELECT substr(ItemName,0, instr(ItemName, ' - ')) AS name, ";
            sql += "COUNT(1) AS play_count, ";
            sql += "SUM(PlayDuration - PauseDuration) AS total_duarion ";
            sql += "FROM PlaybackActivity ";
            sql += "WHERE ItemType = 'Episode' ";
            sql += "AND DateCreated >= @start_date AND DateCreated <= @end_date ";
            sql += "AND UserId not IN (select UserId from UserList) ";

            if (!string.IsNullOrEmpty(user_id))
            {
                sql += "AND UserId = @user_id ";
            }

            if (config.IgnoreSmallerThan > 0)
            {
                sql += "AND (PlayDuration - PauseDuration) > " + config.IgnoreSmallerThan + " ";
            }

            sql += "GROUP BY name";

            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql))
                {
                    TryBind(statement, "@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                    TryBind(statement, "@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));
                    TryBind(statement, "@user_id", user_id);

                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        string item_label = row.GetString(0);
                        int action_count = row.GetInt(1);
                        int seconds_sum = row.GetInt(2);

                        Dictionary<string, object> row_data = new Dictionary<string, object>();
                        row_data.Add("label", item_label);
                        row_data.Add("count", action_count);
                        row_data.Add("time", seconds_sum);
                        report.Add(row_data);
                    }
                }
            }

            return report;
        }

        public List<Dictionary<string, object>> GetMoviesReport(string user_id, int days, DateTime end_date, ReportPlaybackOptions config)
        {
            List<Dictionary<string, object>> report = new List<Dictionary<string, object>>();

            DateTime start_date = end_date.Subtract(new TimeSpan(days, 0, 0, 0));
            Dictionary<String, Dictionary<string, int>> usage = new Dictionary<String, Dictionary<string, int>>();

            string sql = "";
            sql += "SELECT ItemName AS name, ";
            sql += "COUNT(1) AS play_count, ";
            sql += "SUM(PlayDuration - PauseDuration) AS total_duarion ";
            sql += "FROM PlaybackActivity ";
            sql += "WHERE ItemType = 'Movie' ";
            sql += "AND DateCreated >= @start_date AND DateCreated <= @end_date ";
            sql += "AND UserId not IN (select UserId from UserList) ";

            if (!string.IsNullOrEmpty(user_id))
            {
                sql += "AND UserId = @user_id ";
            }

            if (config.IgnoreSmallerThan > 0)
            {
                sql += "AND (PlayDuration - PauseDuration) > " + config.IgnoreSmallerThan + " ";
            }

            sql += "GROUP BY name";

            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql))
                {
                    TryBind(statement, "@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                    TryBind(statement, "@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));
                    TryBind(statement, "@user_id", user_id);

                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        string item_label = row.GetString(0);
                        int action_count = row.GetInt(1);
                        int seconds_sum = row.GetInt(2);

                        Dictionary<string, object> row_data = new Dictionary<string, object>();
                        row_data.Add("label", item_label);
                        row_data.Add("count", action_count);
                        row_data.Add("time", seconds_sum);
                        report.Add(row_data);
                    }
                }
            }

            return report;
        }

        public List<Dictionary<string, object>> GetUserReport(int days, DateTime end_date)
        {
            List<Dictionary<string, object>> report = new List<Dictionary<string, object>>();

            DateTime start_date = end_date.Subtract(new TimeSpan(days, 0, 0, 0));
            Dictionary<String, Dictionary<string, int>> usage = new Dictionary<String, Dictionary<string, int>>();

            string sql = "";
            sql += "SELECT x.latest_date, x.UserId, x.play_count, x.total_duarion, y.ItemName, y.DeviceName, y.ItemId ";
            sql += "FROM( ";
            sql += "SELECT MAX(DateCreated) AS latest_date, UserId, COUNT(1) AS play_count, SUM(PlayDuration - PauseDuration) AS total_duarion ";
            sql += "FROM PlaybackActivity ";
            sql += "WHERE DateCreated >= @start_date AND DateCreated <= @end_date ";
            sql += "AND UserId not IN (select UserId from UserList) ";
            sql += "GROUP BY UserId ";
            sql += ") AS x ";
            sql += "INNER JOIN PlaybackActivity AS y ON x.latest_date = y.DateCreated AND x.UserId = y.UserId ";
            sql += "ORDER BY x.latest_date DESC";

            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql))
                {
                    TryBind(statement, "@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                    TryBind(statement, "@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));

                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        Dictionary<string, object> row_data = new Dictionary<string, object>();

                        DateTime latest_date = ReadDateTime(row.GetString(0)).ToLocalTime();
                        row_data.Add("latest_date", latest_date);

                        string user_id = row.GetString(1);
                        row_data.Add("user_id", user_id);

                        int action_count = row.GetInt(2);
                        int seconds_sum = row.GetInt(3);
                        row_data.Add("total_count", action_count);
                        row_data.Add("total_time", seconds_sum);

                        string item_name = row.GetString(4);
                        row_data.Add("item_name", item_name);

                        string client_name = row.GetString(5);
                        row_data.Add("client_name", client_name);

                        int item_id = row.GetInt(6);
                        row_data.Add("item_id", item_id);

                        report.Add(row_data);
                    }
                }
            }

            return report;
        }

        public List<Dictionary<string, object>> GetUserPlayListReport(int days, DateTime end_date, string user_id, string filter_name, bool aggregate_data, string[] types, ReportPlaybackOptions config)
        {
            List<Dictionary<string, object>> report = new List<Dictionary<string, object>>();

            DateTime start_date = end_date.Subtract(new TimeSpan(days, 0, 0, 0));
            Dictionary<String, Dictionary<string, int>> usage = new Dictionary<String, Dictionary<string, int>>();

            string sql = "SELECT ";
            sql += "strftime('%Y-%m-%d', DateCreated) AS PlayDate, ";

            if (aggregate_data)
            {
                sql += "MIN(strftime('%H:%M:%S', DateCreated)) AS PlayTime, ";
                sql += "UserId, ItemName, ItemId, ItemType, SUM(PlayDuration - PauseDuration) AS Duration, ";
                sql += "MAX(RemoteAddress) AS RemoteAddress ";
            }
            else
            {
                sql += "strftime('%H:%M:%S', DateCreated) AS PlayTime, ";
                sql += "UserId, ItemName, ItemId, ItemType, PlayDuration - PauseDuration AS Duration, ";
                sql += "RemoteAddress ";
            }

            sql += "FROM PlaybackActivity ";
            
            sql += "WHERE DateCreated >= @start_date AND DateCreated <= @end_date ";

            if (!string.IsNullOrEmpty(user_id))
            {
                sql += "AND UserId = @user_id ";
            }

            if (!string.IsNullOrEmpty(filter_name))
            {
                filter_name = filter_name.Replace("*", "%");
                sql += "AND ItemName like @filter_name ";
            }

            if (config.IgnoreSmallerThan > 0)
            {
                sql += "AND (PlayDuration - PauseDuration) > " + config.IgnoreSmallerThan + " ";
            }

            if (aggregate_data)
            {
                sql += "GROUP BY PlayDate, UserId, ItemName, ItemId, ItemType ";
                sql += "ORDER BY PlayDate DESC, PlayTime DESC";
            }
            else
            {
                sql += "ORDER BY PlayDate DESC, PlayTime DESC";
            }

            lock (connection)
            {
                using (var statement = connection.PrepareStatement(sql))
                {
                    TryBind(statement, "@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                    TryBind(statement, "@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));
                    TryBind(statement, "@user_id", user_id);
                    TryBind(statement, "@filter_name", filter_name);

                    while (statement.MoveNext())
                    {
                        var row = statement.Current;
                        Dictionary<string, object> row_data = new Dictionary<string, object>();

                        string play_date = row.GetString(0);
                        row_data.Add("date", play_date);

                        string play_time = row.GetString(1);
                        row_data.Add("time", play_time);

                        string user = row.GetString(2);
                        row_data.Add("user_id", user);

                        string item_name = row.GetString(3);
                        row_data.Add("item_name", item_name);

                        int item_id = row.GetInt(4);
                        row_data.Add("item_id", item_id);

                        string item_type = row.GetString(5);
                        row_data.Add("item_type", item_type);

                        string client_name = row.GetString(6);
                        row_data.Add("duration", client_name);

                        string remote_address = row.GetString(7);
                        row_data.Add("remote_address", remote_address);

                        report.Add(row_data);
                    }
                }
            }

            return report;
        }
    }
}
