using System;
using MediaBrowser.Model.Querying;

namespace emby_user_stats.Data
{
    public interface IUserStatsRepository
    {
        void AddUserAction(UserAction entry);
        QueryResult<UserAction> GetUserActions(DateTime? minDate, int? startIndex, int? limit);
    }
}
