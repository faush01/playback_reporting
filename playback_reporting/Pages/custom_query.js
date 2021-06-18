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

    var custom_chart = null;
    var color_list = [];

    ApiClient.sendCustomQuery = function (url_to_get, query_data) {
        var post_data = JSON.stringify(query_data);
        console.log("sendCustomQuery url  = " + url_to_get);
        console.log("sendCustomQuery data = " + post_data);
        return this.ajax({
            type: "POST",
            url: url_to_get,
            dataType: "json",
            data: post_data,
            contentType: 'application/json'
        });
    };

    function delete_custom_query(view) {

        ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {

            var custom_query_id = parseInt(view.querySelector('#custom_query_id').value);

            // find item
            var index = 0;
            var found_index = -1;
            for (index = 0; index < config.CustomQueries.length; ++index) {
                if (config.CustomQueries[index].Id === custom_query_id) {
                    found_index = index;
                    break;
                }
            }

            if (found_index !== -1) {
                config.CustomQueries.splice(found_index, 1);
            }

            console.log("New CustomQueries Settings : " + JSON.stringify(config.CustomQueries));
            ApiClient.updateNamedConfiguration('playback_reporting', config);

            load_query_list(view, config);
        });

    }

    function save_custom_query(view) {

        ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {

            var custom_query_id = parseInt(view.querySelector('#custom_query_id').value);
            var custom_query_name = view.querySelector('#custom_query_name').value.trim();
            var custom_query_text = view.querySelector('#custom_query_text').value.trim();
            var replace_userid_bool = view.querySelector('#replace_userid').checked;
            var data_label_column = view.querySelector('#custom_query_chart_label_column').value.trim();
            var data_data_column = view.querySelector('#custom_query_chart_data_column').value.trim();
            var chart_type = view.querySelector('#custom_query_chart_type').selectedIndex;

            if (custom_query_name === "") {
                alert("Name can not be empty");
                return;
            }

            // find item
            var index = 0;
            var found_index = -1;
            var max_id = -1;
            for (index = 0; index < config.CustomQueries.length; ++index) {
                if (config.CustomQueries[index].Id === custom_query_id) {
                    found_index = index;
                    break;
                }
                if (config.CustomQueries[index].Id > max_id) {
                    max_id = config.CustomQueries[index].Id;
                }
            }

            console.log("found_index : " + found_index);

            if (found_index === -1) {
                custom_query_id = (max_id + 1);
            }
            else {
                custom_query_id = parseInt(custom_query_id);
            }

            var new_custom_query =
            {
                Id: custom_query_id,
                Name: custom_query_name,
                Query: custom_query_text,
                ReplaceName: replace_userid_bool,
                ChartType: chart_type,
                ChartLabelColumn: data_label_column,
                ChartDataCloumn: data_data_column
            };
            console.log("new_custom_query : " + JSON.stringify(new_custom_query));

            console.log("New CustomQueries Settings : " + JSON.stringify(config.CustomQueries));

            if (found_index === -1) {
                config.CustomQueries.push(new_custom_query);
            }
            else {
                config.CustomQueries[found_index] = new_custom_query;
            }

            console.log("New CustomQueries Settings : " + JSON.stringify(config.CustomQueries));
            ApiClient.updateNamedConfiguration('playback_reporting', config);

            load_query_list(view, config);
        });

    }

    function load_query_list(view, config) {

        var current_selection = view.querySelector('#custom_query_name').value.trim();

        var index = 0;
        var query_list_options = "<option value='-1'>New Query</option>";
        for (index = 0; index < config.CustomQueries.length; ++index) {
            var custom_query_name = config.CustomQueries[index].Name;
            var custom_query_id = config.CustomQueries[index].Id;
            var selected = " ";
            if (current_selection === custom_query_name) {
                selected = " selected ";
            }
            query_list_options += "<option" + selected + "value='" + custom_query_id + "'>" + custom_query_name + "</option>";
        }
        var custom_query_selector = view.querySelector('#custom_query_selector');
        custom_query_selector.innerHTML = query_list_options;
    }

    function on_custom_query_select(view) {
        var custom_query_selector = view.querySelector('#custom_query_selector');
        var custom_query_selected_id = parseInt(custom_query_selector.value);
        // find query with id

        ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {
            var index = 0;
            var custom_query_details = null;
            for (index = 0; index < config.CustomQueries.length; ++index) {
                if (config.CustomQueries[index].Id === custom_query_selected_id) {
                    custom_query_details = config.CustomQueries[index];
                    break;
                }
            }

            var custom_query_id = view.querySelector('#custom_query_id');
            var custom_query_name = view.querySelector('#custom_query_name');
            var custom_query_text = view.querySelector('#custom_query_text');
            var replace_userid_bool = view.querySelector('#replace_userid');
            var data_label_column = view.querySelector('#custom_query_chart_label_column');
            var data_data_column = view.querySelector('#custom_query_chart_data_column');
            var chart_type = view.querySelector('#custom_query_chart_type');

            if (custom_query_details === null) {
                custom_query_id.value = "-1";
                custom_query_name.value = "";
                custom_query_text.value = "";
                replace_userid_bool.checked = false;
                data_label_column.value = "";
                data_data_column.value = "";
                chart_type.options.selectedIndex = 0;
            }
            else {
                custom_query_id.value = custom_query_details.Id;
                custom_query_name.value = custom_query_details.Name;
                custom_query_text.value = custom_query_details.Query;
                replace_userid_bool.checked = custom_query_details.ReplaceName;
                data_label_column.value = custom_query_details.ChartLabelColumn;
                data_data_column.value = custom_query_details.ChartDataCloumn;
                chart_type.options.selectedIndex = custom_query_details.ChartType;
            }
        });

    }

    function build_chart(view, query_results) {

        var data_label_column = view.querySelector('#custom_query_chart_label_column').value.trim();
        var data_data_column = view.querySelector('#custom_query_chart_data_column').value.trim();
        var chart_type = view.querySelector('#custom_query_chart_type').value;

        var chart_div = view.querySelector('#chart_div');
        var chart_canvas = view.querySelector('#custom_query_chart_canvas');

        var custom_query_chart_message = view.querySelector('#custom_query_chart_message');
        custom_query_chart_message.innerHTML = "";

        if (chart_type === "none") {
            chart_div.style.display = "none";
            return;
        }
        else {
            chart_div.style.display = "block";
        }

        var dataset_labels = [];//['January', 'February', 'March', 'April', 'May', 'June', 'July'];
        var dataset_data = [];//[10, 20, 30, 20, 10, 25, 50];

        // find column indexes
        var column_names = query_results["colums"];
        var index = 0;
        var label_index = -1;
        var data_index = -1;
        for (index = 0; index < column_names.length; index++) {
            if (label_index === -1 && column_names[index] === data_label_column) {
                label_index = index;
            }
            if (data_index === -1 && column_names[index] === data_data_column) {
                data_index = index;
            }
        }

        var chart_message = "";

        if (label_index === -1) {
            chart_message += "Could not find Label Column with name (" + data_label_column + ")<br>";
        }
        if (data_index === -1) {
            chart_message += "Could not find Data Column with name (" + data_data_column + ")<br>";
        }

        if (chart_message) {
            custom_query_chart_message.innerHTML = "<br/><h3>Chart Errors:</h3>" + chart_message;
            chart_div.style.display = "none";
            return;
        }

        // extract label and data
        var result_data = query_results["results"];
        for (index = 0; index < result_data.length; index++) {
            var row_data = result_data[index];
            dataset_labels.push(row_data[label_index]);
            var data_value = parseFloat(row_data[data_index]);
            if (isNaN(data_value)) {
                chart_message = "Data is not a number (" + row_data[data_index] + ")";
                break;
            }
            dataset_data.push(data_value);
        }

        console.log("dataset_labels : " + JSON.stringify(dataset_labels));
        console.log("dataset_data : " + JSON.stringify(dataset_data));

        if (chart_message) {
            custom_query_chart_message.innerHTML = "<br/><h3>Chart Errors:</h3>" + chart_message;
            chart_div.style.display = "none";
            return;
        }

        require([Dashboard.getConfigurationResourceUrl('chart.min.js')], function (d3) {

            var ctx = chart_canvas.getContext('2d');

            if (custom_chart) {
                console.log("destroy() existing chart");
                custom_chart.destroy();
            }

            if (color_list.length === 0) {
                color_list.push("#AAAAAA");
            }
            var full_colour_list = [];
            while (full_colour_list.length < dataset_data.length) {
                full_colour_list = full_colour_list.concat(color_list);
            }

            var line_colour = "#AAAAAA";
            var back_colour = full_colour_list;
            
            if (chart_type === "line") {
                line_colour = "#AAAAAAFF";
                back_colour = "#FFFFFF00";
            }
            else if (chart_type === "pie") {
                line_colour = "#AAAAAA";
                back_colour = full_colour_list;
            }

            var chart_data = {
                labels: dataset_labels,
                datasets: [
                    {
                        label: 'Dataset',
                        borderColor: line_colour,
                        backgroundColor: back_colour,
                        data: dataset_data
                    }
                ]
            };

            // chart options
            var chart_options = {
                plugins: {
                    title: {
                        display: false
                    },
                    tooltip: {
                        intersect: true
                    }
                },
                responsive: true,
                maintainAspectRatio: false
            };

            if (chart_type !== "pie") {
                chart_options["scales"] = {
                    xAxes: [{
                        //stacked: true,
                        ticks: {
                            autoSkip: false,
                            maxTicksLimit: 10000
                        }
                    }],
                    yAxes: [{
                        //stacked: true,
                        ticks: {
                            autoSkip: true,
                            beginAtZero: true
                        }
                    }]
                };

                chart_options["plugins"]["legend"] = {
                    display: false
                };
            }

            // create chart
            custom_chart = new Chart(ctx, {
                type: chart_type,
                data: chart_data,
                options: chart_options
            });

        });
    }

    return function (view, params) {

        // init code here
        view.addEventListener('viewshow', function (e) {

            mainTabsManager.setTabs(this, getTabIndex("custom_query"), getTabs);

            var custom_query_save = view.querySelector('#custom_query_save');
            custom_query_save.addEventListener("click", function () { save_custom_query(view); });

            var custom_query_delete = view.querySelector('#custom_query_delete');
            custom_query_delete.addEventListener("click", function () { delete_custom_query(view); });

            var run_custom_query = view.querySelector('#run_custom_query');
            run_custom_query.addEventListener("click", runQuery);

            function runQuery() {

                var custom_query = view.querySelector('#custom_query_text');
                var replace_userid = view.querySelector('#replace_userid').checked;

                //alert("Running: " + custom_query.value);

                var message_div = view.querySelector('#query_result_message');
                message_div.innerHTML = "";
                var table_body = view.querySelector('#custom_query_results');
                table_body.innerHTML = "";

                var url = "user_usage_stats/submit_custom_query?stamp=" + new Date().getTime();
                url = ApiClient.getUrl(url);

                var query_data = {
                    CustomQueryString: custom_query.value,
                    ReplaceUserId: replace_userid
                };

                ApiClient.sendCustomQuery(url, query_data).then(function (result) {
                    //alert("Loaded Data: " + JSON.stringify(result));
                    //console.log("Query Results : " + JSON.stringify(result));

                    var message = result["message"];

                    if (message !== "") {
                        var message_div = view.querySelector('#query_result_message');
                        message_div.innerHTML = message;
                    }
                    else {
                        // build the table
                        var table_row_html = "";

                        // add table heading
                        var result_ladels = result["colums"];
                        table_row_html += "<tr class='detailTableBodyRow detailTableBodyRow-shaded'>";
                        for (var index = 0; index < result_ladels.length; ++index) {
                            var colum_name = result_ladels[index];
                            table_row_html += "<td style='white-space: nowrap;'><strong>" + colum_name + "</strong></td>";
                        }
                        table_row_html += "</tr>";

                        // add the data
                        var result_data_rows = result["results"];
                        for (var index2 = 0; index2 < result_data_rows.length; ++index2) {
                            var row_data = result_data_rows[index2];
                            table_row_html += "<tr class='detailTableBodyRow detailTableBodyRow-shaded'>";

                            for (var index3 = 0; index3 < row_data.length; ++index3) {
                                var cell_data = row_data[index3];
                                table_row_html += "<td style='white-space: nowrap;'>" + cell_data + "</td>";
                            }

                            table_row_html += "</tr>";
                        }

                        var table_area_div = view.querySelector('#table_area_div');
                        table_area_div.setAttribute("style", "overflow:hidden;");
                        
                        var table_body = view.querySelector('#custom_query_results');
                        table_body.innerHTML = table_row_html;

                        table_area_div.setAttribute("style", "overflow:auto;");

                        /*
                        let isDown = false;
                        let startX;
                        let scrollLeft;

                        table_area_div.addEventListener('mousedown', (e) => {
                            isDown = true;
                            //table_area_div.classList.add('active');
                            startX = e.pageX - table_area_div.offsetLeft;
                            scrollLeft = table_area_div.scrollLeft;
                        });
                        table_area_div.addEventListener('mouseleave', () => {
                            isDown = false;
                            //table_area_div.classList.remove('active');
                        });
                        table_area_div.addEventListener('mouseup', () => {
                            isDown = false;
                            //table_area_div.classList.remove('active');
                        });
                        table_area_div.addEventListener('mousemove', (e) => {
                            if (!isDown) return;
                            e.preventDefault();
                            const x = e.pageX - table_area_div.offsetLeft;
                            const walk = (x - startX) * 1; //scroll-fast, for now set to 1 for 1:1 scrolling
                            table_area_div.scrollLeft = scrollLeft - walk;
                            //console.log(walk);
                        });
                        */

                        build_chart(view, result);
                    }
                });
            }

            ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {
                color_list = config.ColourPalette;

                if (config.CustomQueries.length === 0) {

                    var query_text = "SELECT date(DateCreated) AS Date, SUM(PlayDuration) AS PlayTime\n";
                    query_text += "FROM PlaybackActivity\n";
                    query_text += "WHERE ItemType = 'Movie'\n";
                    query_text += "GROUP BY date(DateCreated)\n";
                    query_text += "ORDER BY date(DateCreated) ASC";

                    var new_custom_query =
                    {
                        Id: 1,
                        Name: "Example Query",
                        Query: query_text,
                        ReplaceName: false,
                        ChartType: 1,
                        ChartLabelColumn: "Date",
                        ChartDataCloumn: "PlayTime"
                    };

                    config.CustomQueries.push(new_custom_query);
                }

                //console.log("New CustomQueries Settings : " + JSON.stringify(config.CustomQueries));
                ApiClient.updateNamedConfiguration('playback_reporting', config);

                load_query_list(view, config);
            });

            var custom_query_selector = view.querySelector('#custom_query_selector');
            custom_query_selector.addEventListener("change", function () { on_custom_query_select(view); });

        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});