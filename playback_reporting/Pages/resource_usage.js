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

    var resource_chart = null;
    var color_list = [];

    ApiClient.getServerData = function (url_to_get) {
        console.log("getServerData Url = " + url_to_get);
        return this.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };

    function get_new_date_string(start_date, offset) {

        var base_start_date = new Date(start_date.getTime() - (offset * 60 * 1000));
        var base_time_stamp = base_start_date.getFullYear() + "-" + ("0" + (base_start_date.getMonth() + 1)).slice(-2) + "-" + ("0" + base_start_date.getDate()).slice(-2);
        base_time_stamp += " " + ("0" + base_start_date.getHours()).slice(-2) + ":" + ("0" + base_start_date.getMinutes()).slice(-2) + ":00";
        console.log("base_time_stamp: " + base_time_stamp);
        return base_time_stamp;
    }

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

        // add point zero //yyyy-MM-dd HH:mm:ss
        var hours_selection = view.querySelector('#requested_number_hours');
        var hours_value = hours_selection.options[hours_selection.selectedIndex].value;

        var base_time_stamp = get_new_date_string(new Date(), hours_value * 60);

        server_load_data.push({ x: base_time_stamp, y: 0 });
        server_mem_data.push({ x: base_time_stamp, y: 0 });

        for (var index = 0; index < counter_data.length; ++index) {
            var resource_counter = counter_data[index];

            if (index === 0) {
                server_load_data.push({ x: resource_counter.date, y: 0 });
                server_mem_data.push({ x: resource_counter.date, y: 0 });
            }

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

        var all_colours = [];
        while (all_colours.length < 3 && color_list.length !== 0) {
            all_colours = all_colours.concat(color_list);
        }

        var timeFormat = 'YYYY/MM/DD HH:mm:ss';

        var chart_config = {
            type: 'line',
            data: {
                datasets: [{
                    label: 'CPU Load',
                    backgroundColor: all_colours[0],
                    borderColor: all_colours[0],
                    fill: false,
                    pointRadius: 2,
                    data: server_load_data,
                    lineTension: 0,
                    yAxisID: "y-axis-1"
                },
                {
                    label: 'MEM Used (MB)',
                    backgroundColor: all_colours[1],
                    borderColor: all_colours[1],
                    fill: false,
                    pointRadius: 2,
                    data: server_mem_data,
                    lineTension: 0,
                    yAxisID: "y-axis-2"
                },
                {
                    label: 'Playback Count',
                    backgroundColor: all_colours[2] + "45",
                    borderColor: all_colours[2] + "45",
                    fill: true,
                    pointRadius: 1,
                    data: play_counts,
                    lineTension: 0,
                    yAxisID: "y-axis-3"
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
                        ticks: {
                            max: 100,
                            stepSize: 5,
                            min: 0,
                            beginAtZero: true
                        },
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
                        ticks: {
                            min: 0,
                            beginAtZero: true
                        },
                        position: 'right',
                        id: 'y-axis-2'
                    },
                    {
                        scaleLabel: {
                            display: false,
                            labelString: 'value'
                        },
                        type: 'linear',
                        display: true,
                        ticks: {
                            stepSize: 1,
                            min: 0,
                            beginAtZero: true
                        },
                        position: 'right',
                        id: 'y-axis-3'
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
        var cpu_total = 0;
        var mem_total = 0;

        process_list_data.forEach(function (item_details, index) {
            table_rows += "<tr class='detailTableBodyRow detailTableBodyRow-shaded'>";
            table_rows += "<td style='padding-left:20px;padding-right:30px'>" + item_details.id + "</td>";
            table_rows += "<td style='padding-left:20px;padding-right:30px'>" + item_details.name + "</td>";

            if (item_details.error && item_details.error.startsWith("Cpu:")) {
                table_rows += "<td style='padding-left:20px;padding-right:30px'>&nbsp;</td>";
            }
            else {
                table_rows += "<td style='padding-left:20px;padding-right:30px'>" + item_details.cpu + "</td>";
            }

            if (item_details.error && item_details.error.startsWith("Mem:")) {
                table_rows += "<td style='padding-left:20px;padding-right:30px'>&nbsp;</td>";
            }
            else {
                table_rows += "<td style='padding-left:20px;padding-right:30px'>" + formatBytes(item_details.mem, 2) + "</td>";
            }

            /*
            if (item_details.error) {
                table_rows += "<td style='padding-right: 20px'>" + item_details.error + "</td>";
            }
            else {
                table_rows += "<td style='padding-right: 20px'>&nbsp;</td>";
            }
            */

            table_rows += "</tr>";

            cpu_total += item_details.cpu;
            mem_total += item_details.mem;
        });

        table_body.innerHTML = table_rows;

        var total_cpu_span = view.querySelector('#total_cpu');
        total_cpu_span.innerHTML = "(" + Math.round(cpu_total * 10) / 10 + ")";

        var total_mem_span = view.querySelector('#total_mem');
        total_mem_span.innerHTML = "(" + formatBytes(mem_total, 2) + ")";

    }

    return function (view, params) {

        // init code here
        // https://cdnjs.cloudflare.com/ajax/libs/Chart.js/2.7.2/Chart.bundle.min.js
        view.addEventListener('viewshow', function (e) {
            mainTabsManager.setTabs(this, getTabIndex("resource_usage"), getTabs);

            require([Dashboard.getConfigurationResourceUrl('Chart.bundle.min.js')], function (d3) {

                var hours_selection = view.querySelector('#requested_number_hours');
                hours_selection.addEventListener("change", process_click);

                var chart_refresh_button = view.querySelector('#resource_chart_refresh');
                chart_refresh_button.addEventListener("click", process_click);

                ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {
                    if (config.ColourPalette.length === 0) {
                        color_list = getDefautColours();
                    }
                    else {
                        color_list = config.ColourPalette;
                    }
                    process_click();
                });

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
            refresh_button.addEventListener("click", refesh_process_table);

            var sort_type = view.querySelector('#process_list_sort_order');
            sort_type.addEventListener("change", refesh_process_table);

            refesh_process_table();

            function refesh_process_table() {
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