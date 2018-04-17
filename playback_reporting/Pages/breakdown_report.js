define(['libraryMenu'], function (libraryMenu) {
    'use strict';

    var chart_instance_map = {};

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

    function draw_chart_user_count(view, local_chart, data, group_type) {

        var chart_data_labels_count = [];
        var chart_data_values_count = [];

        var chart_data_labels_time = [];
        var chart_data_values_time = [];

        data.sort(function (a, b) {
            return ((a["count"] < b["count"]) ? -1 : ((a["count"] == b["count"]) ? 0 : 1));
        });

        for (var index in data) {
            chart_data_labels_count.push(data[index]["label"]);
            chart_data_values_count.push(data[index]["count"]);
        }

        data.sort(function (a, b) {
            return ((a["time"] < b["time"]) ? -1 : ((a["time"] == b["time"]) ? 0 : 1));
        });

        for (var index in data) {
            chart_data_labels_time.push(data[index]["label"]);
            chart_data_values_time.push(data[index]["time"]);
        }

        //console.log(chart_data_labels_count);
        //console.log(chart_data_values_count);
        //console.log(chart_data_labels_time);
        //console.log(chart_data_values_time);

        var chart_data_user_count = {
            labels: chart_data_labels_count,
            datasets: [{
                label: "Breakdown",
                backgroundColor: ["#d98880", "#c39bd3", "#7fb3d5", "#76d7c4", "#7dcea0", "#f7dc6f", "#f0b27a", "#d7dbdd", "#85c1e9", "#f1948a"],
                data: chart_data_values_count,
            }]
        };

        var chart_data_user_time = {
            labels: chart_data_labels_time,
            datasets: [{
                label: "Breakdown",
                backgroundColor: ["#d98880", "#c39bd3", "#7fb3d5", "#76d7c4", "#7dcea0", "#f7dc6f", "#f0b27a", "#d7dbdd", "#85c1e9", "#f1948a"],
                data: chart_data_values_time,
            }]
        };

        var chart_canvas_count = view.querySelector('#' + group_type + '_breakdown_count_chart_canvas');
        var cxt_count = chart_canvas_count.getContext('2d');
        if (chart_instance_map[group_type+"_count"]) {
            console.log("destroy() existing chart");
            chart_instance_map[group_type + "_count"].destroy();
        }
        chart_instance_map[group_type + "_count"] = new Chart(cxt_count, {
            type: 'pie',
            data: chart_data_user_count,
            options: {
                title: {
                    display: true,
                    text: group_type + ": PlayCount"
                }
            }
        });

        chart_canvas_count.addEventListener("click", function () {
            var chart = chart_instance_map[group_type + "_count"];
            if (chart) {
                chart.options.legend.display = !chart.options.legend.display;
                chart.update();
            }
        });

        if (chart_instance_map[group_type + "_time"]) {
            console.log("destroy() existing chart");
            chart_instance_map[group_type + "_time"].destroy();
        }

        var chart_canvas_time = view.querySelector('#' + group_type + '_breakdown_time_chart_canvas');
        var cxt_time = chart_canvas_time.getContext('2d');
        chart_instance_map[group_type + "_time"] = new Chart(cxt_time, {
            type: 'pie',
            data: chart_data_user_time,
            options: {
                title: {
                    display: true,
                    text: group_type + ": Time"
                }
            }
        });

        chart_canvas_time.addEventListener("click", function () {
            var chart = chart_instance_map[group_type + "_time"];
            if (chart) {
                chart.options.legend.display = !chart.options.legend.display;
                chart.update();
            }
        });

        console.log("Charts Done");
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
                href: Dashboard.getConfigurationPageUrl('playback_report_settings'),
                name: 'Settings'
            }];
        return tabs;
    }

    return function (view, params) {

        // init code here
        view.addEventListener('viewshow', function (e) {

            libraryMenu.setTabs('playback_reporting', 2, getTabs);

            require([Dashboard.getConfigurationResourceUrl('Chart.bundle.min.js')], function (d3) {

                // build user chart
                var url = "/emby/user_usage_stats/90/UserId/BreakdownReport?stamp=" + new Date().getTime();
                ApiClient.getUserActivity(url).then(function (data) {
                    //alert("Loaded Data: " + JSON.stringify(usage_data));
                    draw_chart_user_count(view, d3, data, "User");
                });

                // build ItemType chart
                var url = "/emby/user_usage_stats/90/ItemType/BreakdownReport?stamp=" + new Date().getTime();
                ApiClient.getUserActivity(url).then(function (data) {
                    //alert("Loaded Data: " + JSON.stringify(usage_data));
                    draw_chart_user_count(view, d3, data, "ItemType");
                });

                // build PlaybackMethod chart
                var url = "/emby/user_usage_stats/90/PlaybackMethod/BreakdownReport?stamp=" + new Date().getTime();
                ApiClient.getUserActivity(url).then(function (data) {
                    //alert("Loaded Data: " + JSON.stringify(usage_data));
                    draw_chart_user_count(view, d3, data, "PlayMethod");
                });

                // build ClientName chart
                var url = "/emby/user_usage_stats/90/ClientName/BreakdownReport?stamp=" + new Date().getTime();
                ApiClient.getUserActivity(url).then(function (data) {
                    //alert("Loaded Data: " + JSON.stringify(usage_data));
                    draw_chart_user_count(view, d3, data, "ClientName");
                });

                // build DeviceName chart
                var url = "/emby/user_usage_stats/90/DeviceName/BreakdownReport?stamp=" + new Date().getTime();
                ApiClient.getUserActivity(url).then(function (data) {
                    //alert("Loaded Data: " + JSON.stringify(usage_data));
                    draw_chart_user_count(view, d3, data, "DeviceName");
                });

                //var toggle = view.querySelector('#toggle_ledgend_user_count');
                //var toggle = view.querySelector('#User_breakdown_count_chart_canvas');
                //toggle.addEventListener("click", function () {
                //    console.log("toggle_ledgend_user_count clicked");
                //    var chart = chart_instance_map["User_count"];
                //    console.log("User_count " + chart);
                //    if (chart) {
                //        chart.options.legend.display = !chart.options.legend.display;
                //        chart.update();
                //    }
                //});
                


            });

        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});