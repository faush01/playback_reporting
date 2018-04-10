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

        public List<Dictionary<string, string>> GetUsageForUser(string date, string user_id, string[] types)
        {
            List<string> type_list = new List<string>();
            foreach (string media_type in types)
            {
                if (type_map.ContainsKey(media_type))
                {
                    type_list.Add("'" + type_map[media_type] + "'");
                }
            }

            string sql_query = "SELECT DateCreated, ItemId, ItemType, ItemName, ClientName, PlaybackMethod, DeviceName, PlayDuration " +
                               "FROM PlaybackActivity " +
                               "WHERE DateCreated >= @date_from AND DateCreated <= @date_to " +
                               "AND UserId = @user_id " +
                               "AND ItemType IN (" + string.Join(",", type_list) + ") " +
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

                            items.Add(item);
                        }
                    }
                }
            }

            return items;
        }

        public Dictionary<String, Dictionary<string, int>> GetUsageForDays(int numberOfDays, string[] types)
        {
            List<string> type_list = new List<string>();
            foreach (string media_type in types)
            {
                if (type_map.ContainsKey(media_type))
                {
                    type_list.Add("'" + type_map[media_type] + "'");
                }
            }

            string sql_query = "SELECT UserId, strftime('%Y-%m-%d', DateCreated) AS date, COUNT(1) AS count " +
                               "FROM PlaybackActivity " +
                               "WHERE DateCreated >= @start_date " +
                               "AND ItemType IN (" + string.Join(",", type_list) + ") " +
                               "GROUP BY UserId, date " +
                               "ORDER BY UserId, date ASC";

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
