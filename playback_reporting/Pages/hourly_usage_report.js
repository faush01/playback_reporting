define(['libraryMenu'], function (libraryMenu) {
    'use strict';

    var my_bar_chart = null;

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
        for (var key in usage_data) {
            //console.log(key + " " + usage_data[key]);
            var day_index = key.substring(0, 1);
            var day_name = days_of_week[day_index];
            var day_hour = key.substring(2);
            chart_labels.push(day_name + " " + day_hour);
            chart_data.push(precisionRound(usage_data[key] / 60, 2));
        }
        chart_labels.push("00");

        var barChartData = {
            labels: chart_labels, //['Mon 00', 'Mon 01', 'Mon 02', 'Mon 03', 'Mon 04', 'Mon 05', 'Mon 06'],
            datasets: [{
                label: 'Minutes',
                type: "bar",
                backgroundColor: '#c39bd3',
                data: chart_data, // [10,20,30,40,50,60,70]
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

        var chart_canvas = view.querySelector('#hourly_usage_chart_canvas');
        var ctx = chart_canvas.getContext('2d');

        if (my_bar_chart) {
            console.log("destroy() existing chart");
            my_bar_chart.destroy();
        }

        my_bar_chart = new Chart(ctx, {
            type: 'bar',
            data: barChartData,
            options: {
                legend: {
                    display: false
                },
                title: {
                    display: true,
                    text: "Usage by Hour"
                },
                tooltips: {
                    mode: 'index',
                    intersect: false
                },
                responsive: true,
                scales: {
                    xAxes: [{
                        stacked: false,
                    }],
                    yAxes: [{
                        stacked: false,
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
                }
            }
        });

        console.log("Chart Done");
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

            libraryMenu.setTabs('playback_reporting', 1, getTabs);

            require([Dashboard.getConfigurationResourceUrl('Chart.bundle.min.js')], function (d3) {
                
                var url = "/emby/user_usage_stats/90/HourlyReport?stamp=" + new Date().getTime();
                ApiClient.getUserActivity(url).then(function (usage_data) {
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