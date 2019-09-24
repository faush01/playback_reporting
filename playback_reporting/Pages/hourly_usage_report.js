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

    var daily_bar_chart = null;
    var hourly_bar_chart = null;
    var weekly_bar_chart = null;
    var filter_names = [];

    ApiClient.getUserActivity = function (url_to_get) {
        console.log("getUserActivity Url = " + url_to_get);
        return this.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };	

    function precisionRound(number, precision) {
        var factor = Math.pow(10, precision);
        return Math.round(number * factor) / factor;
    }

    function draw_graph(view, local_chart, usage_data) {

        var days_of_week = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];

        //console.log(usage_data);
        var chart_labels = [];
        var chart_data = [];
        var aggregated_hours = {};
        var aggregated_days = {};
        for (var key in usage_data) {
            //console.log(key + " " + usage_data[key]);
            var day_index = key.substring(0, 1);
            var day_name = days_of_week[day_index];
            var day_hour = key.substring(2);
            //chart_labels.push(day_name + " " + day_hour + ":00");
            chart_labels.push(day_name + " " + day_hour);
            chart_data.push(usage_data[key]);//precisionRound(usage_data[key] / 60, 2));
            var current_hour_value = 0;
            if (aggregated_hours[day_hour]) {
                current_hour_value = aggregated_hours[day_hour];
            }
            aggregated_hours[day_hour] = current_hour_value + usage_data[key];
            var current_day_value = 0;
            if (aggregated_days[day_index]) {
                current_day_value = aggregated_days[day_index];
            }
            aggregated_days[day_index] = current_day_value + usage_data[key];
        }
        //chart_labels.push("00");

        //console.log(JSON.stringify(aggregated_hours));
        //console.log(JSON.stringify(aggregated_days));

        //
        // daily bar chart data
        //
        var daily_chart_label_data = [];
        var daily_chart_point_data = [];
        var daily_days_labels = Object.keys(aggregated_days);
        daily_days_labels.sort();
        for (var daily_key_index = 0; daily_key_index < daily_days_labels.length; daily_key_index++) {
            var daily_key = daily_days_labels[daily_key_index];
            daily_chart_label_data.push(days_of_week[daily_key]);
            daily_chart_point_data.push(aggregated_days[daily_key]);
        }

        var daily_chart_data = {
            labels: daily_chart_label_data,
            datasets: [{
                label: 'Time',
                type: "bar",
                backgroundColor: '#d98880',
                data: daily_chart_point_data
            }]
        };

        //
        // hourly chart data
        //
        var hourly_chart_label_data = [];
        var hourly_chart_point_data = [];
        var hourly_days_labels = Object.keys(aggregated_hours);
        hourly_days_labels.sort();
        for (var hourly_key_index = 0; hourly_key_index < hourly_days_labels.length; hourly_key_index++) {
            var hourly_key = hourly_days_labels[hourly_key_index];
            hourly_chart_label_data.push(hourly_key);
            hourly_chart_point_data.push(aggregated_hours[hourly_key]);
        }

        var hourly_chart_data = {
            labels: hourly_chart_label_data,
            datasets: [{
                label: 'Time',
                type: "bar",
                backgroundColor: '#d98880',
                data: hourly_chart_point_data
            }]
        };

        //
        // weekly bar chart data
        //
        var weekly_chart_data = {
            labels: chart_labels, //['Mon 00', 'Mon 01', 'Mon 02', 'Mon 03', 'Mon 04', 'Mon 05', 'Mon 06'],
            datasets: [{
                label: 'Time',
                type: "bar",
                backgroundColor: '#d98880',
                data: chart_data // [10,20,30,40,50,60,70]
            }/*,
            {
                label: "Minutes",
                type: "line",
                lineTension: 0,
                borderColor: "#8e5ea2",
                data: chart_data, // [10,20,30,40,50,60,70],
                fill: false
            }*/]
        };

        function y_axis_labels(value, index, values) {
            if (Math.floor(value / 10) === (value / 10)) {
                return seconds2time(value);
            }
        }

        function tooltip_labels(tooltipItem, data) {
            var label = data.datasets[tooltipItem.datasetIndex].label || '';

            if (label) {
                label += ": " + seconds2time(tooltipItem.yLabel);
            }

            return label;
        }

        //
        // daily chart
        //
        var daily_chart_canvas = view.querySelector('#daily_usage_chart_canvas');
        var ctx_daily = daily_chart_canvas.getContext('2d');

        if (daily_bar_chart) {
            console.log("destroy() existing chart: daily_bar_chart");
            daily_bar_chart.destroy();
        }

        daily_bar_chart = new Chart(ctx_daily, {
            type: 'bar',
            data: daily_chart_data,
            options: {
                legend: {
                    display: false
                },
                title: {
                    display: true,
                    text: "Usage by Day"
                },
                responsive: true,
                scales: {
                    xAxes: [{
                        stacked: false
                    }],
                    yAxes: [{
                        stacked: false,
                        ticks: {
                            autoSkip: true,
                            beginAtZero: true,
                            callback: y_axis_labels
                        }
                    }]
                },
                tooltips: {
                    mode: 'index',
                    intersect: false,
                    callbacks: {
                        label: tooltip_labels
                    }
                }
            }
        });

        //
        // hourly chart
        //
        var hourly_chart_canvas = view.querySelector('#hourly_usage_chart_canvas');
        var ctx_hourly = hourly_chart_canvas.getContext('2d');

        if (hourly_bar_chart) {
            console.log("destroy() existing chart: hourly_bar_chart");
            hourly_bar_chart.destroy();
        }

        hourly_bar_chart = new Chart(ctx_hourly, {
            type: 'bar',
            data: hourly_chart_data,
            options: {
                legend: {
                    display: false
                },
                title: {
                    display: true,
                    text: "Usage by Hour"
                },
                responsive: true,
                scales: {
                    xAxes: [{
                        stacked: false
                    }],
                    yAxes: [{
                        stacked: false,
                        ticks: {
                            autoSkip: true,
                            beginAtZero: true,
                            callback: y_axis_labels
                        }
                    }]
                },
                tooltips: {
                    mode: 'index',
                    intersect: false,
                    callbacks: {
                        label: tooltip_labels
                    }
                }
            }
        });

        //
        // weekly chart
        //
        var chart_canvas = view.querySelector('#weekly_usage_chart_canvas');
        var ctx_weekly = chart_canvas.getContext('2d');

        if (weekly_bar_chart) {
            console.log("destroy() existing chart: weekly_bar_chart");
            weekly_bar_chart.destroy();
        }

        weekly_bar_chart = new Chart(ctx_weekly, {
            type: 'bar',
            data: weekly_chart_data,
            options: {
                legend: {
                    display: false
                },
                title: {
                    display: true,
                    text: "Usage by Week"
                },
                responsive: true,
                scaleShowValues: true,
                scales: {
                    xAxes: [{
                        stacked: false,
                        ticks: {
                            //autoSkip: false
                        }
                    }],
                    yAxes: [{
                        stacked: false,
                        ticks: {
                            autoSkip: true,
                            beginAtZero: true,
                            callback: y_axis_labels
                        }
                    }]
                },
                tooltips: {
                    mode: 'index',
                    intersect: false,
                    callbacks: {
                        label: tooltip_labels
                    }
                }
            }
        });

        console.log("Charts Done");
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

            mainTabsManager.setTabs(this, getTabIndex("hourly_usage_report"), getTabs);

            require([Dashboard.getConfigurationResourceUrl('Chart.bundle.min.js')], function (d3) {

                var user_name = "";
                var user_name_index = window.location.href.indexOf("user=");
                if (user_name_index > -1) {
                    user_name = window.location.href.substring(user_name_index + 5);
                }

                var filter_url = ApiClient.getUrl("user_usage_stats/type_filter_list");
                console.log("loading types form : " + filter_url);

                var load_status = view.querySelector('#usage_duration_report_status');
                load_status.innerHTML = "Loading Data...";

                ApiClient.getUserActivity(filter_url).then(function (filter_data) {
                    load_status.innerHTML = "&nbsp;";
                    filter_names = filter_data;

                    // build filter list
                    var filter_items = "";
                    for (var x1 = 0; x1 < filter_names.length; x1++) {
                        var filter_name_01 = filter_names[x1];
                        filter_items += "<input type='checkbox' id='media_type_filter_" + filter_name_01 + "' data_fileter_name='" + filter_name_01 + "' checked> " + filter_name_01 + " ";
                    }

                    var filter_check_list = view.querySelector('#filter_check_list');
                    filter_check_list.innerHTML = filter_items;

                    for (var x2 = 0; x2 < filter_names.length; x2++) {
                        var filter_name_02 = filter_names[x2];
                        view.querySelector('#media_type_filter_' + filter_name_02).addEventListener("click", process_click);
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

                    // add user list to selector
                    var user_url = "user_usage_stats/user_list?stamp=" + new Date().getTime();
                    user_url = ApiClient.getUrl(user_url);

                    ApiClient.getUserActivity(user_url).then(function (user_list) {
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
                        var filter = [];
                        for (var x3 = 0; x3 < filter_names.length; x3++) {
                            var filter_name = filter_names[x3];
                            var filter_checked = view.querySelector('#media_type_filter_' + filter_name).checked;
                            if (filter_checked) {
                                filter.push(filter_name);
                            }
                        }

                        var start = new Date(start_picker.value);
                        var end = new Date(end_picker.value);
                        if (end > new Date()) {
                            end = new Date();
                            end_picker.value = end.toDateInputValue();
                        }

                        var days = Date.daysBetween(start, end);
                        span_days_text.innerHTML = days;
                        var selected_user_id = user_list_selector.options[user_list_selector.selectedIndex].value;

                        var url = "user_usage_stats/HourlyReport?user_id=" + selected_user_id + "&days=" + days + "&end_date=" + end_picker.value + "&filter=" + filter.join(",") + "&stamp=" + new Date().getTime();
                        url = ApiClient.getUrl(url);

                        load_status.innerHTML = "Loading Data...";

                        ApiClient.getUserActivity(url).then(function (usage_data) {
                            load_status.innerHTML = "&nbsp;";
                            //alert("Loaded Data: " + JSON.stringify(usage_data));
                            draw_graph(view, d3, usage_data);
                        }, function (response) { load_status.innerHTML = response.status + ":" + response.statusText; });
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