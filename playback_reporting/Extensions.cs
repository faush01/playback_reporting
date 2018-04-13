using System.Collections.Generic;
using MediaBrowser.Common.Configuration;

namespace playback_reporting
{
    public static class ConfigurationExtension
    {
        public static ReportPlaybackOptions GetReportPlaybackOptions(this IConfigurationManager manager)
        {
            return manager.GetConfiguration<ReportPlaybackOptions>("playback_reporting");
        }
        public static void SaveReportPlaybackOptions(this IConfigurationManager manager, ReportPlaybackOptions options)
        {
            manager.SaveConfiguration("autoorganize", options);
        }
    }

    public class ReportPlaybackOptionsFactory : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new List<ConfigurationStore>
            {
                new ConfigurationStore
                {
                    Key = "playback_reporting",
                    ConfigurationType = typeof (ReportPlaybackOptions)
                }
            };
        }
    }
}
