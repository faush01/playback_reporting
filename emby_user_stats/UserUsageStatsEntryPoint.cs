using emby_user_stats.Data;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace emby_user_stats
{
    class UserUsageStatsEntryPoint : IServerEntryPoint
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IServerConfigurationManager _config;
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _jsonSerializer;

        public IUserStatsRepository Repository;

        public UserUsageStatsEntryPoint(ISessionManager sessionManager,
            ILibraryManager libraryManager, 
            IUserManager userManager, 
            IServerConfigurationManager config,
            IServerApplicationHost appHost,
            ILogManager logger,
            IFileSystem fileSystem,
            IJsonSerializer jsonSerializer)
        {
            _logger = logger.GetLogger("UserUsageStatsEntryPoint");
            _sessionManager = sessionManager;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _config = config;
            _appHost = appHost;
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Run()
        {

            var repo = new UserStatsRepository(_logger, _config.ApplicationPaths, _fileSystem);
            repo.Initialize();
            Repository = repo;

            _sessionManager.PlaybackStart += _sessionManager_PlaybackStart;
            _sessionManager.PlaybackStopped += _sessionManager_PlaybackStopped;
        }


        void _sessionManager_PlaybackStopped(object sender, PlaybackStopEventArgs e)

        {
            if (e.MediaInfo == null)
            {
                return;
            }

            if (e.Item != null && e.Item.IsThemeMedia)
            {
                // Don't report theme song or local trailer playback
                return;
            }

            if (e.Users.Count == 0)
            {
                return;
            }



            AddUserAction(new UserAction
            {
                Id = Guid.NewGuid().ToString("N"),
                Date = DateTime.UtcNow,
                UserId = e.Users[0].Id.ToString("N"),
                ItemId = e.Item.Id.ToString("N"),
                ItemType = e.MediaInfo.Type,
                ActionType = "play_stopped"
            });

        }

        void _sessionManager_PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            if (e.MediaInfo == null)
            {
                return;
            }

            if (e.Item != null && e.Item.IsThemeMedia)
            {
                // Don't report theme song or local trailer playback
                return;
            }

            if (e.Users.Count == 0)
            {
                return;
            }
           
            //string item_json = _jsonSerializer.SerializeToString(e.MediaInfo);
            //_logger.Info("PlayAction Item Media Details : " + item_json);

            AddUserAction(new UserAction
            {
                Id = Guid.NewGuid().ToString("N"),
                Date = DateTime.UtcNow,
                UserId = e.Users[0].Id.ToString("N"),
                ItemId = e.Item.Id.ToString("N"),
                ItemType = e.MediaInfo.Type,
                ActionType = "play_started"
            });
        }

        private void AddUserAction(UserAction entry)
        {
            _logger.Info("Adding User Action");
            Repository.AddUserAction(entry);
        }
    }
}
