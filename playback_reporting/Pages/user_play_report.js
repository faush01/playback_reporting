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

define(['mainTabsManager', Dashboard.getConfigurationResourceUrl('helper_function.js')], function (mainTabsManager) {
    'use strict';

    ApiClient.getUserActivity = function (url_to_get) {
        console.log("getUserActivity Url = " + url_to_get);
        return this.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };

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

            mainTabsManager.setTabs(this, getTabIndex("user_report"), getTabs);

            var user_name = "";
            var user_name_index = window.location.href.indexOf("user=");
            if (user_name_index > -1) {
                user_name = window.location.href.substring(user_name_index + 5);
            }
            //alert(user_name);

            var start_picker = view.querySelector('#start_date');
            var start_date = new Date();
            start_date.setDate(start_date.getDate() - 28);
            start_picker.value = start_date.toDateInputValue();
            start_picker.addEventListener("change", process_click);

            var end_picker = view.querySelector('#end_date');
            var end_date = new Date();
            end_picker.value = end_date.toDateInputValue();
            end_picker.addEventListener("change", process_click);

            var span_days_text = view.querySelector('#span_days');
            
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

                var start = new Date(start_picker.value);
                var end = new Date(end_picker.value);
                if (end > new Date()) {
                    end = new Date();
                    end_picker.value = end.toDateInputValue();
                }

                var days = Date.daysBetween(start, end);
                span_days_text.innerHTML = days;

                var url_to_get = "user_usage_stats/UserPlaylist?user_id=" + selected_user_id + "&days=" + days + "&end_date=" + end_picker.value + "&stamp=" + new Date().getTime();
                url_to_get = ApiClient.getUrl(url_to_get);
                console.log("User Report Details Url: " + url_to_get);

                var load_status = view.querySelector('#user_playlist_status');
                load_status.innerHTML = "Loading Data...";

                ApiClient.getUserActivity(url_to_get).then(function (usage_data) {
                    load_status.innerHTML = "&nbsp;";
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
                    
                }, function (response) { load_status.innerHTML = response.status + ":" + response.statusText; });

            }


        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});