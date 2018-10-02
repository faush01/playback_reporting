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

namespace playback_reporting.Data
{
    public class ActivityRepository : BaseSqliteRepository, IActivityRepository
    {
        private readonly ILogger _logger;
        protected IFileSystem FileSystem { get; private set; }

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
            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    _logger.Info("Initialize PlaybackActivity Repository");

                    string sql_info = "pragma table_info('PlaybackActivity')";
                    List<string> cols = new List<string>();
                    foreach (var row in connection.Query(sql_info))
                    {
                        string table_schema = row[1].ToString().ToLower() + ":" + row[2].ToString().ToLower();
                        cols.Add(table_schema);
                    }
                    string actual_schema = string.Join("|", cols);
                    string required_schema = "datecreated:datetime|userid:text|itemid:text|itemtype:text|itemname:text|playbackmethod:text|clientname:text|devicename:text|playduration:int";
                    if (required_schema != actual_schema)
                    {
                        _logger.Info("PlaybackActivity table schema miss match!");
                        _logger.Info("Expected : " + required_schema);
                        _logger.Info("Received : " + actual_schema);
                        _logger.Info("Dropping and recreating PlaybackActivity table");
                        connection.Execute("drop table if exists PlaybackActivity");
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
                                    "PlayDuration INT" +
                                    ")");

                    connection.Execute("create table if not exists UserList (UserId TEXT)");
                }
            }
        }

        public string RunCustomQuery(string query_string, List<string> col_names, List<List<object>> results)
        {
            string message = "";
            bool columns_done = false;
            int change_count = 0;
            using (WriteLock.Write())
            {
                using (var connection = CreateConnection(true))
                {
                    try
                    {
                        using (var statement = connection.PrepareStatement(query_string))
                        {
                            foreach (var row in statement.ExecuteQuery())
                            {
                                if (!columns_done)
                                {
                                    foreach (var col in row.Columns())
                                    {
                                        col_names.Add(col.Name);
                                    }
                                    columns_done = true;
                                }

                                List<object> row_date = new List<object>();
                                for(int x = 0; x < row.Count; x++)
                                {
                                    string cell_data = row[x].ToString();
                                    row_date.Add(cell_data);
                                }
                                results.Add(row_date);

                                string type = row[0].ToString();
                            }
                            change_count = connection.GetChangeCount();
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

            using (WriteLock.Write())
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
            using (WriteLock.Write())
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
            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    string sql_query = "select UserId from UserList";
                    using (var statement = connection.PrepareStatement(sql_query))
                    {
                        foreach (var row in statement.ExecuteQuery())
                        {
                            string type = row[0].ToString();
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
            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    string sql_query = "select distinct ItemType from PlaybackActivity";
                    using (var statement = connection.PrepareStatement(sql_query))
                    {
                        foreach (var row in statement.ExecuteQuery())
                        {
                            string type = row[0].ToString();
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
            using (WriteLock.Write())
            {
                using (var connection = CreateConnection(true))
                {
                    StringReader sr = new StringReader(data);

                    string line = sr.ReadLine();
                    while (line != null)
                    {
                        string[] tokens = line.Split('\t');
                        _logger.Info("Line Length : " + tokens.Length);
                        if (tokens.Length != 9)
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
                        string duration = tokens[8];

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
                                    "(DateCreated, UserId, ItemId, ItemType, ItemName, PlaybackMethod, ClientName, DeviceName, PlayDuration) " +
                                    "values " +
                                    "(@DateCreated, @UserId, @ItemId, @ItemType, @ItemName, @PlaybackMethod, @ClientName, @DeviceName, @PlayDuration)";

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
                                        add_statment.TryBind("@PlayDuration", duration);
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
            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement(sql_raw))
                    {
                        foreach (var row in statement.ExecuteQuery())
                        {
                            List<string> row_data = new List<string>();
                            for (int x = 0; x < row.Count; x++)
                            {
                                row_data.Add(row[x].ToString());
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

            using (WriteLock.Write())
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
                "(DateCreated, UserId, ItemId, ItemType, ItemName, PlaybackMethod, ClientName, DeviceName, PlayDuration) " +
                "values " +
                "(@DateCreated, @UserId, @ItemId, @ItemType, @ItemName, @PlaybackMethod, @ClientName, @DeviceName, @PlayDuration)";

            using (WriteLock.Write())
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
                            statement.MoveNext();
                        }
                    }, TransactionMode);
                }
            }
        }

        public void UpdatePlaybackAction(PlaybackInfo play_info)
        {
            string sql_add = "update PlaybackActivity set PlayDuration = @PlayDuration where DateCreated = @DateCreated and UserId = @UserId and ItemId = @ItemId";
            using (WriteLock.Write())
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

            string sql_query = "SELECT DateCreated, ItemId, ItemType, ItemName, ClientName, PlaybackMethod, DeviceName, PlayDuration, rowid " +
                               "FROM PlaybackActivity " +
                               "WHERE DateCreated >= @date_from AND DateCreated <= @date_to " +
                               "AND UserId = @user_id " +
                               "AND ItemType IN (" + string.Join(",", filters) + ") " +
                               "ORDER BY DateCreated";

            List<Dictionary<string, string>> items = new List<Dictionary<string, string>>();
            using (WriteLock.Read())
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
                            string item_id = row[1].ToString();

                            Dictionary<string, string> item = new Dictionary<string, string>();
                            item["Time"] = row[0].ReadDateTime().ToLocalTime().ToString("HH:mm");
                            item["Id"] = row[1].ToString();
                            item["Type"] = row[2].ToString();
                            item["ItemName"] = row[3].ToString();
                            item["ClientName"] = row[4].ToString();
                            item["PlaybackMethod"] = row[5].ToString();
                            item["DeviceName"] = row[6].ToString();
                            item["PlayDuration"] = row[7].ToString();
                            item["RowId"] = row[8].ToString();

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
                sql_query += "SELECT UserId, strftime('%Y-%m-%d', DateCreated) AS date, SUM(PlayDuration) AS count ";
            }
            sql_query += "FROM PlaybackActivity ";
            sql_query += "WHERE DateCreated >= @start_date AND DateCreated <= @end_date ";
            sql_query += "AND ItemType IN (" + string.Join(",", filters) + ") ";
            sql_query += "AND UserId not IN (select UserId from UserList) ";
            sql_query += "GROUP BY UserId, date ORDER BY UserId, date ASC";

            DateTime start_date = end_date.Subtract(new TimeSpan(days, 0, 0, 0));
            Dictionary<String, Dictionary<string, int>> usage = new Dictionary<String, Dictionary<string, int>>();

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement(sql_query))
                    {
                        statement.TryBind("@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                        statement.TryBind("@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));

                        foreach (var row in statement.ExecuteQuery())
                        {
                            string user_id = row[0].ToString();
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
                            string date_string = row[1].ToString();
                            int count_int = row[2].ToInt();
                            uu.Add(date_string, count_int);
                        }
                    }
                }
            }

            return usage;
        }

        public SortedDictionary<string, int> GetHourlyUsageReport(int days, DateTime end_date, string[] types)
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

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement(sql))
                    {
                        statement.TryBind("@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                        statement.TryBind("@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));

                        foreach (var row in statement.ExecuteQuery())
                        {
                            DateTime date = row[0].ReadDateTime().ToLocalTime();
                            int duration = row[1].ToInt();

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

        public List<Dictionary<string, object>> GetBreakdownReport(int days, DateTime end_date, string type)
        {
            // UserId ItemType PlaybackMethod ClientName DeviceName

            List<Dictionary<string, object>> report = new List<Dictionary<string, object>>();

            DateTime start_date = end_date.Subtract(new TimeSpan(days, 0, 0, 0));
            Dictionary<String, Dictionary<string, int>> usage = new Dictionary<String, Dictionary<string, int>>();

            string sql = "SELECT " + type + ", COUNT(1) AS PlayCount, SUM(PlayDuration) AS Seconds ";
            sql += "FROM PlaybackActivity ";
            sql += "WHERE DateCreated >= @start_date AND DateCreated <= @end_date ";
            sql += "AND UserId not IN (select UserId from UserList) ";
            sql += "GROUP BY " + type;

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement(sql))
                    {
                        statement.TryBind("@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                        statement.TryBind("@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));

                        foreach (var row in statement.ExecuteQuery())
                        {
                            string item_label = row[0].ToString();
                            int action_count = row[1].ToInt();
                            int seconds_sum = row[2].ToInt();

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

        public SortedDictionary<int, int> GetDurationHistogram(int days, DateTime end_date, string[] types)
        {
            /*
            SELECT CAST(PlayDuration / 300 as int) AS FiveMinBlock, COUNT(1) ActionCount 
            FROM PlaybackActivity 
            GROUP BY CAST(PlayDuration / 300 as int)
            ORDER BY CAST(PlayDuration / 300 as int) ASC;
            */

            List<string> filters = new List<string>();
            foreach (string filter in types)
            {
                filters.Add("'" + filter + "'");
            }

            SortedDictionary<int, int> report = new SortedDictionary<int, int>();

            DateTime start_date = end_date.Subtract(new TimeSpan(days, 0, 0, 0));
            Dictionary<String, Dictionary<string, int>> usage = new Dictionary<String, Dictionary<string, int>>();

            string sql =
                "SELECT CAST(PlayDuration / 300 as int) AS FiveMinBlock, COUNT(1) ActionCount " +
                "FROM PlaybackActivity " +
                "WHERE DateCreated >= @start_date AND DateCreated <= @end_date " +
                "AND UserId not IN (select UserId from UserList) " +
                "AND ItemType IN (" + string.Join(",", filters) + ") " +
                "GROUP BY CAST(PlayDuration / 300 as int) " +
                "ORDER BY CAST(PlayDuration / 300 as int) ASC";

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement(sql))
                    {
                        statement.TryBind("@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                        statement.TryBind("@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));

                        foreach (var row in statement.ExecuteQuery())
                        {
                            int block_num = row[0].ToInt();
                            int count = row[1].ToInt();
                            report.Add(block_num, count);
                        }
                    }
                }
            }

            return report;
        }

        public List<Dictionary<string, object>> GetTvShowReport(int days, DateTime end_date)
        {
            List<Dictionary<string, object>> report = new List<Dictionary<string, object>>();

            DateTime start_date = end_date.Subtract(new TimeSpan(days, 0, 0, 0));
            Dictionary<String, Dictionary<string, int>> usage = new Dictionary<String, Dictionary<string, int>>();

            string sql = "";
            sql += "SELECT substr(ItemName,0, instr(ItemName, ' - ')) AS name, ";
            sql += "COUNT(1) AS play_count, ";
            sql += "SUM(PlayDuration) AS total_duarion ";
            sql += "FROM PlaybackActivity ";
            sql += "WHERE ItemType = 'Episode' ";
            sql += "AND DateCreated >= @start_date AND DateCreated <= @end_date ";
            sql += "AND UserId not IN (select UserId from UserList) ";
            sql += "GROUP BY name";

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement(sql))
                    {
                        statement.TryBind("@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                        statement.TryBind("@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));

                        foreach (var row in statement.ExecuteQuery())
                        {
                            string item_label = row[0].ToString();
                            int action_count = row[1].ToInt();
                            int seconds_sum = row[2].ToInt();

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

        public List<Dictionary<string, object>> GetMoviesReport(int days, DateTime end_date)
        {
            List<Dictionary<string, object>> report = new List<Dictionary<string, object>>();

            DateTime start_date = end_date.Subtract(new TimeSpan(days, 0, 0, 0));
            Dictionary<String, Dictionary<string, int>> usage = new Dictionary<String, Dictionary<string, int>>();

            string sql = "";
            sql += "SELECT ItemName AS name, ";
            sql += "COUNT(1) AS play_count, ";
            sql += "SUM(PlayDuration) AS total_duarion ";
            sql += "FROM PlaybackActivity ";
            sql += "WHERE ItemType = 'Movie' ";
            sql += "AND DateCreated >= @start_date AND DateCreated <= @end_date ";
            sql += "AND UserId not IN (select UserId from UserList) ";
            sql += "GROUP BY name";

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement(sql))
                    {
                        statement.TryBind("@start_date", start_date.ToString("yyyy-MM-dd 00:00:00"));
                        statement.TryBind("@end_date", end_date.ToString("yyyy-MM-dd 23:59:59"));

                        foreach (var row in statement.ExecuteQuery())
                        {
                            string item_label = row[0].ToString();
                            int action_count = row[1].ToInt();
                            int seconds_sum = row[2].ToInt();

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
            sql += "SELECT MAX(DateCreated) AS latest_date, UserId, COUNT(1) AS play_count, SUM(PlayDuration) AS total_duarion ";
            sql += "FROM PlaybackActivity ";
            sql += "WHERE DateCreated >= @start_date AND DateCreated <= @end_date ";
            sql += "AND UserId not IN (select UserId from UserList) ";
            sql += "GROUP BY UserId ";
            sql += ") AS x ";
            sql += "INNER JOIN PlaybackActivity AS y ON x.latest_date = y.DateCreated AND x.UserId = y.UserId ";
            sql += "ORDER BY x.latest_date DESC";

            using (WriteLock.Read())
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

                            DateTime latest_date = row[0].ReadDateTime().ToLocalTime();
                            row_data.Add("latest_date", latest_date);

                            string user_id = row[1].ToString();
                            row_data.Add("user_id", user_id);

                            int action_count = row[2].ToInt();
                            int seconds_sum = row[3].ToInt();
                            row_data.Add("total_count", action_count);
                            row_data.Add("total_time", seconds_sum);

                            string item_name = row[4].ToString();
                            row_data.Add("item_name", item_name);

                            string client_name = row[5].ToString();
                            row_data.Add("client_name", client_name);

                            report.Add(row_data);
                        }
                    }
                }
            }

            return report;
        }
    }
}
