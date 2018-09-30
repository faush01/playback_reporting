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
        public DateTime last_updated = DateTime.MinValue;

        private enum ACTION_TYPE { START, STOP, PAUSE, UNPAUSE, NONE };

        public PlaybackTracker(string key, ILogger logger)
        {
            _logger = logger;
            tracker_key = key;
        }

        public List<string> ProcessProgress(PlaybackProgressEventArgs e)
        {
            List<string> event_log = new List<string>();
            if (IsPaused != e.IsPaused)
            {
                KeyValuePair<DateTime, ACTION_TYPE> play_event;
                if (e.IsPaused)
                {
                    play_event = new KeyValuePair<DateTime, ACTION_TYPE>(DateTime.Now, ACTION_TYPE.PAUSE);
                    event_log.Add("PauseEvent(" + play_event.Key.ToString() + ")");
                }
                else
                {
                    play_event = new KeyValuePair<DateTime, ACTION_TYPE>(DateTime.Now, ACTION_TYPE.UNPAUSE);
                    event_log.Add("UnPaused Event(" + play_event.Key.ToString() + ")");
                }
                event_tracking.Add(play_event);

                IsPaused = e.IsPaused;
            }

            CalculateDuration(event_log);

            return event_log;
        }

        public void ProcessStart(PlaybackProgressEventArgs e)
        {
            IsPaused = e.IsPaused;
            KeyValuePair<DateTime, ACTION_TYPE> play_event = new KeyValuePair<DateTime, ACTION_TYPE>(DateTime.Now, ACTION_TYPE.START);
            event_tracking.Add(play_event);
            _logger.Info("PlaybackTracker : Adding Start Event : " + play_event.Key.ToString());
        }

        public List<string> ProcessStop(PlaybackStopEventArgs e)
        {
            IsPaused = e.IsPaused;
            KeyValuePair<DateTime, ACTION_TYPE> play_event = new KeyValuePair<DateTime, ACTION_TYPE>(DateTime.Now, ACTION_TYPE.STOP);
            event_tracking.Add(play_event);
            _logger.Info("PlaybackTracker : Adding Stop Event : " + play_event.Key.ToString());

            List<string> event_log = new List<string>();
            CalculateDuration(event_log);
            return event_log;
        }

        public void CalculateDuration(List<string> event_log)
        {
            int duration = 0;

            if (TrackedPlaybackInfo == null)
            {
                return;
            }

            List<KeyValuePair<DateTime, ACTION_TYPE>> events = null;
            // if the last event is not a stop event then add one to allow duration calculation to work
            if (event_tracking.Count > 0 && event_tracking[event_tracking.Count - 1].Value != ACTION_TYPE.STOP)
            {
                events = new List<KeyValuePair<DateTime, ACTION_TYPE>>();
                foreach (KeyValuePair<DateTime, ACTION_TYPE> e in event_tracking)
                {
                    events.Add(e);
                }
                KeyValuePair<DateTime, ACTION_TYPE> stop_event = new KeyValuePair<DateTime, ACTION_TYPE>(DateTime.Now, ACTION_TYPE.STOP);
                events.Add(stop_event);
            }
            else
            {
                events = event_tracking;
            }

            event_log.Add("EventCount(" + events.Count + ")");

            KeyValuePair<DateTime, ACTION_TYPE> prev_event = new KeyValuePair<DateTime, ACTION_TYPE>(DateTime.Now, ACTION_TYPE.NONE);

            foreach (KeyValuePair<DateTime, ACTION_TYPE> e in events)
            {
                event_log.Add("Event(" + e.Key.ToString() + "," + e.Value + ")");
                if (prev_event.Value != ACTION_TYPE.NONE)
                {
                    ACTION_TYPE action01 = prev_event.Value;
                    ACTION_TYPE action02 = e.Value;
                    // count up the activity that is considered PLAYING i.e. the client was actually playing and not paused
                    if ((action01 == ACTION_TYPE.START || action01 == ACTION_TYPE.UNPAUSE) && (action02 == ACTION_TYPE.STOP || action02 == ACTION_TYPE.PAUSE))
                    {
                        TimeSpan diff = e.Key.Subtract(prev_event.Key);
                        double diff_seconds = diff.TotalSeconds;
                        duration += (int)diff_seconds;
                        event_log.Add("Diff(" + (int)diff_seconds + ","+ duration + ")");
                    }
                }
                prev_event = e;
            }

            event_log.Add("Total(" + duration + ")");
            TrackedPlaybackInfo.PlaybackDuration = duration;
        }

    }
}
