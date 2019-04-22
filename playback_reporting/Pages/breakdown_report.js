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

    Date.prototype.toDateInputValue = function () {
        var local = new Date(this);
        local.setMinutes(this.getMinutes() - this.getTimezoneOffset());
        return local.toJSON().slice(0, 10);
    };

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

    function draw_chart_user_count(view, local_chart, data, group_type, max_item_count, add_other) {

        var chart_data_labels_count = [];
        var chart_data_values_count = [];

        var chart_data_labels_time = [];
        var chart_data_values_time = [];

        data.sort(function (a, b) {
            return a["count"] > b["count"] ? -1 : a["count"] === b["count"] ? 0 : 1;
        });

        var count = 0;
        var max_items = max_item_count;
        var index;
        var other_count = 0;
        for (index in data) {
            if (count++ < max_items) {
                chart_data_labels_count.push(data[index]["label"]);
                chart_data_values_count.push(data[index]["count"]);
            }
            else {
                other_count += data[index]["count"];
            }
        }
        if (other_count > 0 && add_other) {
            chart_data_labels_count.push("Other");
            chart_data_values_count.push(other_count);
        }

        data.sort(function (a, b) {
            return a["time"] > b["time"] ? -1 : a["time"] === b["time"] ? 0 : 1;
        });

        count = 0;
        other_count = 0;
        for (index in data) {
            if (count++ < max_items) {
                chart_data_labels_time.push(data[index]["label"]);
                chart_data_values_time.push(data[index]["time"]);
            }
            else {
                other_count += data[index]["time"];
            }
        }
        if (other_count > 0 && add_other) {
            chart_data_labels_time.push("Other");
            chart_data_values_time.push(other_count);
        }

        var colours_pallet = ["#d98880", "#c39bd3", "#7fb3d5", "#76d7c4", "#7dcea0", "#f7dc6f", "#f0b27a", "#d7dbdd", "#85c1e9", "#f1948a"];
        var all_colours = [];
        var colour_max = max_items;
        if (max_items) {
            colour_max += 1;
        }
        while (all_colours.length < colour_max) {
            all_colours = all_colours.concat(colours_pallet);
        }

        //console.log(chart_data_labels_count);
        //console.log(chart_data_values_count);
        //console.log(chart_data_labels_time);
        //console.log(chart_data_values_time);

        var chart_data_user_count = {
            labels: chart_data_labels_count,
            datasets: [{
                label: "Breakdown",
                backgroundColor: all_colours,
                data: chart_data_values_count
            }]
        };

        var chart_data_user_time = {
            labels: chart_data_labels_time,
            datasets: [{
                label: "Breakdown",
                backgroundColor: all_colours,
                data: chart_data_values_time
            }]
        };

        function tooltip_labels(tooltipItem, data) {

            var indice = tooltipItem.index; 
            var label = data.labels[indice] || "";

            if (label) {
                label += ": " + seconds2time(data.datasets[0].data[indice]);
            }

            return label;
        }

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
                    text: group_type + " (Plays)"
                },
                legend: {
                    display: false
                },
                legendCallback: function (chart) {
                    var legendHtml = [];
                    legendHtml.push('<table style="width:95%">');
                    var item = chart.data.datasets[0];
                    for (var i = 0; i < item.data.length; i++) {
                        legendHtml.push('<tr>');
                        legendHtml.push('<td style="width: 20px"><div style="width: 20px; background-color:' + item.backgroundColor[i] + '">&nbsp;</div></td>');
                        legendHtml.push('<td style="max-width: 100px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">' + chart.data.labels[i] + '</td>');
                        legendHtml.push('<td style="width: 10px; text-align: right; white-space: nowrap;">' + item.data[i] + '</td>');
                        legendHtml.push('</tr>');
                    }
                    legendHtml.push('</table>');
                    return legendHtml.join("");
                }
            }
        });

        var chart_legend_count = view.querySelector('#' + group_type + '_breakdown_count_chart_legend');
        if (chart_legend_count !== null) {
            chart_legend_count.innerHTML = chart_instance_map[group_type + "_count"].generateLegend();
        }

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
                    text: group_type + " (Time)"
                },
                tooltips: {
                    callbacks: {
                        label: tooltip_labels
                    }
                },
                legend: {
                    display: false
                },
                legendCallback: function (chart) {
                    var legendHtml = [];
                    legendHtml.push('<table style="width:95%">');
                    var item = chart.data.datasets[0];
                    for (var i = 0; i < item.data.length; i++) {
                        legendHtml.push('<tr>');
                        legendHtml.push('<td style="width: 20px"><div style="width: 20px; background-color:' + item.backgroundColor[i] + '">&nbsp;</div></td>');
                        legendHtml.push('<td style="max-width: 100px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">' + chart.data.labels[i] + '</td>');
                        legendHtml.push('<td style="width: 10px; text-align: right; white-space: nowrap;">' + seconds2time(item.data[i]) + '</td>');
                        legendHtml.push('</tr>');
                    }
                    legendHtml.push('</table>');
                    return legendHtml.join("");
                }
            }
        });

        var chart_legend_time = view.querySelector('#' + group_type + '_breakdown_time_chart_legend');
        if (chart_legend_time !== null) {
            chart_legend_time.innerHTML = chart_instance_map[group_type + "_time"].generateLegend();
        }

        console.log("Charts Done");
    }

    function seconds2time(seconds) {
        var d = Math.floor(seconds / 86400);
        seconds = seconds - d * 86400;
        var h = Math.floor(seconds / 3600);
        seconds = seconds - h * 3600;
        var m = Math.floor(seconds / 60);
        var s = seconds - m * 60;
        var time_string = "";
        if (d > 0) {
            time_string += d + ".";
        }
        time_string += padLeft(h) + ":" + padLeft(m) + ":" + padLeft(s);
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
        view.addEventListener('viewshow', function (e) {

            libraryMenu.setTabs('playback_reporting', getTabIndex("breakdown_report"), getTabs);

            require([Dashboard.getConfigurationResourceUrl('Chart.bundle.min.js')], function (d3) {

                var end_date = view.querySelector('#end_date');
                end_date.value = new Date().toDateInputValue();
                end_date.addEventListener("change", process_click);

                var weeks = view.querySelector('#weeks');
                weeks.addEventListener("change", process_click);

                var num_items = view.querySelector('#num_items');
                num_items.addEventListener("change", process_click);

                var add_other_items = view.querySelector('#add_other_items');
                add_other_items.addEventListener("change", process_click);

                process_click();

                function process_click() {
                    var days = parseInt(weeks.value) * 7;
                    var item_count = parseInt(num_items.value);
                    var add_other_line = add_other_items.checked;
                    var url = "";
                    
                    // build user chart
                    url = "user_usage_stats/UserId/BreakdownReport?days=" + days + "&end_date=" + end_date.value + "&stamp=" + new Date().getTime();
                    url = ApiClient.getUrl(url);
                    ApiClient.getUserActivity(url).then(function (data) {
                        //alert("Loaded Data: " + JSON.stringify(usage_data));
                        draw_chart_user_count(view, d3, data, "User", item_count, add_other_line);
                    });
                    
                    // build ItemType chart
                    url = "user_usage_stats/ItemType/BreakdownReport?days=" + days + "&end_date=" + end_date.value + "&stamp=" + new Date().getTime();
                    url = ApiClient.getUrl(url);
                    ApiClient.getUserActivity(url).then(function (data) {
                        //alert("Loaded Data: " + JSON.stringify(usage_data));
                        draw_chart_user_count(view, d3, data, "ItemType", item_count, add_other_line);
                    });

                    // build PlaybackMethod chart
                    url = "user_usage_stats/PlaybackMethod/BreakdownReport?days=" + days + "&end_date=" + end_date.value + "&stamp=" + new Date().getTime();
                    url = ApiClient.getUrl(url);
                    ApiClient.getUserActivity(url).then(function (data) {
                        //alert("Loaded Data: " + JSON.stringify(usage_data));
                        draw_chart_user_count(view, d3, data, "PlayMethod", item_count, add_other_line);
                    });

                    // build ClientName chart
                    url = "user_usage_stats/ClientName/BreakdownReport?days=" + days + "&end_date=" + end_date.value + "&stamp=" + new Date().getTime();
                    url = ApiClient.getUrl(url);
                    ApiClient.getUserActivity(url).then(function (data) {
                        //alert("Loaded Data: " + JSON.stringify(usage_data));
                        draw_chart_user_count(view, d3, data, "ClientName", item_count, add_other_line);
                    });

                    // build DeviceName chart
                    url = "user_usage_stats/DeviceName/BreakdownReport?days=" + days + "&end_date=" + end_date.value + "&stamp=" + new Date().getTime();
                    url = ApiClient.getUrl(url);
                    ApiClient.getUserActivity(url).then(function (data) {
                        //alert("Loaded Data: " + JSON.stringify(usage_data));
                        draw_chart_user_count(view, d3, data, "DeviceName", item_count, add_other_line);
                    });

                    // build TvShows chart
                    url = "user_usage_stats/TvShowsReport?days=" + days + "&end_date=" + end_date.value + "&stamp=" + new Date().getTime();
                    url = ApiClient.getUrl(url);
                    ApiClient.getUserActivity(url).then(function (data) {
                        //alert("Loaded Data: " + JSON.stringify(usage_data));
                        draw_chart_user_count(view, d3, data, "TvShows", item_count, add_other_line);
                    });

                    // build Movies chart
                    url = "user_usage_stats/MoviesReport?days=" + days + "&end_date=" + end_date.value + "&stamp=" + new Date().getTime();
                    url = ApiClient.getUrl(url);
                    ApiClient.getUserActivity(url).then(function (data) {
                        //alert("Loaded Data: " + JSON.stringify(usage_data));
                        draw_chart_user_count(view, d3, data, "Movies", item_count, add_other_line);
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