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
    }
}
