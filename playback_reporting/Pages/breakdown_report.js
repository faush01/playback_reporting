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

    var chart_instance_map = {};
    var color_list = [];

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

    function generate_chart_legend_count(chart, group_type) {
        var legendHtml = [];
        legendHtml.push('<table style="width:95%">');
        var item = chart.data.datasets[0];
        for (var i = 0; i < item.data.length; i++) {
            legendHtml.push('<tr>');
            legendHtml.push('<td style="width: 20px"><div style="width: 20px; background-color:' + item.backgroundColor[i] + '">&nbsp;</div></td>');
            var label_data = chart.data.labels[i];
            if (group_type === "Movies" || group_type === "TvShows") {
                var filter_name = chart.data.labels[i];
                if (group_type === "TvShows") {
                    filter_name += " - *";
                }
                var encoded_uri = encodeURI(filter_name);
                encoded_uri = encoded_uri.replace("'", "%27");
                encoded_uri = encoded_uri.replace("\"", "%22");
                var summary_url = Dashboard.getConfigurationPageUrl('user_play_report') + "&filter_name=" + encoded_uri;
                label_data = "<a href='" + summary_url + "' is='emby-linkbutton' style='padding: 0px;font-weight:normal;'>" + chart.data.labels[i] + "</a>";
            }
            legendHtml.push('<td style="max-width: 100px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">' + label_data + '</td>');
            legendHtml.push('<td style="width: 10px; text-align: right; white-space: nowrap;">' + item.data[i] + '</td>');
            legendHtml.push('</tr>');
        }
        legendHtml.push('</table>');
        return legendHtml.join("");
    }

    function generate_chart_legend_time(chart, group_type) {
        var legendHtml = [];
        legendHtml.push('<table style="width:95%">');
        var item = chart.data.datasets[0];
        for (var i = 0; i < item.data.length; i++) {
            legendHtml.push('<tr>');
            legendHtml.push('<td style="width: 20px"><div style="width: 20px; background-color:' + item.backgroundColor[i] + '">&nbsp;</div></td>');
            var label_data = chart.data.labels[i];
            if (group_type === "Movies" || group_type === "TvShows") {
                var filter_name = chart.data.labels[i];
                if (group_type === "TvShows") {
                    filter_name += " - *";
                }
                var encoded_uri = encodeURI(filter_name);
                encoded_uri = encoded_uri.replace("'", "%27");
                encoded_uri = encoded_uri.replace("\"", "%22");
                var summary_url = Dashboard.getConfigurationPageUrl('user_play_report') + "&filter_name=" + encoded_uri;
                label_data = "<a href='" + summary_url + "' is='emby-linkbutton' style='padding: 0px;font-weight:normal;'>" + chart.data.labels[i] + "</a>";
            }
            legendHtml.push('<td style="max-width: 100px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;">' + label_data + '</td>');
            legendHtml.push('<td style="width: 10px; text-align: right; white-space: nowrap;">' + seconds2time(item.data[i]) + '</td>');
            legendHtml.push('</tr>');

        }
        legendHtml.push('</table>');
        return legendHtml.join("");
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

        var all_colours = [];
        var colour_max = max_items;
        if (max_items) {
            colour_max += 1;
        }
        while (all_colours.length < colour_max && color_list.length !== 0) {
            all_colours = all_colours.concat(color_list);
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

        function tooltip_labels(tooltipItem) {

            var data_index = tooltipItem.dataIndex;
            var label = tooltipItem.label || '';

            if (label) {
                label += ": " + seconds2time(tooltipItem.dataset.data[data_index]);
            }

            return label;
        }

        var chart_canvas_count = view.querySelector('#' + group_type + '_breakdown_count_chart_canvas');
        var cxt_count = chart_canvas_count.getContext('2d');
        if (chart_instance_map[group_type + "_count"]) {
            console.log("destroy() existing chart");
            chart_instance_map[group_type + "_count"].destroy();
        }
        chart_instance_map[group_type + "_count"] = new Chart(cxt_count, {
            type: 'pie',
            data: chart_data_user_count,
            options: {
                plugins: {
                    title: {
                        display: true,
                        text: group_type + " (Plays)"
                    },
                    legend: {
                        display: false
                    }
                }
            }
        });

        var chart_legend_count = view.querySelector('#' + group_type + '_breakdown_count_chart_legend');
        if (chart_legend_count !== null) {
            var legend_data_count = generate_chart_legend_count(chart_instance_map[group_type + "_count"], group_type);
            chart_legend_count.innerHTML = legend_data_count;
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
                plugins: {
                    title: {
                        display: true,
                        text: group_type + " (Time)"
                    },
                    tooltip: {
                        callbacks: {
                            label: tooltip_labels
                        }
                    },
                    legend: {
                        display: false
                    }
                }
            }
        });

        var chart_legend_time = view.querySelector('#' + group_type + '_breakdown_time_chart_legend');
        if (chart_legend_time !== null) {
            var legend_data_time = generate_chart_legend_time(chart_instance_map[group_type + "_time"], group_type);
            chart_legend_time.innerHTML = legend_data_time;
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

            mainTabsManager.setTabs(this, getTabIndex("breakdown_report"), getTabs);

            require([Dashboard.getConfigurationResourceUrl('chart.min.js')], function (d3) {

                var user_name = "";
                var user_name_index = window.location.href.indexOf("user=");
                if (user_name_index > -1) {
                    user_name = window.location.href.substring(user_name_index + 5);
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

                var num_items = view.querySelector('#num_items');
                num_items.addEventListener("change", process_click);

                var add_other_items = view.querySelector('#add_other_items');
                add_other_items.addEventListener("change", process_click);

                var user_list_selector = view.querySelector('#user_list');
                user_list_selector.addEventListener("change", process_click);

                // add user list to selector
                var url = "user_usage_stats/user_list?stamp=" + new Date().getTime();
                url = ApiClient.getUrl(url);

                ApiClient.getUserActivity(url).then(function (user_list) {

                    ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {
                        if (config.ColourPalette.length === 0) {
                            color_list = getDefautColours();
                        }
                        else {
                            color_list = config.ColourPalette;
                        }

                        //alert("Loaded Data: " + JSON.stringify(user_list));
                        var index = 0;
                        var options_html = "<option value=''>All Users</option>";
                        var item_details;
                        for (index = 0; index < user_list.length; ++index) {
                            item_details = user_list[index];
                            if (user_name === item_details.name) {
                                options_html += "<option value='" + item_details.id + "' selected>" + item_details.name + "</option>";
                            }
                            else {
                                options_html += "<option value='" + item_details.id + "'>" + item_details.name + "</option>";
                            }

                        }
                        user_list_selector.innerHTML = options_html;

                        process_click();
                    });
                });

                function process_click() {
                    var start = new Date(start_picker.value);
                    var end = new Date(end_picker.value);
                    if (end > new Date()) {
                        end = new Date();
                        end_picker.value = end.toDateInputValue();
                    }

                    var days = Date.daysBetween(start, end);
                    span_days_text.innerHTML = days;

                    var item_count = parseInt(num_items.value);
                    var add_other_line = add_other_items.checked;
                    var url = "";

                    var load_status = view.querySelector('#breakdown_report_status');
                    load_status.innerHTML = "Loading Data...";
                    var load_count = 0;

                    var selected_user_id = user_list_selector.options[user_list_selector.selectedIndex].value;

                    // build user chart
                    url = "user_usage_stats/UserId/BreakdownReport?user_id=" + selected_user_id + "&days=" + days + "&end_date=" + end_picker.value + "&stamp=" + new Date().getTime();
                    url = ApiClient.getUrl(url);
                    ApiClient.getUserActivity(url).then(function (data) {
                        if (++load_count === 7) { load_status.innerHTML = "&nbsp;"; }
                        //alert("Loaded Data: " + JSON.stringify(usage_data));
                        draw_chart_user_count(view, d3, data, "User", item_count, add_other_line);
                    }, function (response) { load_count = -100; load_status.innerHTML = response.status + ":" + response.statusText; });
                    
                    // build ItemType chart
                    url = "user_usage_stats/ItemType/BreakdownReport?user_id=" + selected_user_id + "&days=" + days + "&end_date=" + end_picker.value + "&stamp=" + new Date().getTime();
                    url = ApiClient.getUrl(url);
                    ApiClient.getUserActivity(url).then(function (data) {
                        if (++load_count === 7) { load_status.innerHTML = "&nbsp;"; }
                        //alert("Loaded Data: " + JSON.stringify(usage_data));
                        draw_chart_user_count(view, d3, data, "ItemType", item_count, add_other_line);
                    }, function (response) { load_count = -100; load_status.innerHTML = response.status + ":" + response.statusText; });

                    // build PlaybackMethod chart
                    url = "user_usage_stats/PlaybackMethod/BreakdownReport?user_id=" + selected_user_id + "&days=" + days + "&end_date=" + end_picker.value + "&stamp=" + new Date().getTime();
                    url = ApiClient.getUrl(url);
                    ApiClient.getUserActivity(url).then(function (data) {
                        if (++load_count === 7) { load_status.innerHTML = "&nbsp;"; }
                        //alert("Loaded Data: " + JSON.stringify(usage_data));
                        draw_chart_user_count(view, d3, data, "PlayMethod", item_count, add_other_line);
                    }, function (response) { load_count = -100; load_status.innerHTML = response.status + ":" + response.statusText; });

                    // build ClientName chart
                    url = "user_usage_stats/ClientName/BreakdownReport?user_id=" + selected_user_id + "&days=" + days + "&end_date=" + end_picker.value + "&stamp=" + new Date().getTime();
                    url = ApiClient.getUrl(url);
                    ApiClient.getUserActivity(url).then(function (data) {
                        if (++load_count === 7) { load_status.innerHTML = "&nbsp;"; }
                        //alert("Loaded Data: " + JSON.stringify(usage_data));
                        draw_chart_user_count(view, d3, data, "ClientName", item_count, add_other_line);
                    }, function (response) { load_count = -100; load_status.innerHTML = response.status + ":" + response.statusText; });

                    // build DeviceName chart
                    url = "user_usage_stats/DeviceName/BreakdownReport?user_id=" + selected_user_id + "&days=" + days + "&end_date=" + end_picker.value + "&stamp=" + new Date().getTime();
                    url = ApiClient.getUrl(url);
                    ApiClient.getUserActivity(url).then(function (data) {
                        if (++load_count === 7) { load_status.innerHTML = "&nbsp;"; }
                        //alert("Loaded Data: " + JSON.stringify(usage_data));
                        draw_chart_user_count(view, d3, data, "DeviceName", item_count, add_other_line);
                    }, function (response) { load_count = -100; load_status.innerHTML = response.status + ":" + response.statusText; });

                    // build TvShows chart
                    url = "user_usage_stats/TvShowsReport?user_id=" + selected_user_id + "&days=" + days + "&end_date=" + end_picker.value + "&stamp=" + new Date().getTime();
                    url = ApiClient.getUrl(url);
                    ApiClient.getUserActivity(url).then(function (data) {
                        if (++load_count === 7) { load_status.innerHTML = "&nbsp;"; }
                        //alert("Loaded Data: " + JSON.stringify(usage_data));
                        draw_chart_user_count(view, d3, data, "TvShows", item_count, add_other_line);
                    }, function (response) { load_count = -100; load_status.innerHTML = response.status + ":" + response.statusText; });

                    // build Movies chart
                    url = "user_usage_stats/MoviesReport?user_id=" + selected_user_id + "&days=" + days + "&end_date=" + end_picker.value + "&stamp=" + new Date().getTime();
                    url = ApiClient.getUrl(url);
                    ApiClient.getUserActivity(url).then(function (data) {
                        if (++load_count === 7) { load_status.innerHTML = "&nbsp;"; }
                        //alert("Loaded Data: " + JSON.stringify(usage_data));
                        draw_chart_user_count(view, d3, data, "Movies", item_count, add_other_line);
                    }, function (response) { load_count = -100; load_status.innerHTML = response.status + ":" + response.statusText; });
                }
            });
        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});