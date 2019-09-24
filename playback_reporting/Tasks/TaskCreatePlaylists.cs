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

            string sql = "";
            sql += "SELECT ItemId, ";
            sql += "COUNT(DISTINCT(UserId)) as count, ";
            sql += "AVG(CAST(strftime('%Y%m%d%H%M', 'now', 'localtime') AS int) - CAST(strftime('%Y%m%d%H%M', DateCreated) AS int)) as av_age ";
            sql += "FROM PlaybackActivity ";
            sql += "WHERE ItemType = 'Movie' ";
            sql += "AND DateCreated > datetime('now', '-14 day', 'localtime') ";
            sql += "GROUP BY ItemId ";
            sql += "ORDER BY count DESC, av_age ASC";
            sql += "LIMIT 20";

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
            string playlist_name = "Most Active Movies (last 14 days)";
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
            create_options.MediaType = "Movie";
            create_options.ItemIdList = items.ToArray();
            await _playlistman.CreatePlaylist(create_options).ConfigureAwait(false);

        }
    }
}
