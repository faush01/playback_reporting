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
        private Dictionary<string, string> type_map = new Dictionary<string, string>();

        public ActivityRepository(ILogger logger, IServerApplicationPaths appPaths, IFileSystem fileSystem) : base(logger)
        {
            DbFilePath = Path.Combine(appPaths.DataPath, "playback_reporting.db");
            FileSystem = fileSystem;
            _logger = logger;

            type_map.Add("movies", "Movie");
            type_map.Add("series", "Episode");
        }

        public void Initialize()
        {
            try
            {
                InitializeInternal();
            }

            catch (Exception ex)
            {
                Logger.ErrorException("Error loading database file. Will reset and retry.", ex);
                FileSystem.DeleteFile(DbFilePath);
                InitializeInternal();
            }
        }

        private void InitializeInternal()
        {
            using (var connection = CreateConnection())
            {
                _logger.Info("Initialize Repository");

                string sql_info = "pragma table_info('PlaybackActivity')";
                List<string> cols = new List<string>();
                foreach (var row in connection.Query(sql_info))
                {
                    string table_schema = row[1].ToString().ToLower() + ":" + row[2].ToString().ToLower();
                    cols.Add(table_schema);
                }
                string actual_schema = string.Join("|", cols);
                string required_schema = "datecreated:datetime|userid:text|itemid:text|itemtype:text|itemname:text|playbackmethod:text|clientname:text|devicename:text|playduration:int";
                if(required_schema != actual_schema)
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
            }
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

                            if(found == false)
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
            if(del_before != null)
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
            bool show_all_types = false;
            List<string> type_list = new List<string>();
            foreach (string media_type in types)
            {
                if (type_map.ContainsKey(media_type))
                {
                    type_list.Add("'" + type_map[media_type] + "'");
                }
                if ("all".Equals(media_type, StringComparison.CurrentCultureIgnoreCase))
                {
                    show_all_types = true;
                }
            }

            string sql_query = "SELECT DateCreated, ItemId, ItemType, ItemName, ClientName, PlaybackMethod, DeviceName, PlayDuration " +
                               "FROM PlaybackActivity " +
                               "WHERE DateCreated >= @date_from AND DateCreated <= @date_to " +
                               "AND UserId = @user_id ";
            if (show_all_types == false)
            {
                sql_query += "AND ItemType IN (" + string.Join(",", type_list) + ") ";
            }
            sql_query += "ORDER BY DateCreated";

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

                            items.Add(item);
                        }
                    }
                }
            }

            return items;
        }

        public Dictionary<String, Dictionary<string, int>> GetUsageForDays(int numberOfDays, string[] types, string data_type)
        {
            bool show_all_types = false;
            List<string> type_list = new List<string>();
            foreach (string media_type in types)
            {
                if (type_map.ContainsKey(media_type))
                {
                    type_list.Add("'" + type_map[media_type] + "'");
                }
                if ("all".Equals(media_type, StringComparison.CurrentCultureIgnoreCase))
                {
                    show_all_types = true;
                }
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
            sql_query += "FROM PlaybackActivity WHERE DateCreated >= @start_date ";
            if (show_all_types == false)
            {
                sql_query += "AND ItemType IN (" + string.Join(",", type_list) + ") ";
            }
            sql_query += "GROUP BY UserId, date ORDER BY UserId, date ASC";

            DateTime from_date = DateTime.Now.Subtract(new TimeSpan(numberOfDays, 0, 0, 0));
            Dictionary<String, Dictionary<string, int>> usage = new Dictionary<String, Dictionary<string, int>>();

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement(sql_query))
                    {
                        statement.TryBind("@start_date", from_date.ToString("yyyy-MM-dd"));

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
    }
}
