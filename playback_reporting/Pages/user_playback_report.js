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

    var my_bar_chart = null;
    var filter_names = [];

    ApiClient.getUserActivity = function (url_to_get) {
        console.log("getUserActivity Url = " + url_to_get);
        return this.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };	

    var color_list = ["#d98880", "#c39bd3", "#7fb3d5", "#76d7c4", "#7dcea0", "#f7dc6f", "#f0b27a", "#d7dbdd", "#85c1e9", "#f1948a"];

    function draw_graph(view, local_chart, usage_data) {

        if (!local_chart) {
            alert("No Chart Lib : " + local_chart);
            return;
        }

        if (!usage_data || usage_data.length == 0) {
            Dashboard.alert({ message: "You have no usage data yet, try playing back some media and then check again.", title: "No playback data!" });
            return;
        }

        //console.log("usage_data: " + JSON.stringify(usage_data));

        // get labels from the first user
        var text_labels = [];
        for (var date_string in usage_data[0].user_usage) {
            text_labels.push(date_string);
        }
        //console.log("Text Lables: " + JSON.stringify(text_labels));

        var data_type = view.querySelector('#data_type');
        var data_t = data_type.options[data_type.selectedIndex].value == "time";

        var chart_title = "";
        if (data_t) {
            chart_title = "User Playback Report (Minutes Played)";
        }
        else {
            chart_title = "User Playback Report (Play Count)";
        }

        var user_ids = []

        // process user usage into data for chart
        var user_usage_datasets = [];
        for (var index = 0; index < usage_data.length; ++index) {
            var user_usage = usage_data[index];
            user_ids.push(user_usage.user_id);
            var point_data = [];
            for (var point_date in user_usage.user_usage) {
                var data_point = user_usage.user_usage[point_date];
                //if (data_t) {
                //    data_point = data_point / 60;
                //    data_point = precisionRound(data_point, 2);
                //}
                point_data.push(data_point);
            }
            var chart_data = {
                label: user_usage.user_name,
                backgroundColor: color_list[index % 10],
                data: point_data
            };
            user_usage_datasets.push(chart_data);
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

        function tooltip_labels(tooltipItem, data) {
            var label = data.datasets[tooltipItem.datasetIndex].label || '';

            if (label) {
                if (data_t) {
                    label += ": " + seconds2time(tooltipItem.yLabel);
                }
                else {
                    label += ": " + tooltipItem.yLabel;
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
                title: {
                    display: true,
                    text: chart_title
                },
                tooltips: {
                    mode: 'index',
                    intersect: false
                },
                responsive: true,
                scales: {
                    xAxes: [{
                        stacked: true,
                    }],
                    yAxes: [{
                        stacked: true,
                        ticks: {
                            autoSkip: true,
                            beginAtZero: true,
                            callback: y_axis_labels
                        }
                    }]
                },
                onClick: function (e) {
                    var activePoint = my_bar_chart.getElementAtEvent(e)[0];
                    if (!activePoint) {
                        return;
                    }
                    var data = activePoint._chart.data;
                    var datasetIndex = activePoint._datasetIndex;
                    var label = data.datasets[datasetIndex].label;
                    var data_label = data.labels[activePoint._index];
                    var value = data.datasets[datasetIndex].data[activePoint._index];
                    var user_id = data.user_id_list[datasetIndex];
                    console.log(label, user_id, data_label, value);

                    display_user_report(label, user_id, data_label, view);
                    //var href = Dashboard.getConfigurationPageUrl("UserUsageReport") + "&user=" + user_id + "&date=" + data_label;
                    //Dashboard.navigate(href);
                },
                tooltips: {
                    callbacks: {
                        label: tooltip_labels
                    }
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
                filter.push(filter_name)
            }
        }

        var url_to_get = "/emby/user_usage_stats/" + user_id + "/" + data_label + "/GetItems?filter=" + filter.join(",") + "&stamp=" + new Date().getTime();
        console.log("User Report Details Url: " + url_to_get);

        ApiClient.getUserActivity(url_to_get).then(function (usage_data) {
            //alert("Loaded Data: " + JSON.stringify(usage_data));
            populate_report(user_name, data_label, usage_data, view);
        });

    }

    function populate_report(user_name, data_label, usage_data, view) {

        if (!usage_data) {
            alert("No Data!");
            return;
        }

        console.log("Processing User Report: " + JSON.stringify(usage_data));

        var user_name_span = view.querySelector('#user_report_user_name');
        user_name_span.innerHTML = user_name;

        var user_report_on_date = view.querySelector('#user_report_on_date');
        user_report_on_date.innerHTML = "(" + data_label + ")";

        var table_body = view.querySelector('#user_usage_report_results');
        var row_html = "";

        for (var index = 0; index < usage_data.length; ++index) {
            var item_details = usage_data[index];
            row_html += "<tr class='detailTableBodyRow detailTableBodyRow-shaded'>";

            row_html += "<td>" + item_details.Time + "</td>";
            row_html += "<td>" + item_details.Name + "</td>";
            row_html += "<td>" + item_details.Type + "</td>";
            row_html += "<td>" + item_details.Client + "</td>";
            row_html += "<td>" + item_details.Device + "</td>";
            row_html += "<td>" + item_details.Method + "</td>";
            row_html += "<td>" + seconds2time(item_details.Duration) + "</td>";
            row_html += "</tr>";
        }
        table_body.innerHTML = row_html;
    }

    function seconds2time(seconds) {
        var h = Math.floor(seconds / 3600);
        seconds = seconds - (h * 3600);
        var m = Math.floor(seconds / 60);
        var s = seconds - (m * 60);
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

    function getTabs() {
        var tabs = [
            {
                href: Dashboard.getConfigurationPageUrl('user_playback_report'),
                name: 'Playback Report'
            },
            {
                href: Dashboard.getConfigurationPageUrl('hourly_usage_report'),
                name: 'Hourly Usage'
            },
            {
                href: Dashboard.getConfigurationPageUrl('breakdown_report'),
                name: 'Breakdown Report'
            },
            {
                href: Dashboard.getConfigurationPageUrl('duration_histogram_report'),
                name: 'Duration Histogram'
            },
            {
                href: Dashboard.getConfigurationPageUrl('playback_report_settings'),
                name: 'Settings'
            }];
        return tabs;
    }

    return function (view, params) {

        // init code here
        // https://cdnjs.cloudflare.com/ajax/libs/Chart.js/2.7.2/Chart.bundle.min.js
        view.addEventListener('viewshow', function (e) {

            libraryMenu.setTabs('playback_reporting', 0, getTabs);

            require([Dashboard.getConfigurationResourceUrl('Chart.bundle.min.js')], function (d3) {

                

                // get filter types form sever
                var filter_url = "/emby/user_usage_stats/type_filter_list";
                ApiClient.getUserActivity(filter_url).then(function (filter_data) {
                    filter_names = filter_data;
                
                    // build filter list
                    var filter_items = "";
                    for (var x = 0; x < filter_names.length; x++) {
                        var filter_name = filter_names[x];
                        filter_items += "<input type='checkbox' id='media_type_filter_" + filter_name + "' data_fileter_name='" + filter_name + "' checked> " + filter_name + " ";
                    }

                    var filter_check_list = view.querySelector('#filter_check_list');
                    filter_check_list.innerHTML = filter_items;

                    for (var x = 0; x < filter_names.length; x++) {
                        var filter_name = filter_names[x];
                        view.querySelector('#media_type_filter_' + filter_name).addEventListener("click", process_click);
                    }

                    var data_type = view.querySelector('#data_type');
                    data_type.addEventListener("change", process_click);

                    var url = "/emby/user_usage_stats/28/PlayActivity?filter=" + filter_names.join(",") + "&data_type=count&stamp=" + new Date().getTime();
                    ApiClient.getUserActivity(url).then(function (usage_data) {
                        //alert("Loaded Data: " + JSON.stringify(usage_data));
                        draw_graph(view, d3, usage_data);
                    });

                    function process_click() {
                        var table_body = view.querySelector('#user_usage_report_results');
                        table_body.innerHTML = "";

                        var filter = [];
                        for (var x = 0; x < filter_names.length; x++) {
                            var filter_name = filter_names[x];
                            var filter_checked = view.querySelector('#media_type_filter_' + filter_name).checked;
                            if (filter_checked) {
                                filter.push(filter_name)
                            }
                        }

                        var data_t = data_type.options[data_type.selectedIndex].value;

                        var filtered_url = "/emby/user_usage_stats/28/PlayActivity?filter=" + filter.join(",") + "&data_type=" + data_t + "&stamp=" + new Date().getTime();
                        ApiClient.getUserActivity(filtered_url).then(function (usage_data) {
                            draw_graph(view, d3, usage_data);
                        });
                    }

                });

            });

        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});