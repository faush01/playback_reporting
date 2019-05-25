using MediaBrowser.Model.Updates;
using System;
using System.Collections.Generic;
using System.Text;

namespace playback_reporting
{
    public static class VersionCheck
    {
        public static bool IsVersionValid(Version app_version, PackageVersionClass server_update_level)
        {
            if (server_update_level != PackageVersionClass.Release)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
