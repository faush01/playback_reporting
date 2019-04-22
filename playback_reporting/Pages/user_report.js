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

define(['libraryMenu', Dashboard.getConfigurationResourceUrl('helper_function.js')], function (libraryMenu) {
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

    return function (view, params) {

        // init code here
        view.addEventListener('viewshow', function (e) {

            libraryMenu.setTabs('playback_reporting', getTabIndex("user_report"), getTabs);

            var end_date = view.querySelector('#end_date');
            end_date.value = new Date().toDateInputValue();
            end_date.addEventListener("change", process_click);

            var weeks = view.querySelector('#weeks');
            weeks.addEventListener("change", process_click);
            var days = parseInt(weeks.value) * 7;

            process_click();

            function process_click() {
                var days = parseInt(weeks.value) * 7;
                var url = "user_usage_stats/user_activity?days=" + days + "&end_date=" + end_date.value + "&stamp=" + new Date().getTime();
                url = ApiClient.getUrl(url);
                ApiClient.getUserActivity(url).then(function (user_data) {
                    console.log("usage_data: " + JSON.stringify(user_data));
                    var table_body = view.querySelector('#user_report_results');
                    var row_html = "";

                    for (var index = 0; index < user_data.length; ++index) {
                        var user_info = user_data[index];

                        row_html += "<tr class='detailTableBodyRow detailTableBodyRow-shaded'>";

                        var user_image = "css/images/logindefault.png";
                        if (user_info.has_image) {
                            user_image = "Users/" + user_info.user_id + "/Images/Primary?width=50";
                            user_image = ApiClient.getUrl(user_image);
                        }                      

                        row_html += "<td><img src='" + user_image + "' style='height:2.9em;border-radius:60px;margin-right:.5em;'></td>";

                        var report_url = Dashboard.getConfigurationPageUrl('user_play_report') + "&user=" + encodeURI(user_info.user_name);
                        var name_link = "<a is='emby-linkbutton' href='" + report_url + "'>" + user_info.user_name + "</a>";
                        row_html += "<td>" + name_link + "</td>";

                        row_html += "<td>" + user_info.last_seen + "</td>";
                        row_html += "<td>" + user_info.item_name + "</td>";
                        row_html += "<td>" + user_info.client_name + "</td>";
                        row_html += "<td>" + user_info.total_count + "</td>";
                        row_html += "<td>" + user_info.total_play_time + "</td>";

                        row_html += "</tr>";
                    }

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