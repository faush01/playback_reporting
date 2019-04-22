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

define(['libraryMenu'], function (libraryMenu) {
    'use strict';

    Date.prototype.toDateInputValue = function () {
        var local = new Date(this);
        local.setMinutes(this.getMinutes() - this.getTimezoneOffset());
        return local.toJSON().slice(0, 10);
    };

    ApiClient.getUserActivity = function (url_to_get) {
        console.log("getUserActivity Url = " + url_to_get);
        return this.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };

    function getTabs() {
        var tabs = [
            {
                href: Dashboard.getConfigurationPageUrl('activity_report'),
                name: 'Activity'
            },
            {
                href: Dashboard.getConfigurationPageUrl('user_report'),
                name: 'Users'
            },
            {
                href: Dashboard.getConfigurationPageUrl('user_play_report'),
                name: 'UserPlayList'
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
                name: 'Usage'
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

    function seconds2time(seconds) {
        var h = Math.floor(seconds / 3600);
        seconds = seconds - h * 3600;
        var m = Math.floor(seconds / 60);
        var s = seconds - m * 60;
        var time_string = padLeft(h) + ":" + padLeft(m) + ":" + padLeft(s);
        return time_string;
    }

    function padLeft(value) {
        if (value < 10) {
            return "0" + value;
        }
        else {
            return value;
        }
    }

    return function (view, params) {

        // init code here
        view.addEventListener('viewshow', function (e) {

            libraryMenu.setTabs('user_play_report', 2, getTabs);

            var user_name = "";
            var user_name_index = window.location.href.indexOf("user=");
            if (user_name_index > -1) {
                user_name = window.location.href.substring(user_name_index + 5);
            }
            //alert(user_name);

            var end_date = view.querySelector('#end_date');
            end_date.value = new Date().toDateInputValue();
            end_date.addEventListener("change", process_click);

            var weeks = view.querySelector('#weeks');
            weeks.addEventListener("change", process_click);
            
            var user_list_selector = view.querySelector('#user_list');
            user_list_selector.addEventListener("change", process_click);

            // add user list to selector
            var url = "user_usage_stats/user_list?stamp=" + new Date().getTime();
            url = ApiClient.getUrl(url);
            ApiClient.getUserActivity(url).then(function (user_list) {
                //alert("Loaded Data: " + JSON.stringify(user_list));
                var index = 0;
                var options_html = "<option value=''>Select User</option>";
                var item_details;
                for (index = 0; index < user_list.length; ++index) {
                    item_details = user_list[index];
                    if (user_name === item_details.name) {
                        options_html += "<option value='" + item_details.id + "' selected>" + item_details.name + "</option>";
                    }
                    else {
                        options_html += "<option value='" + item_details.id + "'>" + item_details.name + "</option>";
                    }

                }
                user_list_selector.innerHTML = options_html;

                process_click();
            });

            
            function process_click() {
                
                var selected_user_id = user_list_selector.options[user_list_selector.selectedIndex].value;
                
                if (selected_user_id === "Select User") {
                    view.querySelector('#user_playlist_results').innerHTML = "";
                    return;
                }

                var days = parseInt(weeks.value) * 7;
                var url_to_get = "user_usage_stats/UserPlaylist?user_id=" + selected_user_id + "&days=" + days + "&end_date=" + end_date.value + "&stamp=" + new Date().getTime();
                url_to_get = ApiClient.getUrl(url_to_get);
                console.log("User Report Details Url: " + url_to_get);

                ApiClient.getUserActivity(url_to_get).then(function (usage_data) {
                    //alert("Loaded Data: " + JSON.stringify(usage_data));

                    var row_html = "";
                    var last_date_string = "";
                    usage_data.forEach(function (item_details, index) {

                        if (last_date_string !== item_details.date) {
                            last_date_string = item_details.date;
                            row_html += "<tr class=''>";
                            row_html += "<td colspan='3'><strong>" + last_date_string + "</strong></td>";
                            row_html += "</tr>";
                        }

                        row_html += "<tr class='detailTableBodyRow detailTableBodyRow-shaded'>";
                        row_html += "<td style='padding-left:30px'>" + item_details.type + "</td>";
                        row_html += "<td>" + item_details.name + "</td>";
                        row_html += "<td>" + seconds2time(item_details.duration) + "</td>";
                        row_html += "</tr>";
                    });

                    var table_body = view.querySelector('#user_playlist_results');
                    table_body.innerHTML = row_html;
                    
                });

            }


        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});