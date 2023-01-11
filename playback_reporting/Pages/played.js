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

    ApiClient.getApiData = function (url_to_get) {
        console.log("getUserActivity Url = " + url_to_get);
        return this.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };

    function PopulatePlayedInfo(view, item_info) {
        console.log(item_info);

        view.querySelector("#search_text").value = "";

        if (item_info !== null) {
            var url = "user_usage_stats/get_item_stats?id=" + item_info.Id;
            url += "&stamp=" + new Date().getTime();
            url = ApiClient.getUrl(url);
            ApiClient.getApiData(url).then(function (user_data) {
                console.log("Loaded Data: " + JSON.stringify(user_data));

                // populate the item info
                var played_item_info = view.querySelector('#played_item_info');
                var item_display_info = "<span style='font-weight: bold; font-size:120%;'>" + item_info.Name + "</span><br>";
                item_display_info += item_info.ItemType + "<br>";
                
                //item_display_info += "Item Id : " + item_info.Id + "<br>";
                //item_display_info += "Series : " + item_info.Series + "<br>";

                played_item_info.innerHTML = item_display_info;

                // clean and populate the user played info
                var played_users_details = view.querySelector('#played_users_details');
                while (played_users_details.firstChild) {
                    played_users_details.removeChild(played_users_details.firstChild);
                }

                for (const user_info of user_data) {
                    var tr = document.createElement("tr");
                    var td = document.createElement("td");

                    var played = user_info.played === "True"
                    if (user_info.child_watched && user_info.child_total) {
                        if (user_info.child_watched === user_info.child_total) {
                            played = true;
                        }
                    }

                    if (played) {
                        var i = document.createElement("i");
                        i.className = "md-icon";
                        i.style.fontSize = "25px";
                        i.style.color = "#00FF00";
                        i.appendChild(document.createTextNode("check_circle_outline"));
                        td.appendChild(i);
                        //td.style.backgroundColor = "#00FF00";
                    }
                    else {
                        var i = document.createElement("i");
                        i.className = "md-icon";
                        i.style.fontSize = "25px";
                        i.style.color = "grey";
                        i.appendChild(document.createTextNode("highlight_off"));
                        td.appendChild(i);
                        //td.style.backgroundColor = "#FF0000";
                    }
                    tr.appendChild(td);

                    td = document.createElement("td");
                    var user_info_txt = user_info.name;
                    if (user_info.child_stats) {
                        user_info_txt += " (" + user_info.child_stats + ")";
                    } 
                    td.appendChild(document.createTextNode(user_info_txt));
                    tr.appendChild(td);

                    played_users_details.appendChild(tr);
                }

            });
        }

    }

    function PopulateSelectedPath(view, item_info) {

        var path_string = view.querySelector("#path_string");
        while (path_string.firstChild) {
            path_string.removeChild(path_string.firstChild);
        }
        
        if (item_info === null) {
            path_string.appendChild(document.createTextNode("\\"));
            return;
        }

        var url = "user_usage_stats/get_item_path";
        url += "?id=" + item_info.Id;
        url += "&stamp=" + new Date().getTime();
        url = ApiClient.getUrl(url);

        ApiClient.getApiData(url).then(function (item_path_data) {
            console.log("Loaded Path Data: " + JSON.stringify(item_path_data));

            //var path_link_data = "";
            for (const path_info of item_path_data) {

                var span = document.createElement("span");
                span.appendChild(document.createTextNode(path_info.Name));
                span.style.cursor = "pointer"; 

                span.addEventListener("click", function () {
                    var item_data = { Name: path_info.Name, Id: path_info.Id };
                    PopulateSelector(view, item_data, "");
                    PopulatePlayedInfo(view, item_data);
                    PopulateSelectedPath(view, item_data);
                });

                path_string.appendChild(document.createTextNode("\\"));
                path_string.appendChild(span);

            }
        });

    }

    function PopulateSelector(view, item_info, search_filter) {

        var parent_id = 0;
        var url = "user_usage_stats/get_items?";

        var is_seaerch = false;

        if (search_filter !== null && search_filter.length > 0) {
            url += "parent=0";
            url += "&filter=" + search_filter;
            is_seaerch = true;
        }
        else if (item_info !== null) {
            parent_id = item_info.Id;
            url += "parent=" + parent_id;
        }
        else {
            url += "parent=0";
        }

        url += "&stamp=" + new Date().getTime();
        url = ApiClient.getUrl(url);

        ApiClient.getApiData(url).then(function (item_data) {
            //alert("Loaded Data: " + JSON.stringify(item_data));

            var item_list = view.querySelector('#item_list');

            // clear current list
            if (is_seaerch || (item_info !== null && item_data.length > 0)) {
                while (item_list.firstChild) {
                    item_list.removeChild(item_list.firstChild);
                }
            }

            // add items
            for (const item_details of item_data) {
                var tr = document.createElement("tr");
                var td = document.createElement("td");
                td.appendChild(document.createTextNode(item_details.Name));
                td.addEventListener("click", function () {
                    PopulateSelectedPath(view, item_details);
                    PopulatePlayedInfo(view, item_details);
                    PopulateSelector(view, item_details, "");
                });
                td.style.cursor = "pointer"; 
                tr.appendChild(td);
                item_list.appendChild(tr);
            }

        });
    }

    var qr_timeout = null;
    function SearchChanged(view, search_box) {

        if (qr_timeout != null) {
            clearTimeout(qr_timeout);
        }

        qr_timeout = setTimeout(function () {

            var search_text = search_box.value;
            search_text = search_text.trim();
            console.log("search: " + search_text);
            var item_info = { Id: "0" };
            PopulateSelector(view, item_info, search_text);

        }, 500);

    }

    return function (view, params) {

        // init code here
        view.addEventListener('viewshow', function (e) {

            mainTabsManager.setTabs(this, getTabIndex("played"), getTabs);

            var style = document.createElement('style');
            style.innerHTML = '#item_list tr:hover { background-color: grey; }';
            var ref = document.querySelector('script');
            ref.parentNode.insertBefore(style, ref);

            var search_box = view.querySelector("#search_text");
            search_box.addEventListener("input", function () { SearchChanged(view, search_box); });

            PopulateSelector(view, null, "");

        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});