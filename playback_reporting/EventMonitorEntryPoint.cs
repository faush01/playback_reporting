using playback_reporting.Data;
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
using System.Threading;
using System.Threading.Tasks;

namespace playback_reporting
{
    class EventMonitorEntryPoint : IServerEntryPoint
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IServerConfigurationManager _config;
        private readonly IServerApplicationHost _appHost;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _jsonSerializer;
        private IActivityRepository Repository;

        public EventMonitorEntryPoint(ISessionManager sessionManager,
            ILibraryManager libraryManager, 
            IUserManager userManager, 
            IServerConfigurationManager config,
            IServerApplicationHost appHost,
            ILogManager logger,
            IFileSystem fileSystem,
            IJsonSerializer jsonSerializer)
        {
            _logger = logger.GetLogger("PlaybackReporting");
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

            var repo = new ActivityRepository(_logger, _config.ApplicationPaths, _fileSystem);
            repo.Initialize();
            Repository = repo;

            _sessionManager.PlaybackStart += _sessionManager_PlaybackStart;
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

            // start a task to report playback started
            System.Threading.Tasks.Task.Run(() => StartPlaybackTimer(e));

            AddUserAction(new UserAction
            {
                Id = Guid.NewGuid().ToString("N"),
                Date = DateTime.Now,
                UserId = e.Users[0].Id.ToString("N"),
                ItemId = e.Item.Id.ToString("N"),
                ItemType = e.MediaInfo.Type,
                ActionType = "play_started"
            });
        }

        public async System.Threading.Tasks.Task StartPlaybackTimer(PlaybackProgressEventArgs e)
        {
            _logger.Info("StartPlaybackTimer : Entered");
            await System.Threading.Tasks.Task.Delay(20000);

            try
            {
                var session = _sessionManager.GetSession(e.DeviceId, e.ClientName, "");
                if (session != null)
                {
                    string event_playing_id = e.Item.Id.ToString("N");
                    string event_user_id = e.Users[0].Id.ToString("N");

                    string session_playing_id = "";
                    if (session.NowPlayingItem != null) session_playing_id = session.NowPlayingItem.Id;
                    string session_user_id = "";
                    if (session.UserId != null) session_user_id = ((Guid)session.UserId).ToString("N");

                    string play_method = "na";
                    if (session.PlayState != null && session.PlayState.PlayMethod != null)
                    {
                        play_method = session.PlayState.PlayMethod.Value.ToString();
                    }
                    if (session.PlayState != null && session.PlayState.PlayMethod == MediaBrowser.Model.Session.PlayMethod.Transcode)
                    {
                        if(session.TranscodingInfo !=  null)
                        {
                            string video_codec = "direct";
                            if(session.TranscodingInfo.IsVideoDirect == false)
                            {
                                video_codec = session.TranscodingInfo.VideoCodec;
                            }
                            string audio_codec = "direct";
                            if (session.TranscodingInfo.IsAudioDirect == false)
                            {
                                audio_codec = session.TranscodingInfo.AudioCodec;
                            }
                            play_method += " (v:" + video_codec + " a:" + audio_codec + ")";
                        }
                    }
                    
                    _logger.Info("StartPlaybackTimer : event_playing_id   = " + event_playing_id);
                    _logger.Info("StartPlaybackTimer : event_user_id      = " + event_user_id);
                    _logger.Info("StartPlaybackTimer : session_playing_id = " + session_playing_id);
                    _logger.Info("StartPlaybackTimer : session_user_id    = " + session_user_id);
                    _logger.Info("StartPlaybackTimer : play_method        = " + play_method);
                    _logger.Info("StartPlaybackTimer : e.ClientName       = " + e.ClientName);
                    _logger.Info("StartPlaybackTimer : e.DeviceName       = " + e.DeviceName);

                    if (event_playing_id == session_playing_id && event_user_id == session_user_id)
                    {
                        _logger.Info("StartPlaybackTimer : All matches, playback registered");
                    }
                    else
                    {
                        _logger.Info("StartPlaybackTimer : Details do not match for play items");
                    }
                }
                else
                {
                    _logger.Info("StartPlaybackTimer : session=Not Found");
                }
            }
            catch(Exception exception)
            {
                _logger.Info("StartPlaybackTimer : Error = " + exception.Message + "\n" + exception.StackTrace);
            }
            _logger.Info("StartPlaybackTimer : Exited");
        }

        private void AddUserAction(UserAction entry)
        {
            _logger.Info("Adding User Action");
            Repository.AddUserAction(entry);
        }
    }
}
