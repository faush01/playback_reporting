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

    var resource_chart = null;

    ApiClient.getServerData = function (url_to_get) {
        console.log("getUserActivity Url = " + url_to_get);
        return this.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };

    var color_list = ["#d98880", "#c39bd3", "#7fb3d5", "#76d7c4", "#7dcea0", "#f7dc6f", "#f0b27a", "#d7dbdd", "#85c1e9", "#f1948a"];

    function draw_graph(view, local_chart, resource_data) {

        console.log("draw_graph called");
        //console.log("resource_data: " + JSON.stringify(resource_data));

        var chart_title = "Resource Usage";

        var chart_canvas = view.querySelector('#resource_usage_chart_canvas');
        var ctx = chart_canvas.getContext('2d');

        if (resource_chart) {
            console.log("destroy() existing chart");
            resource_chart.destroy();
        }

        /*
        var server_load_demo = [{
            x: "2019/05/01 05:35:45",
            y: 7.5
        }, {
            x: "2019/05/01 05:36:34",
            y: 9.9
        }, {
            x: "2019/05/01 05:37:40",
            y: 21
        }, {
            x: "2019/05/01 05:38:12",
            y: 15
        }];
        */

        var server_load_data = [];
        var server_mem_data = [];
        for (var index = 0; index < resource_data.length; ++index) {
            var resource_counter = resource_data[index];
            server_load_data.push({ x: resource_counter.date, y: resource_counter.cpu });
            var mem_value = Math.round(resource_counter.mem / (1024 * 1024));
            server_mem_data.push({ x: resource_counter.date, y: mem_value });
        }
        console.log("resource_data_len: " + index);

        var timeFormat = 'YYYY/MM/DD HH:mm:ss';

        var chart_config = {
            type: 'line',
            data: {
                datasets: [{
                    label: 'CPU Load',
                    backgroundColor: '#FF0000',
                    borderColor: '#666666',
                    fill: false,
                    data: server_load_data,
                    yAxisID: "y-axis-1"
                },
                {
                    label: 'MEM Used (MB)',
                    backgroundColor: '#0000FF',
                    borderColor: '#666666',
                    fill: false,
                    data: server_mem_data,
                    yAxisID: "y-axis-2"
                }]
            },
            options: {
                title: {
                    text: chart_title
                },
                scales: {
                    xAxes: [{
                        type: 'time',
                        time: {
                            parser: timeFormat,
                            // round: 'day'
                            tooltipFormat: 'll HH:mm'
                        },
                        scaleLabel: {
                            display: true,
                            labelString: 'Date'
                        }
                    }],
                    yAxes: [{
                        scaleLabel: {
                            display: true,
                            labelString: 'CPU'
                        },
                        beginAtZero: true,
                        type: 'linear',
                        display: true,
                        position: 'left',
                        id: 'y-axis-1'
                    },
                    {
                        scaleLabel: {
                            display: true,
                            labelString: 'MEM MB'
                        },
                        beginAtZero: true,
                        type: 'linear',
                        display: true,
                        position: 'right',
                        id: 'y-axis-2'
                    }]
                }
            }
        };

        resource_chart = new Chart(ctx, chart_config);

        console.log("Chart Done");
    }


    return function (view, params) {

        // init code here
        // https://cdnjs.cloudflare.com/ajax/libs/Chart.js/2.7.2/Chart.bundle.min.js
        view.addEventListener('viewshow', function (e) {

            libraryMenu.setTabs('playback_reporting', getTabIndex("resource_usage"), getTabs);

            require([Dashboard.getConfigurationResourceUrl('Chart.bundle.min.js')], function (d3) {

                var hours_selection = view.querySelector('#requested_number_hours');
                hours_selection.addEventListener("change", process_click);

                process_click();

                function process_click() {

                    var hours_value = hours_selection.options[hours_selection.selectedIndex].value;
                    var resource_url = "user_usage_stats/resource_usage?hours=" + hours_value + "&stamp=" + new Date().getTime();
                    resource_url = ApiClient.getUrl(resource_url);

                    ApiClient.getServerData(resource_url).then(function (result_data) {

                        draw_graph(view, d3, result_data);

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