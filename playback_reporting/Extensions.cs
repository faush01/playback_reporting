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
            manager.SaveConfiguration("playback_reporting", options);
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
