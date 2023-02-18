﻿/*
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

define(['mainTabsManager', 'appRouter', 'emby-linkbutton', Dashboard.getConfigurationResourceUrl('helper_function.js')], function (mainTabsManager, appRouter) {
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

            mainTabsManager.setTabs(this, getTabIndex("user_play_report"), getTabs);

            var parameters = {};
            var queryString = window.location.href.split('?')[1];
            if (queryString) {
                var params = queryString.split('&');
                for (var i = 0; i < params.length; i++) {
                    var parts = params[i].split('=');
                    var paramName = parts[0];
                    var paramValue = typeof (parts[1]) === 'undefined' ? true : parts[1];
                    if (!parameters[paramName]) {
                        parameters[paramName] = decodeURI(paramValue);
                    }
                }
            }

            //alert(parameters["user"]);
            //alert(parameters["filter_name"]);

            var user_name = "";
            if (parameters["user"]) {
                user_name = parameters["user"];
            }

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

            var aggregate_data = view.querySelector('#aggregate');
            aggregate_data.addEventListener("change", process_click);

            var filter_name_input = view.querySelector('#filter_name');
            if (parameters["filter_name"]) {
                filter_name_input.value = parameters["filter_name"];
            }
            filter_name_input.addEventListener("change", process_click);

            // add user list to selector
            var url = "user_usage_stats/user_list?stamp=" + new Date().getTime();
            url = ApiClient.getUrl(url);

            ApiClient.getUserActivity(url).then(function (user_list) {
                //alert("Loaded Data: " + JSON.stringify(user_list));
                var index = 0;
                var options_html = "<option value=''>All Users</option>";
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
                
                //if (selected_user_id === "Select User") {
                //    view.querySelector('#user_playlist_results').innerHTML = "";
                //    return;
                //}

                var filter_name = filter_name_input.value;
                var encoded_filter_name = encodeURI(filter_name);

                var aggregate = aggregate_data.checked;

                var start = new Date(start_picker.value);
                var end = new Date(end_picker.value);
                if (end > new Date()) {
                    end = new Date();
                    end_picker.value = end.toDateInputValue();
                }

                var days = Date.daysBetween(start, end);
                span_days_text.innerHTML = days;

                var url_to_get = "user_usage_stats/UserPlaylist?aggregate_data=" + aggregate + "&user_id=" + selected_user_id + "&days=" + days + "&end_date=" + end_picker.value + "&filter_name=" + encoded_filter_name + "&stamp=" + new Date().getTime();
                url_to_get = ApiClient.getUrl(url_to_get);
                console.log("User Report Details Url: " + url_to_get);

                var load_status = view.querySelector('#user_playlist_status');
                load_status.innerHTML = "Loading Data...";

                ApiClient.getUserActivity(url_to_get).then(function (usage_data) {
                    load_status.innerHTML = "&nbsp;";
                    //console.log("Loaded UserPlaylist Data: " + JSON.stringify(usage_data));

                    var row_html = "";
                    var last_date_string = "";
                    var row_count = 0
                    usage_data.forEach(function (item_details, index) {

                        if (last_date_string !== item_details.date) {
                            last_date_string = item_details.date;
                            row_html += "<tr class=''>";
                            row_html += "<td colspan='4'><strong>" + last_date_string + "</strong></td>";
                            row_html += "</tr>";
                            row_count = 0
                        }

                        var row_bg_col = "#77777700";
                        if (row_count % 2 == 0) {
                            row_bg_col = "#7777771c";
                        }
                        row_count += 1
                        row_html += "<tr>";

                        row_html += "<td style='width:30px;'>&nbsp;</td>"

                        var user_image = "<i class='md-icon' style='font-size:30px;width:30px;height:30px;'></i>";
                        if (item_details.user_has_image) {
                            var user_img = "Users/" + item_details.user_id + "/Images/Primary?height=152&&quality=90";
                            user_img = ApiClient.getUrl(user_img);
                            user_image = "<img src='" + user_img + "' style='object-fit:cover;width:30px;height:30px;border-radius:1000px;vertical-align:top;'>";
                        }
                        row_html += "<td style='padding-left:15px;padding-right:15px;background:" + row_bg_col + ";'>";
                        row_html += "<table style='padding: 0px; border-spacing: 0px;'>";
                        row_html += "<tr>";
                        row_html += "<td style='vertical-align: middle; width:35px; padding: 0px;' align='center'>" + user_image + "</td>";
                        row_html += "<td style='vertical-align: middle; padding: 0px;'>" + item_details.user_name + "</td>";
                        row_html += "</tr>";
                        row_html += "</table>";
                        row_html += "</td>";

                        if (!aggregate) {
                            row_html += "<td style='padding-left:15px;padding-right:15px;background:" + row_bg_col + ";'>" + item_details.time + "</td>";
                        }
                        row_html += "<td style='padding-left:15px;padding-right:15px;background:" + row_bg_col + ";'>" + item_details.item_type + "</td>";

                        var name_link = appRouter.getRouteUrl({ Id: item_details.item_id, ServerId: ApiClient._serverInfo.Id });
                        var item_link = "<a href='" + name_link + "' is='emby-linkbutton' class='button-link' title='View Emby item'>" + item_details.item_name + "</a>";

                        var direct_name_link = "/web/index.html#!/item?id=" + item_details.item_id + "&serverId=" + ApiClient._serverInfo.Id;
                        var new_window = "<i class='md-icon' style='cursor: pointer; font-size:100%;' onClick='window.open(\"" + direct_name_link + "\");' title='Open Emby item in new window'>launch</i>"

                        var item_name_link = item_link + "&nbsp;&nbsp;" + new_window;
                        row_html += "<td style='padding-left:15px;padding-right:15px;background:" + row_bg_col + ";'>" + item_name_link + "</td>";

                        row_html += "<td style='padding-left:15px;padding-right:15px;background:" + row_bg_col + ";'>" + seconds2time(item_details.duration) + "</td>";
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