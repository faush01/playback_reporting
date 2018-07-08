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

using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace playback_reporting
{
    class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public override string Name => "Playback Reporting";
        public override Guid Id => new Guid("9E6EB40F-9A1A-4CA1-A299-62B4D252453E");
        public override string Description => "Show reports for playback activity";
        public static Plugin Instance { get; private set; }
        public PluginConfiguration PluginConfiguration => Configuration;

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "user_playback_report",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.user_playback_report.html"
                },
                new PluginPageInfo
                {
                    Name = "user_playback_report.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.user_playback_report.js"
                },
                new PluginPageInfo
                {
                    Name = "Chart.bundle.min.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.Chart.bundle.min.js"
                },
                new PluginPageInfo
                {
                    Name = "playback_report_settings",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.playback_report_settings.html"
                },
                new PluginPageInfo
                {
                    Name = "playback_report_settings.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.playback_report_settings.js"
                },
                new PluginPageInfo
                {
                    Name = "hourly_usage_report",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.hourly_usage_report.html"
                },
                new PluginPageInfo
                {
                    Name = "hourly_usage_report.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.hourly_usage_report.js"
                },
                new PluginPageInfo
                {
                    Name = "breakdown_report",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.breakdown_report.html"
                },
                new PluginPageInfo
                {
                    Name = "breakdown_report.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.breakdown_report.js"
                },
                new PluginPageInfo
                {
                    Name = "duration_histogram_report",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.duration_histogram_report.html"
                },
                new PluginPageInfo
                {
                    Name = "duration_histogram_report.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.duration_histogram_report.js"
                },
                new PluginPageInfo
                {
                    Name = "user_report",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.user_report.html",
                    EnableInMainMenu = true
                },
                new PluginPageInfo
                {
                    Name = "user_report.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.user_report.js"
                }
            };
        }
    }
}
