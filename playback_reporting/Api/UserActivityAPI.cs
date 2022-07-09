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

using playback_reporting.Data;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Globalization;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Users;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Querying;

namespace playback_reporting.Api
{
    // http://localhost:8096/emby/user_usage_stats/session_list
    [Route("/user_usage_stats/session_list", "GET", Summary = "Gets Session Info")]
    [Authenticated]
    public class GetSessionInfo : IReturn<Object>
    {

    }

    // http://localhost:8096/emby/user_usage_stats/user_activity
    [Route("/user_usage_stats/user_activity", "GET", Summary = "Gets a report of the available activity per hour")]
    [Authenticated]
    public class GetUserReport : IReturn<Object>
    {
        [ApiMember(Name = "days", Description = "Number of Days", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int days { get; set; }
        [ApiMember(Name = "end_date", Description = "End date of the report in yyyy-MM-dd format", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string end_date { get; set; }
    }

    // http://localhost:8096/user_usage_stats/user_manage/add/1234-4321-1234
    [Route("/user_usage_stats/user_manage/{Action}/{Id}", "GET", Summary = "Get users")]
    [Authenticated]
    public class GetUserManage : IReturn<Object>
    {
        [ApiMember(Name = "Action", Description = "action to perform", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Action { get; set; }
        [ApiMember(Name = "Id", Description = "user Id to perform the action on", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/user_list
    [Route("/user_usage_stats/user_list", "GET", Summary = "Get users")]
    [Authenticated]
    public class GetUserList : IReturn<Object>
    {
    }

    // http://localhost:8096/emby/user_usage_stats/load_backup
    [Route("/user_usage_stats/type_filter_list", "GET", Summary = "Gets types filter list items")]
    [Authenticated]
    public class TypeFilterList : IReturn<Object>
    {
    }

    // http://localhost:8096/emby/user_usage_stats/import_backup
    [Route("/user_usage_stats/import_backup", "POST", Summary = "Post a backup for importing")]
    [Authenticated]
    public class ImportBackup : IRequiresRequestStream, IReturnVoid
    {
        public Stream RequestStream { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/submit_custom_query
    [Route("/user_usage_stats/submit_custom_query", "POST", Summary = "Submit an SQL query")]
    [Authenticated]
    public class CustomQuery : IReturn<Object>
    {
        public String CustomQueryString { get; set; }
        public bool ReplaceUserId { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/load_backup
    [Route("/user_usage_stats/load_backup", "GET", Summary = "Loads a backup from a file")]
    [Authenticated]
    public class LoadBackup : IReturn<Object>
    {
        [ApiMember(Name = "backupfile", Description = "File name of file to load", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string backupfile { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/save_backup
    [Route("/user_usage_stats/save_backup", "GET", Summary = "Saves a backup of the playback report data to the backup path")]
    [Authenticated]
    public class SaveBackup : IReturn<Object>
    {
    }

    // http://localhost:8096/emby/user_usage_stats/PlayActivity
    [Route("/user_usage_stats/PlayActivity", "GET", Summary = "Gets play activity for number of days")]
    [Authenticated]
    public class GetUsageStats : IReturn<Object>
    {
        [ApiMember(Name = "days", Description = "Number of Days", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int days { get; set; }
        [ApiMember(Name = "end_date", Description = "End date of the report in yyyy-MM-dd format", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string end_date { get; set; }
        [ApiMember(Name = "filter", Description = "Comma separated list of media types to filter (movies,series)", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string filter { get; set; }
        [ApiMember(Name = "data_type", Description = "Data type to return (count,time)", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string data_type { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/4c0ea7608f3a41629a0a43a2f23fbb4c/2018-03-23/GetItems
    [Route("/user_usage_stats/{UserID}/{Date}/GetItems", "GET", Summary = "Gets activity for {USER} for {Date} formatted as yyyy-MM-dd")]
    [Authenticated]
    public class GetUserReportData : IReturn<Object>
    {
        [ApiMember(Name = "UserID", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UserID { get; set; }
        [ApiMember(Name = "Date", Description = "UTC DateTime, Format yyyy-MM-dd", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Date { get; set; }
        [ApiMember(Name = "Filter", Description = "Comma separated list of media types to filter (movies,series)", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Filter { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/UserPlaylist
    [Route("/user_usage_stats/UserPlaylist", "GET", Summary = "Gets a report of all played items for a user in a date period")]
    [Authenticated]
    public class GetUserPlaylist : IReturn<Object>
    {
        [ApiMember(Name = "user_id", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string user_id { get; set; }
        [ApiMember(Name = "aggregate_data", Description = "Aggregate the data to total duration per user per item", IsRequired = true, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool aggregate_data { get; set; }
        [ApiMember(Name = "filter_name", Description = "Name Filter", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string filter_name { get; set; }
        [ApiMember(Name = "days", Description = "Number of Days", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int days { get; set; }
        [ApiMember(Name = "end_date", Description = "End date of the report in yyyy-MM-dd format", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string end_date { get; set; }
        [ApiMember(Name = "filter", Description = "Comma separated list of media types to filter (movies,series)", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string filter { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/HourlyReport
    [Route("/user_usage_stats/HourlyReport", "GET", Summary = "Gets a report of the available activity per hour")]
    [Authenticated]
    public class GetHourlyReport : IReturn<Object>
    {
        [ApiMember(Name = "user_id", Description = "User Id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string user_id { get; set; }
        [ApiMember(Name = "days", Description = "Number of Days", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int days { get; set; }
        [ApiMember(Name = "end_date", Description = "End date of the report in yyyy-MM-dd format", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string end_date { get; set; }
        [ApiMember(Name = "filter", Description = "Comma separated list of media types to filter (movies,series)", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string filter { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/ItemType/BreakdownReport
    [Route("/user_usage_stats/{BreakdownType}/BreakdownReport", "GET", Summary = "Gets a breakdown of a usage metric")]
    [Authenticated]
    public class GetBreakdownReport : IReturn<Object>
    {
        [ApiMember(Name = "user_id", Description = "User Id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string user_id { get; set; }
        [ApiMember(Name = "days", Description = "Number of Days", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int days { get; set; }
        [ApiMember(Name = "end_date", Description = "End date of the report in yyyy-MM-dd format", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string end_date { get; set; }
        [ApiMember(Name = "BreakdownType", Description = "Breakdown type", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string BreakdownType { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/TvShowsReport
    [Route("/user_usage_stats/TvShowsReport", "GET", Summary = "Gets TV Shows counts")]
    [Authenticated]
    public class GetTvShowsReport : IReturn<Object>
    {
        [ApiMember(Name = "user_id", Description = "User Id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string user_id { get; set; }
        [ApiMember(Name = "days", Description = "Number of Days", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int days { get; set; }
        [ApiMember(Name = "end_date", Description = "End date of the report in yyyy-MM-dd format", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string end_date { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/MoviesReport
    [Route("/user_usage_stats/MoviesReport", "GET", Summary = "Gets Movies counts")]
    [Authenticated]
    public class GetMoviesReport : IReturn<Object>
    {
        [ApiMember(Name = "user_id", Description = "User Id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string user_id { get; set; }
        [ApiMember(Name = "days", Description = "Number of Days", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int days { get; set; }
        [ApiMember(Name = "end_date", Description = "End date of the report in yyyy-MM-dd format", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string end_date { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/get_items
    [Route("/user_usage_stats/get_items", "GET", Summary = "Get a list of items for type and filtered")]
    [Authenticated]
    public class GetItems : IReturn<Object>
    {
        [ApiMember(Name = "filter", Description = "filter string", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string filter { get; set; }
        [ApiMember(Name = "item_type", Description = "type of items to return", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string item_type { get; set; }
        [ApiMember(Name = "parent", Description = "parentid", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int parent { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/get_item_stats
    [Route("/user_usage_stats/get_item_stats", "GET", Summary = "Get a list of items for type and filtered")]
    [Authenticated]
    public class GetItemStats : IReturn<Object>
    {
        [ApiMember(Name = "id", Description = "item id", IsRequired = true, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int id { get; set; }
    }

    // http://localhost:8096/emby/user_usage_stats/get_item_path
    [Route("/user_usage_stats/get_item_path", "GET", Summary = "Get a list of items for type and filtered")]
    [Authenticated]
    public class GetItemPath : IReturn<Object>
    {
        [ApiMember(Name = "id", Description = "item id", IsRequired = true, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int id { get; set; }
    }

    public class UserActivityAPI : IService, IRequiresRequest
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _config;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IAuthorizationContext _ac;

        private ActivityRepository repository;

        public UserActivityAPI(ILogManager logger,
            IFileSystem fileSystem,
            IServerConfigurationManager config,
            IJsonSerializer jsonSerializer,
            IUserManager userManager,
            ILibraryManager libraryManager,
            ISessionManager sessionManager,
            IAuthorizationContext authContext,
            IUserDataManager userDataManager)
        {
            _logger = logger.GetLogger("PlaybackReporting - UserActivityAPI");
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            _config = config;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _sessionManager = sessionManager;
            _ac = authContext;
            _userDataManager = userDataManager;

            _logger.Info("UserActivityAPI Loaded");
            ActivityRepository repo = new ActivityRepository(_logger, _config.ApplicationPaths, _fileSystem);
            repository = repo;
        }

        public IRequest Request { get; set; }

        public object Get(GetItemPath request)
        {
            _logger.Info("GetItemPath");

            List <PathItem> item_path = new List<PathItem>();

            Guid item_guid = _libraryManager.GetGuid(request.id);
            BaseItem item = _libraryManager.GetItemById(item_guid);

            Folder[] collections = _libraryManager.GetCollectionFolders(item);
            foreach(var collection in collections)
            {
                _logger.Info("GetCollectionFolders: " + collection.Name + "(" + item.InternalId + "," + item.IsTopParent + ")");
            }
            //BaseItem base_item = item.GetTopParent();

            bool hadTopParent = false;
            PathItem pi = new PathItem();
            while (item != null && !item.IsTopParent)
            {
                _logger.Info("AddingPathItem: " + item.Name + "(" + item.InternalId + "," + item.IsTopParent + "," + item.IsResolvedToFolder + ")");

                pi = new PathItem();
                pi.Name = item.Name;
                pi.Id = item.InternalId;
                pi.ItemType = item.GetType().Name;
                item_path.Insert(0, pi);
                
                item = item.GetParent();

                if (item != null && item.IsTopParent)
                {
                    hadTopParent = true;
                    _logger.Info("TopParentItem: " + item.Name + "(" + item.InternalId + "," + item.IsTopParent + "," + item.IsResolvedToFolder + ")");
                }
            }
            
            if (hadTopParent && collections.Length > 0)
            {
                item = collections[0];

                _logger.Info("AddingCollectionItem:" + item.Name + "(" + item.InternalId + "," + item.IsTopParent + "," + item.IsResolvedToFolder + ")");

                pi = new PathItem();
                pi.Name = item.Name;
                pi.Id = item.InternalId;
                pi.ItemType = item.GetType().Name;
                item_path.Insert(0, pi);

                item = item.GetParent();

                pi = new PathItem();
                pi.Name = item.Name;
                pi.Id = item.InternalId;
                pi.ItemType = item.GetType().Name;
                item_path.Insert(0, pi);
            }
            

            return item_path;
        }

        private ItemChildStats GetChildStats(BaseItem item)
        {
            InternalItemsQuery query = new InternalItemsQuery();
            query.ParentIds = new long[] { item.InternalId };
            query.IncludeItemTypes = new string[] { "Episode" };
            query.Recursive = true;

            UserQuery user_query = new UserQuery();
            User[] users = _userManager.GetUserList(user_query);

            ItemChildStats stats = new ItemChildStats();
            BaseItem[] results = _libraryManager.GetItemList(query);
            stats.Total = results.Length;

            foreach(User user in users)
            {
                foreach (BaseItem child in results)
                {
                    UserItemData uid = _userDataManager.GetUserData(user, child);
                    if (uid.Played)
                    {
                        if (stats.Stats.ContainsKey(user))
                        {
                            stats.Stats[user]++;
                        }
                        else
                        {
                            stats.Stats.Add(user, 1);
                        }
                    }
                    else
                    {
                        if (!stats.Stats.ContainsKey(user))
                        {
                            stats.Stats.Add(user, 0);
                        }
                    }

                }
            }

            return stats;
        }

        public object Get(GetItemStats request)
        {
            List<Dictionary<string, string>> details = new List<Dictionary<string, string>>();

            Guid item_guid = _libraryManager.GetGuid(request.id);
            BaseItem item = _libraryManager.GetItemById(item_guid);

            ItemChildStats child_stats = null;
            if (item.GetType() == typeof(Series) || item.GetType() == typeof(Season))
            {
                child_stats = GetChildStats(item);
            }

            UserQuery query = new UserQuery();
            Tuple<string, SortOrder>[] order = new Tuple<string, SortOrder>[1];
            order[0] = new Tuple<string, SortOrder>("name", SortOrder.Ascending);
            query.OrderBy = order;
            User[] users = _userManager.GetUserList(query);
            foreach(User user in users)
            {
                UserItemData uid = _userDataManager.GetUserData(user, item);

                Dictionary<string, string> user_info = new Dictionary<string, string>();
                user_info.Add("name", user.Name);
                user_info.Add("played", uid.Played.ToString());
                user_info.Add("play_count", uid.PlayCount.ToString());
                if (uid.LastPlayedDate != null)
                {
                    user_info.Add("last_played", uid.LastPlayedDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                }
                else
                {
                    user_info.Add("last_played", "");
                }

                if(child_stats != null && child_stats.Stats.ContainsKey(user))
                {
                    user_info.Add("child_stats", child_stats.Stats[user] + "/" + child_stats.Total);
                }
                else
                {
                    user_info.Add("child_stats", "");
                }

                details.Add(user_info);
            }

            return details;
        }

        public object Get(GetItems request)
        {
            List<ItemInfo> items = new List<ItemInfo>();

            /*
            types
                AggregateFolder
                UserRootFolder
                Folder
                CollectionFolder
                Movie
                Series
                Season
                Episode
                Audio
                MusicAlbum
                MusicArtist
                MusicGenre
                Playlist
                Video
                Genre
                Person
                Studio
                UserView
            */

            InternalItemsQuery query = new InternalItemsQuery();

            //(string, SortOrder)[] ord = new (string, SortOrder)[1];
            //ord[0] = ("name", SortOrder.Ascending);
            //query.OrderBy = ord;

            if (request.parent != 0)
            {
                query.ParentIds = new long[] { request.parent };
            }
            else if(!string.IsNullOrEmpty(request.filter))
            {
                query.IncludeItemTypes = new string[] { "MusicAlbum", "Movie", "Series" };
                query.SearchTerm = request.filter;
            }
            else
            {
                query.IncludeItemTypes = new string[] { "CollectionFolder" };
                //query.Parent = _libraryManager.RootFolder;
            }

            BaseItem[] results = _libraryManager.GetItemList(query);

            foreach (BaseItem item in results)
            {
                //_logger.Info(item.Name + "(" + item.InternalId + ")");
                ItemInfo info = new ItemInfo();
                info.Id = item.InternalId;
                // + "(" + item.GetType() + ")" + "(" + item.MediaType + ")" + "(" + item.LocationType + ")" + " (" + item.ExtraType + ")");
                info.Name = item.Name;
                info.ItemType = item.GetType().Name;

                if (item.GetType() == typeof(Episode))
                {
                    Episode e = (Episode)item;
                    info.Series = e.SeriesName;
                    info.Season = e.Season.Name;

                    string epp_name = "";
                    if(e.IndexNumber != null)
                    {
                        epp_name += e.IndexNumber.Value.ToString("D2");
                    }
                    else
                    {
                        epp_name += "00";
                    }
                    epp_name += " - " + e.Name;

                    info.Name = epp_name;
                }
                else if(item.GetType() == typeof(Season))
                {
                    string season_name = "";
                    if (item.IndexNumber != null)
                    {
                        season_name += item.IndexNumber.Value.ToString("D2");
                    }
                    else
                    {
                        season_name += "00";
                    }
                    season_name += " - " + item.Name;
                    info.Name = season_name;
                }

                items.Add(info);
            }

            items.Sort(delegate (ItemInfo c1, ItemInfo c2) { return string.Compare(c1.Name, c2.Name, comparisonType: StringComparison.OrdinalIgnoreCase); });
            //c1.Name.CompareTo(c2.Name)
            return items;
        }

        public object Get(TypeFilterList request)
        {
            AuthorizationInfo auth_user_info = _ac.GetAuthorizationInfo(Request);
            UserPolicy policy = _userManager.GetUserPolicy(auth_user_info.User);
            if (!policy.IsAdministrator)
            {
                return new List<string>();
            }

            List<string> filter_list = repository.GetTypeFilterList();
            return filter_list;
        }

        public object Get(GetUserReport request)
        {
            AuthorizationInfo auth_user_info = _ac.GetAuthorizationInfo(Request);
            UserPolicy policy = _userManager.GetUserPolicy(auth_user_info.User);
            if (!policy.IsAdministrator)
            {
                return new List<Dictionary<string, object>>();
            }

            DateTime end_date;
            if (string.IsNullOrEmpty(request.end_date))
            {
                end_date = DateTime.Now;
            }
            else
            {
                _logger.Info("End_Date: " + request.end_date);
                end_date = DateTime.ParseExact(request.end_date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            List<Dictionary<string, object>> report = repository.GetUserReport(request.days, end_date);

            foreach(var user_info in report)
            {
                string user_id = (string)user_info["user_id"];
                string user_name = "Not Known";
                bool has_image = false;
                MediaBrowser.Controller.Entities.User user = null;
                if (!string.IsNullOrEmpty(user_id))
                {
                    Guid user_guid = new Guid(user_id);
                    user = _userManager.GetUserById(user_guid);
                }

                if (user != null)
                {
                    user_name = user.Name;
                    has_image = user.HasImage(MediaBrowser.Model.Entities.ImageType.Primary);
                }
                user_info.Add("user_name", user_name);
                user_info.Add("has_image", has_image);

                DateTime last_seen = (DateTime)user_info["latest_date"];
                TimeSpan time_ago = DateTime.Now.Subtract(last_seen);

                string last_seen_string = GetLastSeenString(time_ago);
                if (last_seen_string == "")
                {
                    last_seen_string = "just now";
                }
                user_info.Add("last_seen", last_seen_string);

                int seconds = (int)user_info["total_time"];
                TimeSpan total_time = new TimeSpan(10000000L * (long)seconds);

                string time_played = GetLastSeenString(total_time);
                if (time_played == "")
                {
                    time_played = "< 1 minute";
                }
                user_info.Add("total_play_time", time_played);

            }

            return report;
        }

        private string GetLastSeenString(TimeSpan span)
        {
            String last_seen = "";

            if (span.TotalDays > 365)
            {
                last_seen += GetTimePart((int)(span.TotalDays / 365), "year");
            }

            if ((int)(span.TotalDays % 365) > 7)
            {
                last_seen += GetTimePart((int)((span.TotalDays % 365) / 7), "week");
            }

            if ((int)(span.TotalDays % 7) > 0)
            {
                last_seen += GetTimePart((int)(span.TotalDays % 7), "day");
            }

            if (span.Hours > 0)
            {
                last_seen += GetTimePart(span.Hours, "hour");
            }

            if (span.Minutes > 0)
            {
                last_seen += GetTimePart(span.Minutes, "minute");
            }

            return last_seen;
        }

        private string GetTimePart(int value, string name)
        {
            string part = value + " " + name;
            if (value > 1)
            {
                part += "s";
            }
            return part + " ";
        }

        public object Get(GetUserManage request)
        {
            AuthorizationInfo auth_user_info = _ac.GetAuthorizationInfo(Request);
            UserPolicy policy = _userManager.GetUserPolicy(auth_user_info.User);
            if (!policy.IsAdministrator)
            {
                return -1;
            }

            string action = request.Action;
            string id = request.Id;

            if (action == "remove_unknown")
            {
                List<string> user_id_list = new List<string>();
                foreach (var emby_user in _userManager.Users)
                {
                    user_id_list.Add(emby_user.Id.ToString("N"));
                }
                int removed_count = repository.RemoveUnknownUsers(user_id_list);
                return removed_count;
            }
            else
            {
                repository.ManageUserList(action, id);
                return 1;
            }
        }

        public object Get(GetUserList request)
        {
            AuthorizationInfo auth_user_info = _ac.GetAuthorizationInfo(Request);
            UserPolicy policy = _userManager.GetUserPolicy(auth_user_info.User);
            if (!policy.IsAdministrator)
            {
                return new List<Dictionary<string, object>>();
            }

            List<string> user_id_list = repository.GetUserList();

            List<Dictionary<string, object>> users = new List<Dictionary<string, object>>();

            foreach (var emby_user in _userManager.Users)
            {
                Dictionary<string, object> user_info = new Dictionary<string, object>();
                user_info.Add("name", emby_user.Name);
                user_info.Add("id", emby_user.Id.ToString("N"));
                user_info.Add("in_list", user_id_list.Contains(emby_user.Id.ToString("N")));
                users.Add(user_info);
            }

            return users;
        }

        public object Get(GetUserReportData report)
        {
            AuthorizationInfo user_info = _ac.GetAuthorizationInfo(Request);
            UserPolicy policy = _userManager.GetUserPolicy(user_info.User);
            if (!policy.IsAdministrator)
            {
                return new List<Dictionary<string, object>>();
            }

            string[] filter_tokens = new string[0];
            if (report.Filter != null)
            {
                filter_tokens = report.Filter.Split(',');
            }
            List<Dictionary<string, string>> results = repository.GetUsageForUser(report.Date, report.UserID, filter_tokens);

            List<Dictionary<string, object>> user_activity = new List<Dictionary<string, object>>();

            foreach (Dictionary<string, string> item_data in results)
            {
                Dictionary<string, object> item_info = new Dictionary<string, object>();

                item_info["Time"] = item_data["Time"];
                item_info["Id"] = item_data["Id"];
                item_info["Name"] = item_data["ItemName"];
                item_info["Type"] = item_data["Type"];
                item_info["Client"] = item_data["ClientName"];
                item_info["Method"] = item_data["PlaybackMethod"];
                item_info["Device"] = item_data["DeviceName"];
                item_info["Duration"] = item_data["PlayDuration"];
                item_info["RowId"] = item_data["RowId"];

                user_activity.Add(item_info);
            }

            return user_activity;
        }

        public void Post(ImportBackup request)
        {
            AuthorizationInfo user_info = _ac.GetAuthorizationInfo(Request);
            UserPolicy policy = _userManager.GetUserPolicy(user_info.User);
            if (!policy.IsAdministrator)
            {
                return;
            }

            string headers = "";
            foreach (var head in Request.Headers.Keys)
            {
                headers += head + " : " + Request.Headers[head] + "\r\n";
            }
            _logger.Info("Header : " + headers);

            _logger.Info("Files Length : " + Request.Files.Length);

            _logger.Info("ContentType : " + Request.ContentType);

            Stream input_data = request.RequestStream;
            _logger.Info("Stream Info : " + input_data.CanRead);

            byte[] bytes = new byte[10000];
            int read = input_data.Read(bytes, 0, 10000);
            _logger.Info("Bytes Read : " + read);
            _logger.Info("Read : " + bytes);
        }

        public object Get(LoadBackup load_backup)
        {
            AuthorizationInfo user_info = _ac.GetAuthorizationInfo(Request);
            UserPolicy policy = _userManager.GetUserPolicy(user_info.User);
            if (!policy.IsAdministrator)
            {
                return new List<string>() { "Not Authorized" };
            }

            FileInfo fi = new FileInfo(load_backup.backupfile);
            if (fi.Exists == false)
            {
                return new List<string>() { "Backup file does not exist" };
            }

            int count = 0;
            try
            {
                string load_data = "";
                using (StreamReader sr = new StreamReader(new FileStream(fi.FullName, FileMode.Open)))
                {
                    load_data = sr.ReadToEnd();
                }
                count = repository.ImportRawData(load_data);
            }
            catch (Exception e)
            {
                return new List<string>() { e.Message };
            }

            return new List<string>() { "Backup loaded " + count + " items" };
        }
        public object Get(SaveBackup save_backup)
        {
            AuthorizationInfo user_info = _ac.GetAuthorizationInfo(Request);
            UserPolicy policy = _userManager.GetUserPolicy(user_info.User);
            if (!policy.IsAdministrator)
            {
                return new List<string>() { "Not Authorized" };
            }

            BackupManager bum = new BackupManager(_config, _logger, _fileSystem);
            string message = bum.SaveBackup();

            return new List<string>() { message };
        }

        public object Get(GetUsageStats activity)
        {
            AuthorizationInfo user_info = _ac.GetAuthorizationInfo(Request);
            UserPolicy policy = _userManager.GetUserPolicy(user_info.User);
            if (!policy.IsAdministrator)
            {
                return new List<Dictionary<string, object>>();
            }

            string[] filter_tokens = new string[0];
            if (activity.filter != null)
            {
                filter_tokens = activity.filter.Split(',');
            }

            DateTime end_date;
            if (string.IsNullOrEmpty(activity.end_date))
            {
                end_date = DateTime.Now;
            }
            else
            {
                _logger.Info("End_Date: " + activity.end_date);
                end_date = DateTime.ParseExact(activity.end_date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            
            Dictionary<String, Dictionary<string, int>> results = repository.GetUsageForDays(activity.days, end_date, filter_tokens, activity.data_type);

            // add empty user for labels
            results.Add("labels_user", new Dictionary<string, int>());

            List<Dictionary<string, object>> user_usage_data = new List<Dictionary<string, object>>();
            foreach (string user_id in results.Keys)
            {
                Dictionary<string, int> user_usage = results[user_id];

                // fill in missing dates for time period
                SortedDictionary<string, int> userUsageByDate = new SortedDictionary<string, int>();
                DateTime from_date = end_date.AddDays((activity.days * -1) + 1);
                while (from_date <= end_date)
                {
                    string date_string = from_date.ToString("yyyy-MM-dd");
                    if (user_usage.ContainsKey(date_string) == false)
                    {
                        userUsageByDate.Add(date_string, 0);
                    }
                    else
                    {
                        userUsageByDate.Add(date_string, user_usage[date_string]);
                    }

                    from_date = from_date.AddDays(1);
                }

                string user_name = "Not Known";
                if (user_id == "labels_user")
                {
                    user_name = "labels_user";
                }
                else
                {
                    User user = null;
                    try
                    {
                        Guid user_guid = new Guid(user_id);
                        user = _userManager.GetUserById(user_guid);
                    }
                    catch(Exception e)
                    {
                        _logger.ErrorException("Error parsing user GUID : (" + user_id + ")", e);
                    }

                    if (user != null)
                    {
                        user_name = user.Name;
                    }
                    else
                    {
                        // if we could not get the user just use the user ID
                        user_name = user_id;
                    }
                }

                Dictionary<string, object> user_data = new Dictionary<string, object>();
                user_data.Add("user_id", user_id);
                user_data.Add("user_name", user_name);
                user_data.Add("user_usage", userUsageByDate);

                user_usage_data.Add(user_data);
            }

            var sorted_data = user_usage_data.OrderBy(dict => (dict["user_name"] as string).ToLower());

            return sorted_data;
        }

        public object Get(GetHourlyReport request)
        {
            AuthorizationInfo user_info = _ac.GetAuthorizationInfo(Request);
            UserPolicy policy = _userManager.GetUserPolicy(user_info.User);
            if (!policy.IsAdministrator)
            {
                return new SortedDictionary<string, int>();
            }

            string[] filter_tokens = new string[0];
            if (request.filter != null)
            {
                filter_tokens = request.filter.Split(',');
            }

            DateTime end_date;
            if (string.IsNullOrEmpty(request.end_date))
            {
                end_date = DateTime.Now;
            }
            else
            {
                _logger.Info("End_Date: " + request.end_date);
                end_date = DateTime.ParseExact(request.end_date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            SortedDictionary<string, int> report = repository.GetHourlyUsageReport(request.user_id, request.days, end_date, filter_tokens);

            for (int day = 0; day < 7; day++)
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    string key = day + "-" + hour.ToString("D2");
                    if(report.ContainsKey(key) == false)
                    {
                        report.Add(key, 0);
                    }
                }
            }

            return report;
        }

        public object Get(GetBreakdownReport request)
        {
            AuthorizationInfo user_info = _ac.GetAuthorizationInfo(Request);
            UserPolicy policy = _userManager.GetUserPolicy(user_info.User);
            if (!policy.IsAdministrator)
            {
                return new List<Dictionary<string, object>>();
            }

            DateTime end_date;
            if (string.IsNullOrEmpty(request.end_date))
            {
                end_date = DateTime.Now;
            }
            else
            {
                _logger.Info("End_Date: " + request.end_date);
                end_date = DateTime.ParseExact(request.end_date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            List<Dictionary<string, object>> report = repository.GetBreakdownReport(request.user_id, request.days, end_date, request.BreakdownType);

            if (request.BreakdownType == "UserId")
            {
                foreach (var row in report)
                {
                    string user_id = row["label"] as string;
                    MediaBrowser.Controller.Entities.User user = null;
                    if (!string.IsNullOrEmpty(user_id))
                    {
                        Guid user_guid = new Guid(user_id);
                        user = _userManager.GetUserById(user_guid);
                    }

                    if (user != null)
                    {
                        row["label"] = user.Name;
                    }
                    else
                    {
                        row["label"] = "unknown";
                    }
                }
            }

            return report;
        }

        public object Get(GetTvShowsReport request)
        {
            AuthorizationInfo user_info = _ac.GetAuthorizationInfo(Request);
            UserPolicy policy = _userManager.GetUserPolicy(user_info.User);
            if (!policy.IsAdministrator)
            {
                return new List<Dictionary<string, object>>();
            }

            DateTime end_date;
            if (string.IsNullOrEmpty(request.end_date))
            {
                end_date = DateTime.Now;
            }
            else
            {
                _logger.Info("End_Date: " + request.end_date);
                end_date = DateTime.ParseExact(request.end_date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            List<Dictionary<string, object>> report = repository.GetTvShowReport(request.user_id, request.days, end_date);
            return report;
        }

        public object Get(GetMoviesReport request)
        {
            AuthorizationInfo user_info = _ac.GetAuthorizationInfo(Request);
            UserPolicy policy = _userManager.GetUserPolicy(user_info.User);
            if (!policy.IsAdministrator)
            {
                return new List <Dictionary<string, object>>();
            }

            DateTime end_date;
            if (string.IsNullOrEmpty(request.end_date))
            {
                end_date = DateTime.Now;
            }
            else
            {
                _logger.Info("End_Date: " + request.end_date);
                end_date = DateTime.ParseExact(request.end_date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            List<Dictionary<string, object>> report = repository.GetMoviesReport(request.user_id, request.days, end_date);
            return report;
        }

        public object Post(CustomQuery request)
        {
            _logger.Info("CustomQuery : " + request.CustomQueryString);

            Dictionary<string, object> responce = new Dictionary<string, object>();

            AuthorizationInfo user_info = _ac.GetAuthorizationInfo(Request);
            UserPolicy policy = _userManager.GetUserPolicy(user_info.User);
            if (!policy.IsAdministrator)
            {
                return responce;
            }

            List<List<object>> result = new List<List<object>>();
            List<string> colums = new List<string>();
            string message = repository.RunCustomQuery(request.CustomQueryString, colums, result);

            int index_of_user_col = colums.IndexOf("UserId");
            if (request.ReplaceUserId && index_of_user_col > -1)
            {
                colums[index_of_user_col] = "UserName";

                Dictionary<string, string> user_map = new Dictionary<string, string>();
                foreach (var user in _userManager.Users)
                {
                    user_map.Add(user.Id.ToString("N"), user.Name);
                }

                foreach(var row in result)
                {
                    String user_id = (string)row[index_of_user_col];
                    if(user_map.ContainsKey(user_id))
                    {
                        row[index_of_user_col] = user_map[user_id];
                    }
                }
            }


            /*
            List<object> row = new List<object>();
            row.Add("Shaun");
            row.Add(12);
            row.Add("Some Date");
            result.Add(row);

            colums.Add("Name");
            colums.Add("Age");
            colums.Add("Started");
            */

            responce.Add("colums", colums);
            responce.Add("results", result);
            responce.Add("message", message);
            
            return responce;
        }

        public object Get(GetUserPlaylist request)
        {
            

            AuthorizationInfo user_info = _ac.GetAuthorizationInfo(Request);
            UserPolicy policy = _userManager.GetUserPolicy(user_info.User);
            if (!policy.IsAdministrator)
            {
                return new List<Dictionary<string, object>>();
            }

            DateTime end_date;
            if (string.IsNullOrEmpty(request.end_date))
            {
                end_date = DateTime.Now;
            }
            else
            {
                _logger.Info("End_Date: " + request.end_date);
                end_date = DateTime.ParseExact(request.end_date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            List<Dictionary<string, object>> report = repository.GetUserPlayListReport(request.days, end_date, request.user_id, request.filter_name, request.aggregate_data, null);

            foreach (var row in report)
            {
                string user_id = row["user"] as string;
                MediaBrowser.Controller.Entities.User user = null;
                if (!string.IsNullOrEmpty(user_id))
                {
                    Guid user_guid = new Guid(user_id);
                    user = _userManager.GetUserById(user_guid);
                }

                if (user != null)
                {
                    row["user"] = user.Name;
                }
                else
                {
                    row["user"] = "unknown";
                }
            }

            /*
            Dictionary<string, object> row_data = new Dictionary<string, object>();
            row_data.Add("date", "2018-01-10");
            row_data.Add("name", "The Last Moon Man");
            row_data.Add("type", "Movie");
            row_data.Add("duration", 2567);
            report.Add(row_data);

            row_data = new Dictionary<string, object>();
            row_data.Add("date", "2018-01-10");
            row_data.Add("name", "Hight Up There");
            row_data.Add("type", "Movie");
            row_data.Add("duration", 3654);
            report.Add(row_data);
            */

            return report;
        }

        public object Get(GetSessionInfo request)
        {
            List<Dictionary<string, object>> report = new List<Dictionary<string, object>>();

            AuthorizationInfo user_info = _ac.GetAuthorizationInfo(Request);
            UserPolicy policy = _userManager.GetUserPolicy(user_info.User);
            if (!policy.IsAdministrator)
            {
                return report;
            }

            foreach (SessionInfo session in _sessionManager.Sessions)
            {
                Dictionary<string, object> data = new Dictionary<string, object>();

                data.Add("TranscodingInfo", session.TranscodingInfo);
                data.Add("PlayState", session.PlayState);
                data.Add("NowPlayingItem", session.NowPlayingItem);
                data.Add("device_name", session.DeviceName);
                data.Add("client_name", session.Client);
                data.Add("app_icon", session.AppIconUrl);
                data.Add("app_version", session.ApplicationVersion);

                data.Add("has_user", session.HasUser);
                data.Add("user_id", session.UserId);
                data.Add("user_name", session.UserName);
                data.Add("has_image", session.UserPrimaryImageTag);

                data.Add("remote_address", session.RemoteEndPoint);

                TimeSpan ts = DateTime.UtcNow.Subtract(session.LastActivityDate.DateTime);
                data.Add("last_active", ts.ToString(@"dd\.hh\:mm\:ss"));

                report.Add(data);
            }

            return report;
        }

    }
}
