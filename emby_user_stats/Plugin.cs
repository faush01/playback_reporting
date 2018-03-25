using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace emby_user_stats
{
    class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public override string Name => "User Usage Statistics";
        public override Guid Id => new Guid("9E6EB40F-9A1A-4CA1-A299-62B4D252453E");
        public override string Description => "Show user usage statistics";
        public static Plugin Instance { get; private set; }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "MainConfig",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.config.html",
                    EnableInMainMenu = true
                },
                new PluginPageInfo
                {
                    Name = "ConfigJs",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.config.js"
                },
                new PluginPageInfo
                {
                    Name = "d3.v4.min.js",
                    EmbeddedResourcePath = GetType().Namespace + ".Pages.d3.v4.min.js"
                }
            };
        }
    }
}
