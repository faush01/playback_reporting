﻿/*
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

using System;
using System.Collections.Generic;
using playback_reporting.Api;
using MediaBrowser.Model.Querying;

namespace playback_reporting.Data
{
    public interface IActivityRepository
    {
        int RemoveUnknownUsers(List<string> known_user_ids);
        void ManageUserList(string action, string id);
        List<string> GetUserList();
        List<string> GetTypeFilterList();
        int ImportRawData(string data);
        string ExportRawData();
        void DeleteOldData(DateTime? del_before);
        void AddPlaybackAction(PlaybackInfo play_info);
        void UpdatePlaybackAction(PlaybackInfo play_info);
        List<Dictionary<string, string>> GetUsageForUser(string date, string user_id, string[] filter, ReportPlaybackOptions config);
        Dictionary<String, Dictionary<string, int>> GetUsageForDays(int days, DateTime end_date, string[] types, string data_type, ReportPlaybackOptions config);
        SortedDictionary<string, int> GetHourlyUsageReport(string user_id, int days, DateTime end_date, string[] types, ReportPlaybackOptions config);
        List<Dictionary<string, object>> GetBreakdownReport(string user_id, int days, DateTime end_date, string type, ReportPlaybackOptions config);
        List<Dictionary<string, object>> GetTvShowReport(string user_id, int days, DateTime end_date, ReportPlaybackOptions config);
        List<Dictionary<string, object>> GetMoviesReport(string user_id, int days, DateTime end_date, ReportPlaybackOptions config);
        List<Dictionary<string, object>> GetUserReport(int days, DateTime end_date);
        string RunCustomQuery(string query_string, List<string> col_names, List<List<object>> results);
        List<Dictionary<string, object>> GetUserPlayListReport(int days, DateTime end_date, string user_id, string filter_name, bool aggregate_data, string[] types, ReportPlaybackOptions config);
    }
}
