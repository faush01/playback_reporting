define([], function () {
    'use strict';

    ApiClient.getUserActivity = function (url_to_get) {
        return this.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };	

    ApiClient.getAllUserActivity = function () {
        return this.ajax({
            type: "GET",
            url: "/emby/user_usage_stats/30/PlayActivity?stamp=" + new Date().getTime(),
            dataType: "json"
        });
    };

    var color_list = ["#d98880", "#c39bd3", "#7fb3d5", "#76d7c4", "#7dcea0", "#f7dc6f", "#f0b27a", "#d7dbdd", "#85c1e9", "#f1948a"];

    function draw_graph(context, local_chart, usage_data) {

        if (!local_chart) {
            alert("No Chart Lib : " + local_chart);
            return;
        }

        if (!usage_data || usage_data.length == 0) {
            Dashboard.alert({ message: "You have no user usage data yet, try playing back some media and then ckeck again.", title: "No usage data!" });
            return;
        }

        //console.log("usage_data: " + JSON.stringify(usage_data));

        // get labels from the first user
        var text_labels = [];
        for (var date_string in usage_data[0].user_usage) {
            text_labels.push(date_string);
        }
        //console.log("Text Lables: " + JSON.stringify(text_labels));

        var user_ids = []

        // process user usage into data for chart
        var user_usage_datasets = [];
        for (var index = 0; index < usage_data.length; ++index) {
            var user_usage = usage_data[index];
            user_ids.push(user_usage.user_id);
            var point_data = [];
            for (var point_date in user_usage.user_usage) {
                point_data.push(user_usage.user_usage[point_date]);
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

        var ctx = document.getElementById('user_stats_chart_canvas').getContext('2d');
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
                            beginAtZero: true,
                            stepSize: 1
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

                    display_user_report(label, user_id, data_label);
                    //var href = Dashboard.getConfigurationPageUrl("UserUsageReport") + "&user=" + user_id + "&date=" + data_label;
                    //Dashboard.navigate(href);
                }
            }
        });

        console.log("Chart Done");

    }

    function display_user_report(user_name, user_id, data_label) {
        console.log("Building User Report");

        var url_to_get = "/emby/user_usage_stats/" + user_id + "/" + data_label + "/GetItems?stamp=" + new Date().getTime();
        console.log("User Report Details Url: " + url_to_get);

        ApiClient.getUserActivity(url_to_get).then(function (usage_data) {
            //alert("Loaded Data: " + JSON.stringify(usage_data));
            populate_report(user_name, data_label, usage_data);
        });

    }

    function populate_report(user_name, data_label, usage_data) {

        if (!usage_data) {
            alert("No Data!");
            return;
        }

        console.log("Processing User Report: " + JSON.stringify(usage_data));

        var user_name_span = document.getElementById("user_report_user_name");
        user_name_span.innerHTML = user_name;

        var user_report_on_date = document.getElementById("user_report_on_date");
        user_report_on_date.innerHTML = data_label;

        var table_body = document.getElementById("user_usage_report_results");
        var row_html = "";

        for (var index = 0; index < usage_data.length; ++index) {
            var item_details = usage_data[index];
            row_html += "<tr class='detailTableBodyRow detailTableBodyRow-shaded'>";

            row_html += "<td>" + item_details.Time + "</td>";

            if (item_details.Id) {
                row_html += "<td><a href='itemdetails.html?id=" + item_details.Id + "'>";
                row_html += item_details.Name;
                row_html += "</a></td>";
            }
            else {
                row_html += "<td>" + item_details.Name + "</td>";
            }
            row_html += "<td>" + item_details.Type + "</td>";

            row_html += "</tr>";
        }
        table_body.innerHTML = row_html;

    }

    return function (view, params) {

        // init code here
        // https://cdnjs.cloudflare.com/ajax/libs/Chart.js/2.7.2/Chart.bundle.min.js
        view.addEventListener('viewshow', function (e) {

            require([Dashboard.getConfigurationResourceUrl('Chart.bundle.min.js')], function (d3) {

                ApiClient.getAllUserActivity().then(function (usage_data) {
                    //alert("Loaded Data: " + JSON.stringify(usage_data));
                    draw_graph(view, d3, usage_data);
                });

            });

        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});