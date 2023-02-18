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

define(['mainTabsManager', 'appRouter', 'emby-linkbutton', Dashboard.getConfigurationResourceUrl('helper_function.js')], function (mainTabsManager, appRouter) {
    'use strict';

    var my_bar_chart = null;
    var filter_names = [];
    var color_list = [];

    ApiClient.getUserActivity = function (url_to_get) {
        console.log("getUserActivity Url = " + url_to_get);
        return this.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };

    ApiClient.sendCustomQuery = function (url_to_get, query_data) {
        var post_data = JSON.stringify(query_data);
        console.log("sendCustomQuery url  = " + url_to_get);
        console.log("sendCustomQuery data = " + post_data);
        return this.ajax({
            type: "POST",
            url: url_to_get,
            dataType: "json",
            data: post_data,
            contentType: 'application/json'
        });
    };

    function draw_graph(view, local_chart, usage_data, user_list) {

        console.log("draw_graph called");

        if (!local_chart) {
            alert("No Chart Lib : " + local_chart);
            return;
        }

        // sort all user and assign colours
        var full_user_list = [];
        for (var i = 0; i < user_list.length; i++) {
            full_user_list.push(user_list[i].name);
        }

        full_user_list.sort();
        var user_colour_map = {};
        for (var x = 0; x < full_user_list.length; x++) {
            user_colour_map[full_user_list[x]] = color_list[x % color_list.length];
        }

        //console.log("usage_data: " + JSON.stringify(usage_data));
        //console.log("user_list: " + JSON.stringify(user_list));
        //console.log("full_user_list: " + JSON.stringify(full_user_list));
        //console.log("color_list: " + JSON.stringify(color_list));
        //console.log("user_colour_map: " + JSON.stringify(user_colour_map));

        // get labels from the first user
        var text_labels = [];
        if (usage_data.length > 0) {
            for (var date_string in usage_data[0].user_usage) {
                text_labels.push(date_string);
            }
        }
        //console.log("Text Lables: " + JSON.stringify(text_labels));

        var data_type = view.querySelector('#data_type');
        var data_t = data_type.options[data_type.selectedIndex].value === "time";

        var chart_title = "";
        if (data_t) {
            chart_title = "User Playback Report (Minutes Played)";
        }
        else {
            chart_title = "User Playback Report (Play Count)";
        }

        // process user usage into data for chart
        var user_ids = [];
        var user_usage_datasets = [];
        var user_count = 0;
        for (var index = 0; index < usage_data.length; ++index) {
            var user_usage = usage_data[index];
            if (user_usage.user_id !== "labels_user") {
                user_ids.push(user_usage.user_id);
                var point_data = [];
                for (var point_date in user_usage.user_usage) {
                    var data_point = user_usage.user_usage[point_date];
                    point_data.push(data_point);
                }
                var user_bar_colour = user_colour_map[user_usage.user_name];
                if (user_bar_colour === undefined) {
                    user_bar_colour = "#FF0000";
                }
                //var user_bar_colour = color_list[user_count++ % color_list.length];
                var chart_data = {
                    label: user_usage.user_name,
                    backgroundColor: user_bar_colour,
                    data: point_data
                };
                user_usage_datasets.push(chart_data);
            }
        }

        var userUsageChartData = {
            user_id_list: user_ids,
            labels: text_labels,
            datasets: user_usage_datasets
        };
        //console.log("userUsageChartData: " + JSON.stringify(userUsageChartData));

        /*
        var barChartData = {
            labels: ['January', 'February', 'March', 'April', 'May', 'June', 'July'],
            datasets: [{
                label: 'Dataset 1',
                backgroundColor: '#FF0000',
                data: [
                    10,
                    20,
                    30,
                    40,
                    50,
                    60,
                    70
                ]
            }, {
                label: 'Dataset 2',
                backgroundColor: '#0000FF',
                data: [
                    10,
                    20,
                    30,
                    40,
                    50,
                    60,
                    70
                ]
            }, {
                label: 'Dataset 3',
                backgroundColor: '#00FF00',
                data: [
                    10,
                    20,
                    30,
                    40,
                    50,
                    60,
                    70
                ]
            }]

        };
        */

        function y_axis_labels(value, index, values) {
            if (data_t) {
                if (Math.floor(value / 10) === (value / 10)) {
                    return seconds2time(value);
                }
            }
            else {
                return value;
            }
        }

        function tooltip_labels(tooltipItem) {

            var data_index = tooltipItem.dataIndex;
            var label = tooltipItem.dataset.label || '';

            //var label = data.datasets[tooltipItem.datasetIndex].label || '';

            if (label) {
                if (data_t) {
                    label += ": " + seconds2time(tooltipItem.dataset.data[data_index]);
                }
                else {
                    label += ": " + tooltipItem.dataset.data[data_index];
                }
            }
            return label;
        }

        var chart_canvas = view.querySelector('#user_stats_chart_canvas');
        var ctx = chart_canvas.getContext('2d');

        if (my_bar_chart) {
            console.log("destroy() existing chart");
            my_bar_chart.destroy();
        }

        my_bar_chart = new Chart(ctx, {
            type: 'bar',
            data: userUsageChartData,//barChartData,
            options: {
                plugins: {
                    title: {
                        display: true,
                        text: chart_title
                    },
                    tooltip: {
                        mode: 'point',
                        intersect: true,
                        callbacks: {
                            label: tooltip_labels
                        }
                    }
                },
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    x: {
                        stacked: true,
                        ticks: {
                            autoSkip: false,
                            maxTicksLimit: 10000
                        }
                    },
                    y: {
                        stacked: true,
                        ticks: {
                            autoSkip: true,
                            beginAtZero: true,
                            callback: y_axis_labels
                        }
                    }
                },
                onClick: function (e) {
                    var activePoint = my_bar_chart.getElementsAtEventForMode(e, 'nearest', { intersect: true }, false);
                    if (activePoint === undefined || activePoint.length === 0 || activePoint[0] === undefined) {
                        return;
                    }
                    
                    var datasetIndex = activePoint[0].datasetIndex;
                    var index = activePoint[0].index;
                    var data = my_bar_chart.data;

                    var label = data.datasets[datasetIndex].label;
                    var data_label = data.labels[index];
                    var value = data.datasets[datasetIndex].data[index];
                    var user_id = data.user_id_list[datasetIndex];

                    console.log(label, user_id, data_label, value);

                    display_user_report(label, user_id, data_label, view);
                    //var href = Dashboard.getConfigurationPageUrl("UserUsageReport") + "&user=" + user_id + "&date=" + data_label;
                    //Dashboard.navigate(href);
                }
            }
        });

        console.log("Chart Done");

    }

    function display_user_report(user_name, user_id, data_label, view) {
        console.log("Building User Report");

        var all_select = view.querySelector('#media_type_all');
        var movies_select = view.querySelector('#media_type_movies');
        var series_select = view.querySelector('#media_type_series');
        var filter = [];
        for (var x = 0; x < filter_names.length; x++) {
            var filter_name = filter_names[x];
            var filter_checked = view.querySelector('#media_type_filter_' + filter_name).checked;
            if (filter_checked) {
                filter.push(filter_name);
            }
        }

        var url_to_get = "user_usage_stats/" + user_id + "/" + data_label + "/GetItems?filter=" + filter.join(",") + "&stamp=" + new Date().getTime();
        url_to_get = ApiClient.getUrl(url_to_get);
        console.log("User Report Details Url: " + url_to_get);

        ApiClient.getUserActivity(url_to_get).then(function (user_details) {
            //console.log("Loaded User Data: " + JSON.stringify(user_details));
            populate_report(user_name, user_id, data_label, user_details, view);
        });

    }

    function remove_item(index, user_name, user_id, data_label, view) {
        if (!confirm("Are you sure you want to remove this item?")) {
            return;
        }

        var sql = "DELETE FROM PlaybackActivity WHERE rowid = " + index;
        console.log("Remove Item Query : " + sql);

        var url = "user_usage_stats/submit_custom_query?stamp=" + new Date().getTime();
        url = ApiClient.getUrl(url);

        var query_data = {
            CustomQueryString: sql,
            ReplaceUserId: false
        };

        ApiClient.sendCustomQuery(url, query_data).then(function (result) {
            var message = result["message"];
            console.log("Remove Item Result : " + message);
            display_user_report(user_name, user_id, data_label, view);
        });
    }

    function populate_report(user_name, user_id, data_label, user_details, view) {

        if (user_details === undefined) {
            alert("No Data!");
            return;
        }

        var usage_data = user_details.activity;

        //console.log("Processing User Report: " + JSON.stringify(usage_data));

        var user_image = "<i class='md-icon' style='font-size:30px;width:30px;height:30px;'></i>";
        if (user_details.has_image) {
            var user_img = "Users/" + user_id + "/Images/Primary?height=152&&quality=90";
            user_img = ApiClient.getUrl(user_img);
            user_image = "<img src='" + user_img + "' style='object-fit:cover;width:30px;height:30px;border-radius:1000px;vertical-align:top;'>";
        }
        var user_report_image = view.querySelector('#user_report_user_img');
        user_report_image.innerHTML = user_image;

        var user_report_name = view.querySelector('#user_report_user_name');
        user_report_name.innerHTML = user_name;

        var user_report_date = view.querySelector('#user_report_on_date');
        user_report_date.innerHTML = "(" + data_label + ")";

        var table_body = view.querySelector('#user_usage_report_results');

        // build report table
        var row_html = "";
        for (var index = 0; index < usage_data.length; ++index) {
            var item_details = usage_data[index];

            var row_bg_col = "#77777700";
            if (index % 2 == 0) {
                row_bg_col = "#7777771c";
            }

            row_html += "<tr style='background:" + row_bg_col + ";'>";

            row_html += "<td>" + item_details.Time + "</td>";

            var name_link = appRouter.getRouteUrl({ Id: item_details.Id, ServerId: ApiClient._serverInfo.Id });
            var item_link = "<a href='" + name_link + "' is='emby-linkbutton' class='button-link' title='View Emby item'>" + item_details.Name + "</a>";

            var direct_name_link = "/web/index.html#!/item?id=" + item_details.Id + "&serverId=" + ApiClient._serverInfo.Id;
            var new_window = "<i class='md-icon' style='cursor: pointer; font-size:100%;' onClick='window.open(\"" + direct_name_link + "\");' title='Open Emby item in new window'>launch</i>"

            row_html += "<td>" + item_link + "&nbsp;&nbsp;" + new_window + "</td>";

            row_html += "<td>" + item_details.Type + "</td>";
            row_html += "<td>" + item_details.Client + "</td>";
            row_html += "<td>" + item_details.Device + "</td>";
            row_html += "<td>" + item_details.Method + "</td>";
            row_html += "<td>" + seconds2time(item_details.Duration) + "</td>";

            var but_id = "del_but_row_" + item_details.RowId
            var del_link = "<i class='md-icon largeIcon' style='cursor: pointer;font-size:150%;' id='" + but_id + "' title='Delete this entry'>delete</i>"
            row_html += "<td>" + del_link + "</td>";

            row_html += "</tr>";
        }
        table_body.innerHTML = row_html;

        // add delete actions
        usage_data.forEach(function (item_details, index) {
            var but_id = "del_but_row_" + item_details.RowId
            var del_icon = view.querySelector('#' + but_id);
            del_icon.addEventListener("click", function () { remove_item(item_details.RowId, user_name, user_id, data_label, view); });
        });

        // show user activity content
        var user_report_content = view.querySelector('#user_report_content');
        user_report_content.style.display = "block";
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

    function precisionRound(number, precision) {
        var factor = Math.pow(10, precision);
        return Math.round(number * factor) / factor;
    }

    return function (view, params) {

        // init code here
        view.addEventListener('viewshow', function (e) {

            mainTabsManager.setTabs(this, getTabIndex("user_playback_report"), getTabs);

            require([Dashboard.getConfigurationResourceUrl('chart.min.js')], function (d3) {

                var load_status = view.querySelector('#user_stats_chart_status');
                load_status.innerHTML = "Loading Data...";

                // get filter types form sever
                var filter_url = ApiClient.getUrl("user_usage_stats/type_filter_list");
                ApiClient.getUserActivity(filter_url).then(function (filter_data) {
                    filter_names = filter_data;
                
                    // build filter list
                    var filter_items = "";
                    var filter_name = "";
                    var x;
                    for (x = 0; x < filter_names.length; x++) {
                        filter_name = filter_names[x];
                        filter_items += "<input type='checkbox' id='media_type_filter_" + filter_name + "' data_fileter_name='" + filter_name + "' checked> " + filter_name + " ";
                    }

                    var filter_check_list = view.querySelector('#filter_check_list');
                    filter_check_list.innerHTML = filter_items;

                    for (x = 0; x < filter_names.length; x++) {
                        filter_name = filter_names[x];
                        view.querySelector('#media_type_filter_' + filter_name).addEventListener("click", process_click);
                    }

                    var data_type = view.querySelector('#data_type');
                    data_type.addEventListener("change", process_click);

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
                    var days = Date.daysBetween(start_date, end_date);
                    span_days_text.innerHTML = days;

                    process_click();

                    function process_click() {
                        // hide user activity content
                        var user_report_content = view.querySelector('#user_report_content');
                        user_report_content.style.display = "none";
                        // clear user report table
                        var table_body = view.querySelector('#user_usage_report_results');
                        table_body.innerHTML = "";

                        var filter = [];
                        for (var x = 0; x < filter_names.length; x++) {
                            var filter_name = filter_names[x];
                            var filter_checked = view.querySelector('#media_type_filter_' + filter_name).checked;
                            if (filter_checked) {
                                filter.push(filter_name);
                            }
                        }

                        var data_t = data_type.options[data_type.selectedIndex].value;

                        var start = new Date(start_picker.value);
                        var end = new Date(end_picker.value);
                        if (end > new Date()) {
                            end = new Date();
                            end_picker.value = end.toDateInputValue();
                        }

                        days = Date.daysBetween(start, end);
                        span_days_text.innerHTML = days;

                        var filtered_url = "user_usage_stats/PlayActivity?filter=" + filter.join(",") + "&days=" + days + "&end_date=" + end_picker.value + "&data_type=" + data_t + "&stamp=" + new Date().getTime();
                        filtered_url = ApiClient.getUrl(filtered_url);
                        var user_url = "user_usage_stats/user_list?stamp=" + new Date().getTime();
                        user_url = ApiClient.getUrl(user_url);

                        load_status.innerHTML = "Loading Data...";

                        ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {
                            if (config.ColourPalette.length === 0) {
                                color_list = getDefautColours();
                            }
                            else {
                                color_list = config.ColourPalette;
                            }

                            ApiClient.getUserActivity(user_url).then(function (user_list) {
                                ApiClient.getUserActivity(filtered_url).then(function (usage_data) {
                                    load_status.innerHTML = "&nbsp;";
                                    draw_graph(view, d3, usage_data, user_list);
                                }, function (response) { load_status.innerHTML = response.status + ":" + response.statusText; });
                            });
                        });
                    }

                }, function (response) { load_status.innerHTML = response.status + ":" + response.statusText; });

            });

        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});