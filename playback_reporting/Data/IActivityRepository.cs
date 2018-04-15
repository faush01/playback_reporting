using System;
using System.Collections.Generic;
using playback_reporting.Api;
using MediaBrowser.Model.Querying;

namespace playback_reporting.Data
{
    public interface IActivityRepository
    {
        int ImportRawData(string data);
        string ExportRawData();
        void DeleteOldData(DateTime? del_before);
        void AddPlaybackAction(PlaybackInfo play_info);
        void UpdatePlaybackAction(PlaybackInfo play_info);
        List<Dictionary<string, string>> GetUsageForUser(string date, string user_id, string[] filter);
        Dictionary<String, Dictionary<string, int>> GetUsageForDays(int numberOfDays, string[] types, string data_type);
    }
}
