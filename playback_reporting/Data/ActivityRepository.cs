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

using playback_reporting.Api;
using MediaBrowser.Controller;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using SQLitePCL.pretty;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Threading;

namespace playback_reporting.Data
{
    public class ActivityRepository : BaseSqliteRepository, IActivityRepository
    {
        private readonly ILogger _logger;
        protected IFileSystem FileSystem { get; private set; }

        static protected ReaderWriterLockSlim WriteLock2;

        public ActivityRepository(ILogger logger, IServerApplicationPaths appPaths, IFileSystem fileSystem) : base(logger)
        {
            DbFilePath = Path.Combine(appPaths.DataPath, "playback_reporting.db");
            FileSystem = fileSystem;
            _logger = logger;
        }

        public void Initialize()
        {
            try
            {
                InitializeInternal();
            }

            catch (Exception ex)
            {
                Logger.ErrorException("Error loading PlaybackActivity database file.", ex);
                //FileSystem.DeleteFile(DbFilePath);
                //InitializeInternal();
            }
        }

        private void InitializeInternal()
        {
            using (lock_manager.getLockItem().Write())
            {
                using (var connection = CreateConnection())
                {
                    _logger.Info("Initialize PlaybackActivity Repository");

                    string sql_info = "pragma table_info('PlaybackActivity')";
                    List<string> cols = new List<string>();
                    using (var statement = connection.PrepareStatement(sql_info))
                    {
                        foreach (var row in statement.ExecuteQuery())
                        {
                            string table_schema = row.GetString(1).ToLower() + ":" + row.GetString(2).ToLower();
                            cols.Add(table_schema);
                        }
                    }
                    string actual_schema = string.Join("|", cols);
                    string required_schema = "datecreated:datetime|userid:text|itemid:text|itemtype:text|itemname:text|playbackmethod:text|clientname:text|devicename:text|playduration:int|pauseduration:int";
                    if (required_schema != actual_schema)
                    {
                        _logger.Info("PlaybackActivity table schema miss match!");
                        _logger.Info("Expected : " + required_schema);
                        _logger.Info("Received : " + actual_schema);
                        
                        string new_table_name = "PlaybackActivity_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        _logger.Info("Renaming table to : " + new_table_name);
                        try
                        {
                            connection.Execute("ALTER TABLE PlaybackActivity RENAME TO " + new_table_name);
                        }
                        catch(Exception e)
                        {
                            _logger.ErrorException("Error Renaming PlaybackActivity Table to : " + new_table_name, e);
                        }
                        //_logger.Info("Dropping and recreating PlaybackActivity table");
                        //connection.Execute("drop table if exists PlaybackActivity");
                    }
                    else
                    {
                        _logger.Info("PlaybackActivity table schema OK");
                        _logger.Info("Expected : " + required_schema);
                        _logger.Info("Received : " + actual_schema);
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
                                    "PauseDuration INT" +
                                    ")");

                    connection.Execute("create table if not exists UserList (UserId TEXT)");
                }
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
            using (lock_manager.getLockItem().Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement(sql))
                    {
                        statement.TryBind("@start_time", start_date_sql.ToString("yyyy-MM-dd HH:mm:ss"));

                        foreach (var row in statement.ExecuteQuery())
                        {
                            string item_id = row.GetString(0);
                            DateTime start_time = row.ReadDateTime(1).ToLocalTime();
                            int duration = row.GetInt(2);
                            DateTime end_time = start_time.AddSeconds(duration);

                            string start_key = start_time.ToString("yyyy-MM-dd HH:mm:ss.fffff") + "-" + item_id + "-A";
                            KeyValuePair<DateTime, int> data_start = new KeyValuePair<DateTime, int>(start_time, 1);
                            string end_key = end_time.ToString("yyyy-MM-dd HH:mm:ss.fffff") + "-" + item_id + "-B";
                            KeyValuePair<DateTime, int> data_end = new KeyValuePair<DateTime, int>(end_time, 0);

                            _logger.Info("Play Action : " + start_key + " - " + end_key);

                            actions.Add(start_key, data_start);
                            actions.Add(end_key, data_end);
                        }
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

                _logger.Info("Play Count : " + key + " | " + data.Value + " | " + data_timestamp.ToString("yyyy-MM-dd HH:mm:ss") + " | " + count);
                results.Add(new KeyValuePair<string, int>(data_timestamp.ToString("yyyy-MM-dd HH:mm:ss"), count));
            }

            return results;
        }

        public string RunCustomQuery(string query_string, List<string> col_names, List<List<object>> results)
        {
            string message = "";
            int change_count = 0;
            using (lock_manager.getLockItem().Write())
            {
                using (var connection = CreateConnection(true))
                {
                    try
                    {
                        using (var statement = connection.PrepareStatement(query_string))
                        {
                            var result_set = statement.ExecuteQuery();

                            foreach (var col in statement.Columns)
                            {
                                col_names.Add(col.Name);
                            }

                            int col_count = statement.Columns.Count;

                            foreach (var row in result_set)
                            {
                                List<object> row_date = new List<object>();
                                for(int x = 0; x < col_count; x++)
                                {
                                    string cell_data = row.GetString(x);
                                    row_date.Add(cell_data);
                                }
                                results.Add(row_date);
                            }
                            change_count = connection.TotalChanges;
                        }
                    }
                    catch(Exception e)
                    {
                        _logger.ErrorException("Error in SQL", e);
                        message = "Error Running Query</br>" + e.Message;
                        message += "<pre>" + e.ToString() + "</pre>";
                    }
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
                               "where UserId not in ('" + string.Join("', '", known_user_ids) + "') ";

            using (lock_manager.getLockItem().Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        db.Execute(sql_query);
                    }, TransactionMode);
                }
            }
            return 1;
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
            using (lock_manager.getLockItem().Write())
            {
                using (var connection = CreateConnection(true))
                {
                    connection.RunInTransaction(db =>
                    {
                        using (var statement = db.PrepareStatement(sql))
                        {
                            statement.TryBind("@id", id);
                            statement.MoveNext();
                        }
                    }, TransactionMode);
                }
            }
        }

        public List<string> GetUserList()
        {
            List<string> user_id_list = new List<string>();
            using (lock_manager.getLockItem().Read())
            {
                using (var connection = CreateConnection(true))
                {
                    string sql_query = "select UserId from UserList";
                    using (var statement = connection.PrepareStatement(sql_query))
                    {
                        foreach (var row in statement.ExecuteQuery())
                        {
                            string type = row.GetString(0);
                            user_id_list.Add(type);
                        }
                    }
                }
            }

            return user_id_list;
        }

        public List<string> GetTypeFilterList()
        {
            List<string> filter_Type_list = new List<string>();
            using (lock_manager.getLockItem().Read())
            {
                using (var connection = CreateConnection(true))
                {
                    string sql_query = "select distinct ItemType from PlaybackActivity";
                    using (var statement = connection.PrepareStatement(sql_query))
                    {
                        foreach (var row in statement.ExecuteQuery())
                        {
                            string type = row.GetString(0);
                            filter_Type_list.Add(type);
                        }
                    }
                }
            }
            return filter_Type_list;
        }

        public int ImportRawData(string data)
        {
            int count = 0;
            _logger.Info("Loading Data");
            using (lock_manager.getLockItem().Write())
            {
                using (var connection = CreateConnection(true))
                {
                    StringReader sr = new StringReader(data);

                    string line = sr.ReadLine();
                    while (line != null)
                    {
                        string[] tokens = line.Split('\t');
                        _logger.Info("Line Length : " + tokens.Length);
                        if (tokens.Length != 10)
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

                        //_logger.Info(date + "\t" + user_id + "\t" + item_id + "\t" + item_type + "\t" + item_name + "\t" + play_method + "\t" + client_name + "\t" + device_name + "\t" + duration);

                        string sql = "select rowid from PlaybackActivity where DateCreated = @DateCreated and UserId = @UserId and ItemId = @ItemId";
                        using (var statement = connection.PrepareStatement(sql))
                        {

                            statement.TryBind("@DateCreated", date);
                            statement.TryBind("@UserId", user_id);
                            statement.TryBind("@ItemId", item_id);
                            bool found = false;
                            foreach (var row in statement.ExecuteQuery())
                            {
                                found = true;
                                break;
                            }

                            if (found == false)
                            {
                                _logger.Info("Not Found, Adding");

                                string sql_add = "insert into PlaybackActivity " +
                                    "(DateCreated, UserId, ItemId, ItemType, ItemName, PlaybackMethod, ClientName, DeviceName, PlayDuration, PauseDuration) " +
                                    "values " +
                                    "(@DateCreated, @UserId, @ItemId, @ItemType, @ItemName, @PlaybackMethod, @ClientName, @DeviceName, @PlayDuration, @PauseDuration)";

                                connection.RunInTransaction(db =>
                                {
                                    using (var add_statment = db.PrepareStatement(sql_add))
                                    {
                                        add_statment.TryBind("@DateCreated", date);
                                        add_statment.TryBind("@UserId", user_id);
                                        add_statment.TryBind("@ItemId", item_id);
                                        add_statment.TryBind("@ItemType", item_type);
                                        add_statment.TryBind("@ItemName", item_name);
                                        add_statment.TryBind("@PlaybackMethod", play_method);
                                        add_statment.TryBind("@ClientName", client_name);
                                        add_statment.TryBind("@DeviceName", device_name);
                                        add_statment.TryBind("@PlayDuration", play_duration);
                                        add_statment.TryBind("@PauseDuration", paused_duration);
                                        add_statment.MoveNext();
                                    }
                                }, TransactionMode);
                                count++;
                            }
                            else
                            {
                                //_logger.Info("Found, ignoring");
                            }
                        }

                        line = sr.ReadLine();
                    }
                }
            }
            return count;
        }

        public string ExportRawData()
        {
            StringWriter sw = new StringWriter();

            string sql_raw = "SELECT * FROM PlaybackActivity ORDER BY DateCreated";
            using (lock_manager.getLockItem().Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement(sql_raw))
                    {
                        var row_set = statement.ExecuteQuery();
                        int col_count = statement.Columns.Count;
                        foreach (var row in row_set)
                        {
                            List<string> row_data = new List<string>();
                            for (int x = 0; x < col_count; x++)
                            {
                                row_data.Add(row.GetString(x));
                            }
                            sw.WriteLine(string.Join("\t", row_data));
                        }
                    }
                }
            }
            sw.Flush();
            return sw.ToString();
        }

        public void DeleteOldData(DateTime? del_before)
        {
            string sql = "delete from PlaybackActivity";
            if (del_before != null)
            {
                DateTime date = (DateTime)del_before;
                sql += " where DateCreated < '" + date.ToDateTimeParamValue() + "'";
            }

            _logger.Info("DeleteOldData : " + sql);

            using (lock_manager.getLockItem().Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        db.Execute(sql);
                    }, TransactionMode);
                }
            }
        }

        public void AddPlaybackAction(PlaybackInfo play_info)
        {
            string sql_add = "insert into PlaybackActivity " +
                "(DateCreated, UserId, ItemId, ItemType, ItemName, PlaybackMethod, ClientName, DeviceName, PlayDuration, PauseDuration) " +
                "values " +
                "(@DateCreated, @UserId, @ItemId, @ItemType, @ItemName, @PlaybackMethod, @ClientName, @DeviceName, @PlayDuration, @PauseDuration)";

            using (lock_manager.getLockItem().Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        using (var statement = db.PrepareStatement(sql_add))
                        {
                            statement.TryBind("@DateCreated", play_info.Date.ToDateTimeParamValue());
                            statement.TryBind("@UserId", play_info.UserId);
                            statement.TryBind("@ItemId", play_info.ItemId);
                            statement.TryBind("@ItemType", play_info.ItemType);
                            statement.TryBind("@ItemName", play_info.ItemName);
                            statement.TryBind("@PlaybackMethod", play_info.PlaybackMethod);
                            statement.TryBind("@ClientName", play_info.ClientName);
                            statement.TryBind("@DeviceName", play_info.DeviceName);
                            statement.TryBind("@PlayDuration", play_info.PlaybackDuration);
                            statement.TryBind("@PauseDuration", play_info.PausedDuration);
                            statement.MoveNext();
                        }
                    }, TransactionMode);
                }
            }
        }

        public void UpdatePlaybackAction(PlaybackInfo play_info)
        {
            string sql_add = "update PlaybackActivity set PlayDuration = @PlayDuration, PauseDuration = @PauseDuration where DateCreated = @DateCreated and UserId = @UserId and ItemId = @ItemId";
            using (lock_manager.getLockItem().Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        using (var statement = db.PrepareStatement(sql_add))
                        {
                            statement.TryBind("@DateCreated", play_info.Date.ToDateTimeParamValue());
                            statement.TryBind("@UserId", play_info.UserId);
                            statement.TryBind("@ItemId", play_info.ItemId);
                            statement.TryBind("@PlayDuration", play_info.PlaybackDuration);
                            statement.TryBind("@PauseDuration", play_info.PausedDuration);
                            statement.MoveNext();
                        }
                    }, TransactionMode);
                }
            }
        }

        public List<Dictionary<string, string>> GetUsageForUser(string date, string user_id, string[] types)
        {
            List<string> filters = new List<string>();
            foreach (string filter in types)
            {
                filters.Add("'" + filter + "'");
            }

            string sql_query = "SELECT DateCreated, ItemId, ItemType, ItemName, ClientName, PlaybackMethod, DeviceName, (PlayDuration - PauseDuration) AS PlayDuration , rowid " +
                               "FROM PlaybackActivity " +
                               "WHERE DateCreated >= @date_from AND DateCreated <= @date_to " +
                               "AND UserId = @user_id " +
                               "AND ItemType IN (" + string.Join(",", filters) + ") " +
                               "ORDER BY DateCreated";

            List<Dictionary<string, string>> items = new List<Dictionary<string, string>>();
            using (lock_manager.getLockItem().Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement(sql_query))
                    {
                        statement.TryBind("@date_from", date + " 00:00:00");
                        statement.TryBind("@date_to", date + " 23:59:59");
                        statement.TryBind("@user_id", user_id);
                        foreach (var row in statement.ExecuteQuery())
                        {
                            string item_id = row.GetString(1);

                            Dictionary<string, string> item = new Dictionary<string, string>();
                            item["Time"] = row.ReadDateTime(0).ToLocalTime().ToString("HH:mm");
                            item["Id"] = row.GetString(1);
                            item["Type"] = row.GetString(2);
                            item["ItemName"] = row.GetString(3);
                            item["ClientName"] = row.GetString(4);
                            item["PlaybackMethod"] = row.GetString(5);
                            item["DeviceName"] = row.GetString(6);
                            item["PlayDuration"] = row.GetString(7);
                            item["RowId"] = row.GetString(8);

                            items.Add(item);
                        }
                    }
                }
            }

            return items;
        }

        public Dictionary<String, Dictionary<string, int>> GetUsageForDays(int days, DateTime end_date, string[] types, string data_type)
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
            sql_query += "GROUP BY UserId, date ORDER BY UserId, date ASC";

            DateTime start_date = end_date.Subtract(new TimeSpan(days, 0, 0, 0));
            Dictionary<String, Dictionary<string, int>> usage = new Dictionary<String, Dictionary<string, int>>();

            using (lock_manager.getLockItem().Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement(sql_query))
                    {
                        statement.TryBind("@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                        statement.TryBind("@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));

                        foreach (var row in statement.ExecuteQuery())
                        {
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
            }

            return usage;
        }

        public SortedDictionary<string, int> GetHourlyUsageReport(string user_id, int days, DateTime end_date, string[] types)
        {
            List<string> filters = new List<string>();
            foreach (string filter in types)
            {
                filters.Add("'" + filter + "'");
            }

            SortedDictionary<string, int> report_data = new SortedDictionary<string, int>();

            DateTime start_date = end_date.Subtract(new TimeSpan(days, 0, 0, 0));

            string sql = "SELECT DateCreated, PlayDuration ";
            sql += "FROM PlaybackActivity ";
            sql += "WHERE DateCreated >= @start_date AND DateCreated <= @end_date ";
            sql += "AND UserId not IN (select UserId from UserList) ";
            sql += "AND ItemType IN (" + string.Join(",", filters) + ")";

            if (!string.IsNullOrEmpty(user_id))
            {
                sql += " AND UserId = @user_id";
            }

            using (lock_manager.getLockItem().Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement(sql))
                    {
                        statement.TryBind("@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                        statement.TryBind("@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));
                        statement.TryBind("@user_id", user_id);

                        foreach (var row in statement.ExecuteQuery())
                        {
                            DateTime date = row.ReadDateTime(0).ToLocalTime();
                            int duration = row.GetInt(1);

                            int seconds_left_in_hour = 3600 - ((date.Minute * 60) + date.Second);
                            _logger.Info("Processing - date: " + date.ToString() + " duration: " + duration + " seconds_left_in_hour: " + seconds_left_in_hour);
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
            }

            return report_data;
        }

        private void AddTimeToHours(SortedDictionary<string, int> report_data, string key, int count)
        {
            _logger.Info("Adding Time : " + key + " - " + count);
            if (report_data.ContainsKey(key))
            {
                report_data[key] += count;
            }
            else
            {
                report_data.Add(key, count);
            }
        }

        public List<Dictionary<string, object>> GetBreakdownReport(string user_id, int days, DateTime end_date, string type)
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

            sql += "GROUP BY " + type;

            using (lock_manager.getLockItem().Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement(sql))
                    {
                        statement.TryBind("@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                        statement.TryBind("@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));
                        statement.TryBind("@user_id", user_id);

                        foreach (var row in statement.ExecuteQuery())
                        {
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
            }

            return report;
        }

        public List<Dictionary<string, object>> GetTvShowReport(string user_id, int days, DateTime end_date)
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

            sql += "GROUP BY name";

            using (lock_manager.getLockItem().Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement(sql))
                    {
                        statement.TryBind("@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                        statement.TryBind("@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));
                        statement.TryBind("@user_id", user_id);

                        foreach (var row in statement.ExecuteQuery())
                        {
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
            }

            return report;
        }

        public List<Dictionary<string, object>> GetMoviesReport(string user_id, int days, DateTime end_date)
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

            sql += "GROUP BY name";

            using (lock_manager.getLockItem().Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement(sql))
                    {
                        statement.TryBind("@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                        statement.TryBind("@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));
                        statement.TryBind("@user_id", user_id);

                        foreach (var row in statement.ExecuteQuery())
                        {
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
            }

            return report;
        }

        public List<Dictionary<string, object>> GetUserReport(int days, DateTime end_date)
        {
            List<Dictionary<string, object>> report = new List<Dictionary<string, object>>();

            DateTime start_date = end_date.Subtract(new TimeSpan(days, 0, 0, 0));
            Dictionary<String, Dictionary<string, int>> usage = new Dictionary<String, Dictionary<string, int>>();

            string sql = "";
            sql += "SELECT x.latest_date, x.UserId, x.play_count, x.total_duarion, y.ItemName, y.DeviceName ";
            sql += "FROM( ";
            sql += "SELECT MAX(DateCreated) AS latest_date, UserId, COUNT(1) AS play_count, SUM(PlayDuration - PauseDuration) AS total_duarion ";
            sql += "FROM PlaybackActivity ";
            sql += "WHERE DateCreated >= @start_date AND DateCreated <= @end_date ";
            sql += "AND UserId not IN (select UserId from UserList) ";
            sql += "GROUP BY UserId ";
            sql += ") AS x ";
            sql += "INNER JOIN PlaybackActivity AS y ON x.latest_date = y.DateCreated AND x.UserId = y.UserId ";
            sql += "ORDER BY x.latest_date DESC";

            using (lock_manager.getLockItem().Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement(sql))
                    {
                        statement.TryBind("@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                        statement.TryBind("@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));

                        foreach (var row in statement.ExecuteQuery())
                        {
                            Dictionary<string, object> row_data = new Dictionary<string, object>();

                            DateTime latest_date = row.ReadDateTime(0).ToLocalTime();
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

                            report.Add(row_data);
                        }
                    }
                }
            }

            return report;
        }

        public List<Dictionary<string, object>> GetUserPlayListReport(int days, DateTime end_date, string user_id, string filter_name, bool aggregate_data, string[] types)
        {
            List<Dictionary<string, object>> report = new List<Dictionary<string, object>>();

            DateTime start_date = end_date.Subtract(new TimeSpan(days, 0, 0, 0));
            Dictionary<String, Dictionary<string, int>> usage = new Dictionary<String, Dictionary<string, int>>();

            string sql = "SELECT ";
            sql += "strftime('%Y-%m-%d', DateCreated) AS PlayDate, ";

            if (aggregate_data)
            {
                sql += "MIN(strftime('%H:%M:%S', DateCreated)) AS PlayTime, ";
                sql += "UserId, ItemName, ItemType, SUM(PlayDuration - PauseDuration) AS Duration ";
            }
            else
            {
                sql += "strftime('%H:%M:%S', DateCreated) AS PlayTime, ";
                sql += "UserId, ItemName, ItemType, PlayDuration - PauseDuration AS Duration ";
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

            if (aggregate_data)
            {
                sql += "GROUP BY PlayDate, UserId, ItemName, ItemType ";
                sql += "ORDER BY PlayDate DESC, PlayTime DESC";
            }
            else
            {
                sql += "ORDER BY PlayDate DESC, PlayTime DESC";
            }

            using (lock_manager.getLockItem().Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement(sql))
                    {
                        statement.TryBind("@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                        statement.TryBind("@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));
                        statement.TryBind("@user_id", user_id);
                        statement.TryBind("@filter_name", filter_name);

                        foreach (var row in statement.ExecuteQuery())
                        {
                            Dictionary<string, object> row_data = new Dictionary<string, object>();

                            string play_date = row.GetString(0);
                            row_data.Add("date", play_date);

                            string play_time = row.GetString(1);
                            row_data.Add("time", play_time);

                            string user = row.GetString(2);
                            row_data.Add("user", user);

                            string item_name = row.GetString(3);
                            row_data.Add("name", item_name);

                            string item_type = row.GetString(4);
                            row_data.Add("type", item_type);

                            string client_name = row.GetString(5);
                            row_data.Add("duration", client_name);

                            report.Add(row_data);
                        }
                    }
                }
            }

            return report;
        }
    }
}
