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

using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Playlists;
using MediaBrowser.Model.Tasks;
using playback_reporting.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace playback_reporting.Tasks
{
    class TaskCreatePlaylists : IScheduledTask
    {
        private string task_name = "Create Playlists";

        public string Name => task_name;
        public string Key => "PlaybackReportingCreatePlaylists";
        public string Description => "Creates playlists for most popular items based on user activity";
        public string Category => "Playback Reporting";

        private readonly ILogger _logger;
        private readonly IPlaylistManager _playlistman;
        private readonly IServerApplicationHost _appHost;
        private readonly ILibraryManager _libraryManager;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;

        public TaskCreatePlaylists(ILogManager logger, 
            IPlaylistManager playlistman, 
            IServerApplicationHost appHost, 
            ILibraryManager libraryManager,
            IServerConfigurationManager config,
            IFileSystem fileSystem)
        {
            _logger = logger.GetLogger("PlaybackReporting - CreatePlaylists");
            _playlistman = playlistman;
            _appHost = appHost;
            _libraryManager = libraryManager;
            _config = config;
            _fileSystem = fileSystem;

            if (VersionCheck.IsVersionValid(_appHost.ApplicationVersion, _appHost.SystemUpdateLevel) == false)
            {
                _logger.Info("ERROR : Plugin not compatible with this server version");
                throw new NotImplementedException("This task is not available on this version of Emby");
            }
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            var trigger = new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerDaily,
                TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
            }; //3am daily
            return new[] { trigger };
        }

        public async System.Threading.Tasks.Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (VersionCheck.IsVersionValid(_appHost.ApplicationVersion, _appHost.SystemUpdateLevel) == false)
            {
                _logger.Info("ERROR : Plugin not compatible with this server version");
                return;
            }

            // query the user playback info for the most active movies
            ActivityRepository repository = new ActivityRepository(_logger, _config.ApplicationPaths, _fileSystem);
            ReportPlaybackOptions config = _config.GetReportPlaybackOptions();

            foreach(var activity_playlist in config.ActivityPlaylists)
            {
                string list_name = activity_playlist.Name;
                string list_type = activity_playlist.Type;
                int list_days = activity_playlist.Days;
                int list_size = activity_playlist.Size;

                _logger.Info("Activity Playlist - Name:" + list_name + " Type:" + list_type + " Days:" + list_days);

                string sql = "";
                sql += "SELECT ItemId, ";
                sql += "COUNT(DISTINCT(UserId)) as count, ";
                sql += "AVG(CAST(strftime('%Y%m%d%H%M', 'now', 'localtime') AS int) - CAST(strftime('%Y%m%d%H%M', DateCreated) AS int)) as av_age ";
                sql += "FROM PlaybackActivity ";
                sql += "WHERE ItemType = '" + list_type + "' ";
                sql += "AND DateCreated > datetime('now', '-" + list_days + " day', 'localtime') ";
                sql += "GROUP BY ItemId ";
                sql += "ORDER BY count DESC, av_age ASC ";
                sql += "LIMIT " + list_size;

                List<string> cols = new List<string>();
                List<List<Object>> query_results = new List<List<object>>();
                repository.RunCustomQuery(sql, cols, query_results);

                List<long> items = new List<long>();
                foreach (List<Object> row in query_results)
                {
                    long item_id = long.Parse((string)row[0]);
                    items.Add(item_id);
                }

                // create a playlist with the most active movies
                string playlist_name = list_name;
                InternalItemsQuery query = new InternalItemsQuery();
                query.IncludeItemTypes = new string[] { "Playlist" };
                query.Name = playlist_name;

                BaseItem[] results = _libraryManager.GetItemList(query, false);
                foreach (BaseItem item in results)
                {
                    _logger.Info("Deleting Existing Movie Playlist : " + item.InternalId);
                    DeleteOptions delete_options = new DeleteOptions();
                    delete_options.DeleteFileLocation = true;
                    _libraryManager.DeleteItem(item, delete_options);
                }

                _logger.Info("Creating Movie Playlist");
                PlaylistCreationRequest create_options = new PlaylistCreationRequest();
                create_options.Name = playlist_name;

                if (list_type == "Movie")
                {
                    create_options.MediaType = "Movie";
                }
                else
                {
                    create_options.MediaType = "Episode";
                }

                create_options.ItemIdList = items.ToArray();
                await _playlistman.CreatePlaylist(create_options).ConfigureAwait(false);
            }
        }
    }
}
