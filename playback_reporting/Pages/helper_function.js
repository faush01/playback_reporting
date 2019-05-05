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

function getTabs() {
    var tabs = [
        {
            href: Dashboard.getConfigurationPageUrl('activity_report'),
            name: 'Active'
        },
        {
            href: Dashboard.getConfigurationPageUrl('resource_usage'),
            name: 'Resources'
        },
        {
            href: Dashboard.getConfigurationPageUrl('user_report'),
            name: 'Users'
        },
        {
            href: Dashboard.getConfigurationPageUrl('user_playback_report'),
            name: 'Playback'
        },
        {
            href: Dashboard.getConfigurationPageUrl('breakdown_report'),
            name: 'Breakdown'
        },
        {
            href: Dashboard.getConfigurationPageUrl('hourly_usage_report'),
            name: 'Time'
        },
        {
            href: Dashboard.getConfigurationPageUrl('duration_histogram_report'),
            name: 'Duration'
        },
        {
            href: Dashboard.getConfigurationPageUrl('custom_query'),
            name: 'Query'
        },
        {
            href: Dashboard.getConfigurationPageUrl('playback_report_settings'),
            name: 'Settings'
        }];
    return tabs;
}

function getTabIndex(tab_name) {
    var tabs = getTabs();
    var index = 0;
    for (index = 0; index < tabs.length; ++index) {
        var path = tabs[index].href;
        if (path.endsWith("=" + tab_name)) {
            return index;
        }
    }
    return -1;
}

Date.prototype.toDateInputValue = function () {
    var local = new Date(this);
    local.setMinutes(this.getMinutes() - this.getTimezoneOffset());
    return local.toJSON().slice(0, 10);
};

Date.daysBetween = function (date1, date2) {
    //Get 1 day in milliseconds
    var one_day = 1000 * 60 * 60 * 24;

    // Convert both dates to milliseconds
    var date1_ms = date1.getTime();
    var date2_ms = date2.getTime();

    // Calculate the difference in milliseconds
    var difference_ms = date2_ms - date1_ms;

    // Convert back to days and return
    return Math.round(difference_ms / one_day);
};



