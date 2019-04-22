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

define(['libraryMenu', Dashboard.getConfigurationResourceUrl('helper_function.js')], function (libraryMenu) {
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

    function precisionRound(number, precision) {
        var factor = Math.pow(10, precision);
        return Math.round(number * factor) / factor;
    }

    function draw_graph(view, local_chart, usage_data) {

        console.log("data: " + usage_data);

        //console.log(usage_data);
        var chart_labels = [];
        var chart_data = [];
        for (var key in usage_data) {
            console.log(key + " " + usage_data[key]);
            var label = (key * 5) + "-" + ((key * 5) + 4);
            chart_labels.push(label);
            chart_data.push(usage_data[key]);
        }

        var barChartData = {
            labels: chart_labels, //['Mon 00', 'Mon 01', 'Mon 02', 'Mon 03', 'Mon 04', 'Mon 05', 'Mon 06'],
            datasets: [{
                label: 'Count',
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

        var chart_canvas = view.querySelector('#duration_histogram_chart_canvas');
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
                    text: "Counts per 5 minute intervals"
                },
                tooltips: {
                    mode: 'index',
                    intersect: false
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

    return function (view, params) {

        // init code here
        view.addEventListener('viewshow', function (e) {

            libraryMenu.setTabs('playback_reporting', getTabIndex("duration_histogram_report"), getTabs);

            require([Dashboard.getConfigurationResourceUrl('Chart.bundle.min.js')], function (d3) {

                // get filter types form sever
                var filter_url = ApiClient.getUrl("user_usage_stats/type_filter_list");
                console.log("loading types form : " + filter_url);
                ApiClient.getUserActivity(filter_url).then(function (filter_data) {
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

                    process_click();

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

                        var url = "user_usage_stats/DurationHistogramReport?days=" + days + "&end_date=" + end_picker.value + "&filter=" + filter.join(",") + "&stamp=" + new Date().getTime();
                        url = ApiClient.getUrl(url);
                        ApiClient.getUserActivity(url).then(function (usage_data) {
                            //alert("Loaded Data: " + JSON.stringify(usage_data));
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