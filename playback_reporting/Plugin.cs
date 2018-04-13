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
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.user_playback_report.html",
                    EnableInMainMenu = true
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
                }
            };
        }
    }
}
