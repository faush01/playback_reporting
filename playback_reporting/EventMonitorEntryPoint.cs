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
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using MediaBrowser.Model.Dto;
using System.Diagnostics;
using System.Linq;

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

        private readonly object syncLock = new object();
        private Dictionary<string, PlaybackInfo> playback_trackers = null;
        private ActivityRepository _repository;

        public EventMonitorEntryPoint(ISessionManager sessionManager,
            ILibraryManager libraryManager,
            IUserManager userManager,
            IServerConfigurationManager config,
            IServerApplicationHost appHost,
            ILogManager logger,
            IFileSystem fileSystem,
            IJsonSerializer jsonSerializer)
        {
            _logger = logger.GetLogger("PlaybackReporting - EventMonitorEntryPoint");
            _sessionManager = sessionManager;
            _libraryManager = libraryManager;
            _userManager = userManager;
            _config = config;
            _appHost = appHost;
            _fileSystem = fileSystem;
            _jsonSerializer = jsonSerializer;
            playback_trackers = new Dictionary<string, PlaybackInfo>();
        }

        public void Dispose()
        {

        }

        public void Run()
        {
            _logger.Info("EventMonitorEntryPoint Running");

            if (VersionCheck.IsVersionValid(_appHost.ApplicationVersion, _appHost.SystemUpdateLevel) == false)
            {
                _logger.Info("ERROR : Plugin not compatible with this server version");
                return;
            }
            
            _repository = new ActivityRepository(_logger, _config.ApplicationPaths, _fileSystem);
            _repository.Initialize();

            _sessionManager.PlaybackStart += _sessionManager_PlaybackStart;
            _sessionManager.PlaybackStopped += _sessionManager_PlaybackStop;

            // start playback monitor
            System.Threading.Tasks.Task.Run(() => PlaybackMonitoringTask());

            System.Threading.Tasks.Task.Run(() => ResourceMonitoringTask());
        }

        void _sessionManager_PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            _logger.Info("_sessionManager_PlaybackStart : Entered");
            lock (syncLock)
            {
                List<PlaybackInfo> playinfo_list = ProcessSessions();
                RemoveOldPlayinfo(playinfo_list);
            }
        }

        void _sessionManager_PlaybackStop(object sender, PlaybackStopEventArgs e)
        {
            _logger.Info("_sessionManager_PlaybackStop : Entered");
            lock (syncLock)
            {
                List<PlaybackInfo> playinfo_list = ProcessSessions();
                RemoveOldPlayinfo(playinfo_list);
            }
        }

        public async System.Threading.Tasks.Task PlaybackMonitoringTask()
        {
            _logger.Info("PlaybackMonitoringTask : Started");
            int thread_sleep = 20;
            int max_thread_sleep = 300;

            while (true)
            {
                try
                {
                    lock (syncLock)
                    {
                        List<PlaybackInfo> playinfo_list = ProcessSessions();
                        RemoveOldPlayinfo(playinfo_list);
                    }

                    thread_sleep = 20;
                }
                catch (Exception err)
                {
                    _logger.ErrorException("PlaybackMonitoringTask Exception", err);

                    // try to throttle repeated exceptions up to a max of 5 min
                    if (thread_sleep < max_thread_sleep)
                    {
                        thread_sleep = thread_sleep + 10;
                    }
                    _logger.Info("PlaybackMonitoringTask New Thread Sleep : " + thread_sleep);
                }

                await System.Threading.Tasks.Task.Delay(thread_sleep * 1000);
            }
        }

        private List<PlaybackInfo> ProcessSessions()
        {
            List<PlaybackInfo> active_playinfo_list = new List<PlaybackInfo>();

            foreach (SessionInfo session in _sessionManager.Sessions)
            {
                if (session.NowPlayingItem == null)
                {
                    // nothing playing so move on to next
                    continue;
                }

                PlaybackInfo playback_info = GetPlaybackInfo(session);
                active_playinfo_list.Add(playback_info);
                SavePlayStarted(playback_info);

                // if we had a last paused increment paused time
                if (playback_info.LastPauseTime != null)
                {
                    TimeSpan pause_diff = DateTime.Now.Subtract((DateTime)playback_info.LastPauseTime);
                    playback_info.LastPauseTime = null;
                    playback_info.PausedDuration += (int)pause_diff.TotalSeconds;
                }
                // if paused then set this interval paused start time
                if (session.PlayState != null && session.PlayState.IsPaused)
                {
                    playback_info.LastPauseTime = DateTime.Now;
                }

                TimeSpan diff = DateTime.Now.Subtract(playback_info.Date);
                playback_info.PlaybackDuration = (int)diff.TotalSeconds;

                _repository.UpdatePlaybackAction(playback_info);
            }

            return active_playinfo_list;
        }

        private void RemoveOldPlayinfo(List<PlaybackInfo> active_list)
        {
            List<string> key_list = new List<string>();
            foreach (string key in playback_trackers.Keys)
            {
                key_list.Add(key);
            }

            foreach (string key in key_list)
            {
                PlaybackInfo playback_info = playback_trackers[key];
                if (active_list.Contains(playback_info) == false)
                {
                    _logger.Info("Saving final duration for Item : " + key);
                    TimeSpan diff = DateTime.Now.Subtract(playback_info.Date);
                    playback_info.PlaybackDuration = (int)diff.TotalSeconds;

                    // if we had a last paused increment paused time
                    if (playback_info.LastPauseTime != null)
                    {
                        TimeSpan pause_diff = DateTime.Now.Subtract((DateTime)playback_info.LastPauseTime);
                        playback_info.LastPauseTime = null;
                        playback_info.PausedDuration += (int)pause_diff.TotalSeconds;
                    }

                    _repository.UpdatePlaybackAction(playback_info);

                    _logger.Info("Removing Old Key from playback_trackers : " + key);
                    playback_trackers.Remove(key);
                }
            }
        }

        private void SavePlayStarted(PlaybackInfo playback_info)
        {
            if (playback_info.StartupSaved == false)
            {
                _logger.Info("Saving PlaybackInfo to DB");
                _repository.AddPlaybackAction(playback_info);
                playback_info.StartupSaved = true;
            }
        }

        private PlaybackInfo GetPlaybackInfo(SessionInfo session)
        {
            string userId = session.UserId;
            string deviceId = session.DeviceId;
            string deviceName = session.DeviceName;
            string clientName = session.Client;
            string session_playing_id = session.NowPlayingItem.Id;

            string key = deviceId + "-" + userId + "-" + session_playing_id;

            PlaybackInfo playback_info = null;
            if (playback_trackers.ContainsKey(key))
            {
                //_logger.Info("Existing tracker found! : " + key);
                playback_info = playback_trackers[key];
            }
            else
            {
                _logger.Info("Adding PlaybackInfo to playback_trackers : " + key);
                playback_info = new PlaybackInfo();

                BaseItemDto item = session.NowPlayingItem;

                playback_info.Key = key;
                playback_info.Date = DateTime.Now;
                playback_info.UserId = userId;
                playback_info.DeviceName = deviceName;
                playback_info.ClientName = clientName;
                playback_info.ItemId = session_playing_id;
                playback_info.ItemName = GetItemName(session.NowPlayingItem);
                playback_info.PlaybackMethod = GetPlaybackMethod(session);
                playback_info.ItemType = session.NowPlayingItem.Type;
                playback_info.ItemType = session.NowPlayingItem.Type;

                playback_trackers.Add(key, playback_info);
            }

            return playback_info;
        }

        private string GetPlaybackMethod(SessionInfo session)
        {
            string play_method = "na";
            if (session.PlayState != null && session.PlayState.PlayMethod != null)
            {
                play_method = session.PlayState.PlayMethod.Value.ToString();
            }
            if (session.PlayState != null && session.PlayState.PlayMethod == MediaBrowser.Model.Session.PlayMethod.Transcode)
            {
                if (session.TranscodingInfo != null)
                {
                    string video_codec = "direct";
                    if (session.TranscodingInfo.IsVideoDirect == false)
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

            return play_method;
        }

        private string GetItemName(BaseItemDto item)
        {
            string item_name = "Not Known";

            if (item == null)
            {
                return item_name;
            }

            if (item.Type == "Episode")
            {
                string series_name = item.SeriesName;
                string season_no = String.Format("{0:D2}", item.ParentIndexNumber);
                string epp_no = String.Format("{0:D2}", item.IndexNumber);
                item_name = series_name + " - s" + season_no + "e" + epp_no + " - " + item.Name;
            }
            else if (item.Type == "Audio")
            {
                string artist = "Not Known";
                if (item.ArtistItems != null && item.AlbumArtists.Length > 0)
                {
                    List<string> artists_list = new List<string>();
                    foreach (var artist_pair in item.AlbumArtists)
                    {
                        artists_list.Add(artist_pair.Name);
                    }
                    artist = string.Join(", ", artists_list);
                }
                string album = "Not Known";
                if (string.IsNullOrEmpty(item.Album) == false)
                {
                    album = item.Album;
                }
                item_name = artist + " - " + item.Name + " (" + album + ")";

            }
            else
            {
                item_name = item.Name;
            }

            return item_name;
        }

        public async System.Threading.Tasks.Task ResourceMonitoringTask()
        {
            _logger.Info("ResourceMonitoringTask:Started");
            DateTime last_run_time = DateTime.Now;

            List<double> cpu_values = new List<double>();
            List<long> mem_values = new List<long>();

            ResourcesCounters resource_counters = ResourcesCounters.Instance;
            Dictionary<string, ProcessDetails> process_list = resource_counters.GetProcessList();

            while (true)
            {
                try
                {
                    foreach (var proc in Process.GetProcesses())
                    {
                        DateTime now = DateTime.Now;
                        string process_key = proc.Id + "-" + proc.ProcessName;
                        ProcessDetails proc_details = null;

                        if (process_list.ContainsKey(process_key) == false)
                        {
                            proc_details = new ProcessDetails(proc);
                            _logger.Debug("Adding Process:{0}", proc_details);
                            process_list.Add(process_key, proc_details);
                        }
                        else
                        {
                            proc_details = process_list[process_key];
                        }

                        // try to get working set memory if we have not thrown an exception in the past
                        if (proc_details.ExceptionTypes.Contains(ExceptionType.MemoryException) == false)
                        {
                            try
                            {
                                proc_details.Memory = proc.WorkingSet64;
                            }
                            catch(Exception e)
                            {
                                _logger.Debug("Adding Exception Thrown Error for MemoryException");
                                proc_details.ExceptionTypes.Add(ExceptionType.MemoryException);
                                if (string.IsNullOrEmpty(proc_details.ErrorMessage))
                                {
                                    proc_details.ErrorMessage = "Mem:" + e.Message;
                                }
                            }
                        }
                            
                        double proc_total_ms = 0;
                        if (proc_details.ExceptionTypes.Contains(ExceptionType.TimeException) == false)
                        {
                            try
                            {
                                proc_total_ms = proc.TotalProcessorTime.TotalMilliseconds;
                            }
                            catch (Exception e)
                            {
                                _logger.Debug("Adding Exception Thrown Error for TimeException");
                                proc_details.ExceptionTypes.Add(ExceptionType.TimeException);
                                if (string.IsNullOrEmpty(proc_details.ErrorMessage))
                                {
                                    proc_details.ErrorMessage = "Cpu:" + e.Message;
                                }
                            }
                        }

                        if (proc_total_ms > 0 && proc_details.LastSampleTime != DateTime.MinValue)
                        {
                            double time_diff = (now - proc_details.LastSampleTime).TotalMilliseconds;
                            double proc_time_diff = proc_total_ms - proc_details.TotalMilliseconds_last;
                            double cpuUsageTotal = proc_time_diff / (Environment.ProcessorCount * time_diff);
                            proc_details.CpuUsage = cpuUsageTotal * 100;
                        }
                        else
                        {
                            proc_details.CpuUsage = 0;
                        }

                        proc_details.Updated = true;
                        proc_details.LastSampleTime = now;
                        proc_details.TotalMilliseconds_last = proc_total_ms;

                     }

                    // calculate totals
                    double total_cpu = 0;
                    long total_mem = 0;
                    string[] proc_keys = process_list.Keys.ToArray();
                    foreach (string key in proc_keys)
                    {
                        ProcessDetails proc_details = process_list[key];
                        if(proc_details.Updated)
                        {
                            proc_details.Updated = false;
                            total_cpu += proc_details.CpuUsage;
                            total_mem += proc_details.Memory;
                        }
                        else
                        {
                            _logger.Debug("Removing Process:{0}", proc_details);
                            process_list.Remove(key);
                        }
                    }

                    //_logger.Debug("CPU:{0} Mem:{1}", total_cpu, total_mem);
                    Dictionary<string, object> counters = new Dictionary<string, object>();
                    counters.Add("date", DateTime.Now);
                    counters.Add("cpu", total_cpu);
                    counters.Add("mem", total_mem);
                    _repository.AddResourceCounter(counters);
                }
                catch (Exception e)
                {
                    _logger.Debug("ResourceMonitoringTask Error: {0}", e);
                }

                await System.Threading.Tasks.Task.Delay(60000);
            }
        }

        public async System.Threading.Tasks.Task ResourceMonitoringTask2()
        {
            _logger.Info("ResourceMonitoringTask:Started");
            DateTime last_run_time = DateTime.Now;

            List<double> cpu_values = new List<double>();
            List<long> mem_values = new List<long>();

            ResourcesCounters resource_counters = ResourcesCounters.Instance;
            Dictionary<string, ProcessDetails> process_list = resource_counters.GetProcessList();

            while (true)
            {
                try
                {
                    List<string> current_proceses = new List<string>();

                    foreach (var proc in Process.GetProcesses())
                    {
                        try
                        {
                            string process_key = proc.Id + "-" + proc.ProcessName;
                            current_proceses.Add(process_key);

                            ProcessDetails proc_details = null;

                            if (process_list.ContainsKey(process_key))
                            {
                                proc_details = process_list[process_key];
                                proc_details.Memory = proc.WorkingSet64;
                                DateTime now = DateTime.Now;
                                double proc_total_ms = 0;
                                try
                                {
                                    proc_total_ms = proc.TotalProcessorTime.TotalMilliseconds;
                                }
                                catch(Exception e)
                                {
                                    if (string.IsNullOrEmpty(proc_details.ErrorMessage))
                                    {
                                        proc_details.ErrorMessage = "TotalProcessorTime:" + e.Message;
                                    }
                                }

                                if (proc_total_ms > 0 && proc_details.LastSampleTime != DateTime.MinValue)
                                {
                                    double time_diff = (now - proc_details.LastSampleTime).TotalMilliseconds;
                                    double proc_time_diff = proc_total_ms - proc_details.TotalMilliseconds_last;
                                    double cpuUsageTotal = proc_time_diff / (Environment.ProcessorCount * time_diff);
                                    proc_details.CpuUsage = cpuUsageTotal * 100;
                                }
                                else
                                {
                                    proc_details.CpuUsage = 0;
                                }

                                proc_details.LastSampleTime = now;
                                proc_details.TotalMilliseconds_last = proc_total_ms;

                                process_list[process_key] = proc_details;
                            }
                            else
                            {
                                proc_details = new ProcessDetails(proc);
                                _logger.Debug("Adding Process:{0}", proc_details);
                                process_list.Add(process_key, proc_details);
                            }
                        }
                        catch (Exception e1)
                        {
                            string process_key = proc.Id + "-" + proc.ProcessName;
                            if (process_list.ContainsKey(process_key))
                            {
                                ProcessDetails proc_details = process_list[process_key];
                                proc_details.ErrorMessage = e1.Message;
                            }
                            else
                            {
                                ProcessDetails proc_details = new ProcessDetails();
                                proc_details.Id = proc.Id;
                                proc_details.Name = proc.ProcessName;
                                proc_details.ErrorMessage = e1.Message;
                                process_list.Add(process_key, proc_details);
                            }
                        }
                    }

                    // calculate totals
                    double total_cpu = 0;
                    long total_mem = 0;
                    string[] proc_keys = process_list.Keys.ToArray();
                    foreach (string key in proc_keys)
                    {
                        if (current_proceses.Contains(key) == false)
                        {
                            ProcessDetails proc_details = process_list[key];
                            _logger.Debug("Removing Process:{0}", proc_details);
                            process_list.Remove(key);
                        }
                        else
                        {
                            ProcessDetails proc_details = process_list[key];

                            total_cpu += proc_details.CpuUsage;
                            total_mem += proc_details.Memory;
                        }
                    }

                    //_logger.Debug("CPU:{0} Mem:{1}", total_cpu, total_mem);

                    cpu_values.Add(total_cpu);
                    mem_values.Add(total_mem);

                    if (cpu_values.Count >= 6)
                    {
                        double cpu_running_average = cpu_values.Sum(x => Convert.ToDouble(x));
                        cpu_running_average = cpu_running_average / cpu_values.Count;
                        cpu_values.Clear();

                        long mem_running_average = mem_values.Sum(x => Convert.ToInt64(x));
                        mem_running_average = mem_running_average / mem_values.Count;
                        mem_values.Clear();

                        Dictionary<string, object> counters = new Dictionary<string, object>();
                        counters.Add("date", DateTime.Now);
                        counters.Add("cpu", cpu_running_average);
                        counters.Add("mem", mem_running_average);
                        _repository.AddResourceCounter(counters);
                    }

                }
                catch (Exception e)
                {
                    _logger.Debug("ResourceMonitoringTask Error: {0}", e);
                }

                await System.Threading.Tasks.Task.Delay(10000);
            }
        }
    }
}
