using System;
using System.Collections.Generic;
using playback_reporting.Api;
using MediaBrowser.Model.Querying;

namespace playback_reporting.Data
{
    public interface IActivityRepository
    {
        void AddUserAction(UserAction entry);
        QueryResult<UserAction> GetUserActions(DateTime? minDate, int? startIndex, int? limit);
        List<Dictionary<string, string>> GetUsageForUser(string date, string user_id);
        Dictionary<String, Dictionary<string, int>> GetUsageForDays(int numberOfDays);
    }
}
