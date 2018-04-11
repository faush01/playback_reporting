using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace playback_reporting.Data
{
    class PlaybackTracker
    {
        private bool IsPaused = false;
        public PlaybackInfo TrackedPlaybackInfo { set; get; }
        private readonly ILogger _logger;
        private List<KeyValuePair<DateTime, ACTION_TYPE>> event_tracking = new List<KeyValuePair<DateTime, ACTION_TYPE>>();
        private string tracker_key;

        private enum ACTION_TYPE { START, STOP, PAUSE, UNPAUSE, NONE };

        public PlaybackTracker(string key, ILogger logger)
        {
            _logger = logger;
            tracker_key = key;
        }

        public void ProcessProgress(PlaybackProgressEventArgs e)
        {
            if (IsPaused != e.IsPaused)
            {
                KeyValuePair<DateTime, ACTION_TYPE> play_event;
                if (e.IsPaused)
                {
                    play_event = new KeyValuePair<DateTime, ACTION_TYPE>(DateTime.Now, ACTION_TYPE.PAUSE);
                    _logger.Info("PlaybackTracker : Adding Paused Event : " + play_event.Key.ToString());
                }
                else
                {
                    play_event = new KeyValuePair<DateTime, ACTION_TYPE>(DateTime.Now, ACTION_TYPE.UNPAUSE);
                    _logger.Info("PlaybackTracker : Adding Unpaused Event : " + play_event.Key.ToString());
                }
                event_tracking.Add(play_event);

                IsPaused = e.IsPaused;
            }
        }

        public void ProcessStart(PlaybackProgressEventArgs e)
        {
            IsPaused = e.IsPaused;
            KeyValuePair<DateTime, ACTION_TYPE> play_event = new KeyValuePair<DateTime, ACTION_TYPE>(DateTime.Now, ACTION_TYPE.START);
            event_tracking.Add(play_event);
            _logger.Info("PlaybackTracker : Adding Start Event : " + play_event.Key.ToString());
        }

        public void ProcessStop(PlaybackStopEventArgs e)
        {
            IsPaused = e.IsPaused;
            KeyValuePair<DateTime, ACTION_TYPE> play_event = new KeyValuePair<DateTime, ACTION_TYPE>(DateTime.Now, ACTION_TYPE.STOP);
            event_tracking.Add(play_event);
            _logger.Info("PlaybackTracker : Adding Stop Event : " + play_event.Key.ToString());

            CalculateDuration();
        }

        public void CalculateDuration()
        {
            int duration = 0;

            _logger.Info("PlaybackTracker : Finding Duration : EventCount : " + event_tracking.Count);

            KeyValuePair<DateTime, ACTION_TYPE> prev_event = new KeyValuePair<DateTime, ACTION_TYPE>(DateTime.Now, ACTION_TYPE.NONE);

            foreach (KeyValuePair<DateTime, ACTION_TYPE> e in event_tracking)
            {
                if(prev_event.Value != ACTION_TYPE.NONE)
                {
                    ACTION_TYPE action01 = prev_event.Value;
                    ACTION_TYPE action02 = e.Value;
                    // count up the activity that is considered PLAYING i.e. the client was actually playing and not paused
                    if ((action01 == ACTION_TYPE.START || action01 == ACTION_TYPE.UNPAUSE) && (action02 == ACTION_TYPE.STOP || action02 == ACTION_TYPE.PAUSE))
                    {
                        TimeSpan diff = e.Key.Subtract(prev_event.Key);
                        double diff_seconds = diff.TotalSeconds;
                        duration += (int)diff_seconds;
                        _logger.Info("PlaybackTracker : Event : Time Diff : " + (int)diff_seconds + " total : " + duration);
                    }
                }

                _logger.Info("PlaybackTracker : Event : " + e.Key.ToString() + " " + e.Value);
                prev_event = e;
            }

            _logger.Info("PlaybackTracker : Calculated total play duration : " + duration);
            if (TrackedPlaybackInfo != null)
            {
                TrackedPlaybackInfo.PlaybackDuration = duration;
            }
        }

    }
}
