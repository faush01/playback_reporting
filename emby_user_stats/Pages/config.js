define([], function () {
    'use strict';

    ApiClient.getAllUserActivity = function () {
        return this.ajax({
            type: "GET",
            url: "/emby/user_usage_stats/30/PlayActivity",
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
            alert("No Data : " + usage_data);
            return;
        }

        //console.log("usage_data: " + JSON.stringify(usage_data));

        // get labels from the first user
        var text_labels = [];
        for (var date_string in usage_data[0].user_usage) {
            text_labels.push(date_string);
        }
        //console.log("Text Lables: " + JSON.stringify(text_labels));

        // process user usage into data for chart
        var user_usage_datasets = [];
        for (var index = 0; index < usage_data.length; ++index) {
            var user_usage = usage_data[index];
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
        window.myBar = new Chart(ctx, {
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
                        stacked: true
                    }]
                }
            }
        });

        console.log("Chart Done");

    }

    return function (view, params) {

        // init code here

        view.addEventListener('viewshow', function (e) {

            require(['https://cdnjs.cloudflare.com/ajax/libs/Chart.js/2.7.2/Chart.bundle.min.js'], function (d3) {

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