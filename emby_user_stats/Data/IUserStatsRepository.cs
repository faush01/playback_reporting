using System;
using System.Collections.Generic;
using emby_user_stats.Api;
using MediaBrowser.Model.Querying;

namespace emby_user_stats.Data
{
    public interface IUserStatsRepository
    {
        void AddUserAction(UserAction entry);
        QueryResult<UserAction> GetUserActions(DateTime? minDate, int? startIndex, int? limit);
        List<Dictionary<string, string>> GetUsageForUser(string date, string user_id);
        Dictionary<String, Dictionary<string, int>> GetUsageForDays(int numberOfDays);
    }
}
