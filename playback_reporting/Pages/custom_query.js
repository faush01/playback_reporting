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

define(['libraryMenu'], function (libraryMenu) {
    'use strict';


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

    function getTabs() {
        var tabs = [
            {
                href: Dashboard.getConfigurationPageUrl('user_report'),
                name: 'Users'
            },
            {
                href: Dashboard.getConfigurationPageUrl('user_playback_report'),
                name: 'Playback'
            },
            {
                href: Dashboard.getConfigurationPageUrl('breakdown_report'),
                name: 'Breakdown'
            },
            {
                href: Dashboard.getConfigurationPageUrl('hourly_usage_report'),
                name: 'Hourly'
            },
            {
                href: Dashboard.getConfigurationPageUrl('duration_histogram_report'),
                name: 'Duration'
            },
            {
                href: Dashboard.getConfigurationPageUrl('custom_query'),
                name: 'Query'
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

            libraryMenu.setTabs('custom_query', 5, getTabs);

            var run_custom_query = view.querySelector('#run_custom_query');
            run_custom_query.addEventListener("click", runQuery);

            function runQuery() {

                var custom_query = view.querySelector('#custom_query_text');

                //alert("Running: " + custom_query.value);

                var message_div = view.querySelector('#query_result_message');
                message_div.innerHTML = "";
                var table_body = view.querySelector('#custom_query_results');
                table_body.innerHTML = "";

                var url = "user_usage_stats/submit_custom_query?stamp=" + new Date().getTime();
                url = ApiClient.getUrl(url);

                var query_data = {
                    CustomQueryString: custom_query.value
                };

                ApiClient.sendCustomQuery(url, query_data).then(function (result) {
                    //alert("Loaded Data: " + JSON.stringify(result));

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
                        table_area_div.setAttribute("style", "overflow:hidden;height:500px;");
                        
                        var table_body = view.querySelector('#custom_query_results');
                        table_body.innerHTML = table_row_html;

                        table_area_div.setAttribute("style", "overflow:auto;height:500px;");
                    }
                });
            }

        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});