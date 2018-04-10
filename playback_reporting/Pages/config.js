define([], function () {
    'use strict';

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
            Dashboard.alert({ message: "You have no usage data yet, try playing back some media and then ckeck again.", title: "No playback data!" });
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

        var user_ids = []

        // process user usage into data for chart
        var user_usage_datasets = [];
        for (var index = 0; index < usage_data.length; ++index) {
            var user_usage = usage_data[index];
            user_ids.push(user_usage.user_id);
            var point_data = [];
            for (var point_date in user_usage.user_usage) {
                var data_point = user_usage.user_usage[point_date];
                if (data_t) {
                    data_point = data_point / 60;
                }
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

        var chart_canvas = view.querySelector('#user_stats_chart_canvas');
        var ctx = chart_canvas.getContext('2d');
        var my_bar_chart = new Chart(ctx, {
            type: 'bar',
            data: userUsageChartData,//barChartData,
            options: {
                title: {
                    display: true,
                    text: 'Playback Stats'
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
                            callback: function (value, index, values) {
                                if (Math.floor(value) === value) {
                                    return value;
                                }
                            }
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
        if (all_select.checked) { filter.push("all"); }
        if (movies_select.checked) { filter.push("movies"); }
        if (series_select.checked) { filter.push("series"); }

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

    return function (view, params) {

        // init code here
        // https://cdnjs.cloudflare.com/ajax/libs/Chart.js/2.7.2/Chart.bundle.min.js
        view.addEventListener('viewshow', function (e) {

            require([Dashboard.getConfigurationResourceUrl('Chart.bundle.min.js')], function (d3) {

                var data_type = view.querySelector('#data_type');
                var all_select = view.querySelector('#media_type_all');
                var movies_select = view.querySelector('#media_type_movies');
                var series_select = view.querySelector('#media_type_series');

                var url = "/emby/user_usage_stats/30/PlayActivity?filter=all,movies,series&data_type=count&stamp=" + new Date().getTime();
                ApiClient.getUserActivity(url).then(function (usage_data) {
                    //alert("Loaded Data: " + JSON.stringify(usage_data));
                    draw_graph(view, d3, usage_data);
                });

                all_select.addEventListener("click", process_click);
                movies_select.addEventListener("click", process_click);
                series_select.addEventListener("click", process_click);
                data_type.addEventListener("change", process_click);

                function process_click() {
                    var table_body = view.querySelector('#user_usage_report_results');
                    table_body.innerHTML = "";
                    var filter = [];
                    if (all_select.checked) { filter.push("all"); }
                    if (movies_select.checked) { filter.push("movies"); }
                    if (series_select.checked) { filter.push("series"); }
                    var data_t = data_type.options[data_type.selectedIndex].value;
                    var filtered_url = "/emby/user_usage_stats/30/PlayActivity?filter=" + filter.join(",") + "&data_type=" + data_t + "&stamp=" + new Date().getTime();
                    ApiClient.getUserActivity(filtered_url).then(function (usage_data) {
                        draw_graph(view, d3, usage_data);
                    });
                }

            });

        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});