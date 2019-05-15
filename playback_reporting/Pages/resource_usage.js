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
        console.log("getServerData Url = " + url_to_get);
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

        var counter_data = resource_data.counters;
        var server_load_data = [];
        var server_mem_data = [];
        for (var index = 0; index < counter_data.length; ++index) {
            var resource_counter = counter_data[index];

            server_load_data.push({ x: resource_counter.date, y: resource_counter.cpu });

            var mem_value = Math.round(resource_counter.mem / (1024 * 1024));
            server_mem_data.push({ x: resource_counter.date, y: mem_value });
        }
        console.log("counter_data_len: " + index);

        var play_counts = [];
        var play_counts_data = resource_data.play_counts;
        var lastr_count = 0;
        for (var index2 = 0; index2 < play_counts_data.length; ++index2) {
            var play_counter = play_counts_data[index2];

            play_counts.push({ x: play_counter.Key, y: lastr_count });
            play_counts.push({ x: play_counter.Key, y: play_counter.Value });

            lastr_count = play_counter.Value;
        }


        var timeFormat = 'YYYY/MM/DD HH:mm:ss';

        var chart_config = {
            type: 'line',
            data: {
                datasets: [{
                    label: 'CPU Load',
                    backgroundColor: color_list[0],
                    borderColor: color_list[0],
                    fill: false,
                    data: server_load_data,
                    yAxisID: "y-axis-1"
                },
                {
                    label: 'MEM Used (MB)',
                    backgroundColor: color_list[1],
                    borderColor: color_list[1],
                    fill: false,
                    data: server_mem_data,
                    yAxisID: "y-axis-2"
                },
                {
                    label: 'Playback Count',
                    backgroundColor: color_list[2],
                    borderColor: color_list[2],
                    fill: false,
                    data: play_counts,
                    lineTension: 0,
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
                            display: false,
                            labelString: 'value'
                        },
                        beginAtZero: true,
                        type: 'linear',
                        display: true,
                        position: 'left',
                        id: 'y-axis-1'
                    },
                    {
                        scaleLabel: {
                            display: false,
                            labelString: 'value'
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

    function formatBytes(bytes, decimals) {
        if (bytes === 0) return '0 B';

        const k = 1024;
        const dm = decimals < 0 ? 0 : decimals;
        const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];

        const i = Math.floor(Math.log(bytes) / Math.log(k));

        return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
    }

    function show_process_list(view, process_list_data) {

        //console.log("process_list_data: " + JSON.stringify(process_list_data));

        var sort_order_selection = view.querySelector('#process_list_sort_order');
        var sort_order = sort_order_selection.options[sort_order_selection.selectedIndex].value;
        
        if (sort_order === "id") {
            process_list_data.sort(function (a, b) { return a.id > b.id ? 1 : -1; });
        }
        else if (sort_order === "name") {
            process_list_data.sort(function (a, b) { return a.name > b.name ? 1 : -1; });
        }
        else if (sort_order === "mem") {
            process_list_data.sort(function (a, b) { return a.mem < b.mem ? 1 : -1; });
        }
        else {
            process_list_data.sort(function (a, b) { return a.cpu < b.cpu ? 1 : -1; });
        }
        
        var table_body = view.querySelector('#process_list_results');

        var table_rows = "";

        process_list_data.forEach(function (item_details, index) {
            table_rows += "<tr class='detailTableBodyRow detailTableBodyRow-shaded'>";
            table_rows += "<td style='padding-right: 20px'>" + item_details.id + "</td>";
            table_rows += "<td style='padding-right: 20px'>" + item_details.name + "</td>";
            table_rows += "<td style='padding-right: 20px'>" + item_details.cpu + "</td>";
            table_rows += "<td style='padding-right: 20px'>" + formatBytes(item_details.mem, 2) + "</td>";
            table_rows += "<td style='padding-right: 20px'>" + item_details.error + "</td>";
            table_rows += "</tr>";
        });

        table_body.innerHTML = table_rows;
    }

    return function (view, params) {

        // init code here
        // https://cdnjs.cloudflare.com/ajax/libs/Chart.js/2.7.2/Chart.bundle.min.js
        view.addEventListener('viewshow', function (e) {
            libraryMenu.setTabs('playback_reporting', getTabIndex("resource_usage"), getTabs);

            require([Dashboard.getConfigurationResourceUrl('Chart.bundle.min.js')], function (d3) {

                var hours_selection = view.querySelector('#requested_number_hours');
                hours_selection.addEventListener("change", process_click);

                var chart_refresh_button = view.querySelector('#resource_chart_refresh');
                chart_refresh_button.addEventListener("click", process_click);
                
                process_click();

                function process_click() {

                    var hours_value = hours_selection.options[hours_selection.selectedIndex].value;
                    var resource_url = "user_usage_stats/resource_usage?hours=" + hours_value + "&stamp=" + new Date().getTime();
                    resource_url = ApiClient.getUrl(resource_url);

                    var chart_status = view.querySelector('#resource_usage_chart_status');
                    chart_status.innerHTML = "Loading Data...";
                    ApiClient.getServerData(resource_url).then(function (result_data) {
                        draw_graph(view, d3, result_data);
                        chart_status.innerHTML = "&nbsp;";
                    }, function (response) { chart_status.innerHTML = response.status + ":" + response.statusText; });
                }

            });

            var refresh_button = view.querySelector('#process_list_refresh');
            refresh_button.addEventListener("click", reresh_process_table);

            var sort_type = view.querySelector('#process_list_sort_order');
            sort_type.addEventListener("change", reresh_process_table);

            reresh_process_table();

            function reresh_process_table() {
                var process_list_url = "user_usage_stats/process_list?stamp=" + new Date().getTime();
                process_list_url = ApiClient.getUrl(process_list_url);

                var process_list_status = view.querySelector('#process_list_status');
                process_list_status.innerHTML = "Loading Data...";
                //process_list_status.style.display = "block";
                ApiClient.getServerData(process_list_url).then(function (result_data) {
                    show_process_list(view, result_data);
                    process_list_status.innerHTML = "&nbsp;";
                    //process_list_status.style.display = "none";
                }, function (response) { process_list_status.innerHTML = response.status + ":" + response.statusText; });
            }

        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});