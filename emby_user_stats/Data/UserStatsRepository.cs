using emby_user_stats.Api;
using MediaBrowser.Controller;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using SQLitePCL.pretty;
using System;
using System.Collections.Generic;
using System.IO;

namespace emby_user_stats.Data
{
    public class UserStatsRepository : BaseSqliteRepository, IUserStatsRepository
    {
        protected IFileSystem FileSystem { get; private set; }

        public UserStatsRepository(ILogger logger, IServerApplicationPaths appPaths, IFileSystem fileSystem) : base(logger)
        {
            DbFilePath = Path.Combine(appPaths.DataPath, "user_usage_stats.db");
            FileSystem = fileSystem;
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
                connection.Execute("create table if not exists UserUsageActions (" + 
                                   "Id GUID PRIMARY KEY NOT NULL, " +
                                   "DateCreated DATETIME NOT NULL, " +
                                   "ActionType TEXT, " +
                                   "UserId TEXT, " +
                                   "ItemId TEXT, " + 
                                   "ItemType TEXT" + 
                                   ")");
                connection.Execute("create index if not exists idx_UserUsageActions on UserUsageActions(Id)");
            }
        }

        public void AddUserAction(UserAction entry)
        {
            string sql_add = "replace into UserUsageActions " +
                "(Id, UserId, ItemId, ItemType, ActionType, DateCreated) " +
                "values " +
                "(@Id, @UserId, @ItemId, @ItemType, @ActionType, @DateCreated)";

            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }
            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        using (var statement = db.PrepareStatement(sql_add))
                        {
                            statement.TryBind("@Id", entry.Id.ToGuidBlob());
                            statement.TryBind("@UserId", entry.UserId);
                            statement.TryBind("@ItemId", entry.ItemId);
                            statement.TryBind("@ItemType", entry.ItemType);
                            statement.TryBind("@ActionType", entry.ActionType);
                            statement.TryBind("@DateCreated", entry.Date.ToDateTimeParamValue());
                            statement.MoveNext();
                        }
                    }, TransactionMode);
                }
            }
        }

        public QueryResult<UserAction> GetUserActions(DateTime? minDate, int? startIndex, int? limit)
        {
            var result = new QueryResult<UserAction>();

            var list = new List<UserAction>();

            result.Items = list.ToArray();

            return result;
        }

        public List<ReportDayUsage> GetUsageForUser(string start_date, string user_id)
        {
            string sql_query = "SELECT strftime('%Y-%m-%d', DateCreated) AS date, COUNT(1) AS count " +
                               "FROM UserUsageActions " +
                               "WHERE DateCreated >= @start_date " +
                               "AND UserId = @user_id " +
                               "AND ActionType = 'play_started' " +
                               "GROUP BY date " +
                               "ORDER BY date ASC";

            List<ReportDayUsage> items = new List<ReportDayUsage>();
            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement(sql_query))
                    {
                        statement.TryBind("@start_date", start_date);
                        statement.TryBind("@user_id", user_id);
                        foreach (var row in statement.ExecuteQuery())
                        {
                            ReportDayUsage test = new ReportDayUsage();
                            test.Date = row[0].ToString();
                            test.Count = row[1].ToInt();
                            items.Add(test);
                        }
                    }
                }
            }

            return items;
        }

        public Dictionary<String, Dictionary<string, int>> GetUsageForDays(int numberOfDays)
        {
            string sql_query = "SELECT UserId, strftime('%Y-%m-%d', DateCreated) AS date, COUNT(1) AS count " +
                               "FROM UserUsageActions " +
                               "WHERE DateCreated >= @start_date " +
                               "AND ActionType = 'play_started' " +
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
