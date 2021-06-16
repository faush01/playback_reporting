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

    var original_colour = "";

    ApiClient.getUserActivity = function (url_to_get) {
        console.log("getUserActivity Url = " + url_to_get);
        return this.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };	

    function setBackupPathCallBack(selectedDir, view) {
        ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {
            config.BackupPath = selectedDir;
            console.log("New Config Settings : " + JSON.stringify(config));
            ApiClient.updateNamedConfiguration('playback_reporting', config);

            var backup_path_label = view.querySelector('#backup_path_label');
            backup_path_label.innerHTML = selectedDir;
        });
    }

    function remove_playlist_item(view, name) {
        ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {
            var fount_at = -1;
            for (var x = 0; x < config.ActivityPlaylists.length; x++) {
                var playlist_item = config.ActivityPlaylists[x];
                if (name === playlist_item.Name) {
                    fount_at = x;
                    break;
                }
            }

            if (fount_at !== -1) {
                config.ActivityPlaylists.splice(fount_at, 1);
            }

            ApiClient.updateNamedConfiguration('playback_reporting', config);

            loadPage(view, config);
        });
    }

    function edit_playlist_item(view, name) {
        ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {
            var fount_at = -1;
            for (var x = 0; x < config.ActivityPlaylists.length; x++) {
                var playlist_item = config.ActivityPlaylists[x];
                if (name === playlist_item.Name) {
                    fount_at = x;
                    break;
                }
            }

            if (fount_at !== -1) {
                var item = config.ActivityPlaylists[fount_at];
                var activity_playlist_name = view.querySelector('#activity_playlist_name');
                var activity_playlist_type = view.querySelector('#activity_playlist_type');
                var activity_playlist_days = view.querySelector('#activity_playlist_days');
                var activity_playlist_size = view.querySelector('#activity_playlist_size');

                activity_playlist_name.value = item.Name;
                activity_playlist_type.value = item.Type;
                activity_playlist_days.value = item.Days;
                activity_playlist_size.value = item.Size;
            }
        });
    }

    function loadPage(view, config) {

        console.log("Settings Page Loaded With Config : " + JSON.stringify(config));
        var max_data_age_select = view.querySelector('#max_data_age_select');
        max_data_age_select.value = config.MaxDataAge;

        var backup_files_to_keep = view.querySelector('#files_to_keep');
        backup_files_to_keep.value = config.MaxBackupFiles;

        var backup_path_label = view.querySelector('#backup_path_label');
        backup_path_label.innerHTML = config.BackupPath;

        var activity_playlist_list = view.querySelector('#activity_playlist_list');
        while (activity_playlist_list.firstChild) {
            activity_playlist_list.removeChild(activity_playlist_list.firstChild);
        }
        config.ActivityPlaylists.forEach(function (playlist_item, index) {

            var tr = document.createElement("tr");
            tr.className = "detailTableBodyRow detailTableBodyRow-shaded";

            var td = document.createElement("td");
            td.appendChild(document.createTextNode(playlist_item.Name));
            td.style.cssText = "padding-left: 20px; padding-right: 20px;";
            tr.appendChild(td);

            td = document.createElement("td");
            td.appendChild(document.createTextNode(playlist_item.Type));
            td.style.cssText = "padding-left: 20px; padding-right: 20px;";
            tr.appendChild(td);

            td = document.createElement("td");
            td.appendChild(document.createTextNode(playlist_item.Days));
            td.style.cssText = "padding-left: 20px; padding-right: 20px;";
            tr.appendChild(td);

            td = document.createElement("td");
            td.appendChild(document.createTextNode(playlist_item.Size));
            td.style.cssText = "padding-left: 20px; padding-right: 20px;";
            tr.appendChild(td);

            td = document.createElement("td");
            td.style.cssText = "padding-left: 20px; padding-right: 20px;";
            var btn = document.createElement("BUTTON");
            var i = document.createElement("i");
            i.className = "md-icon";
            var t = document.createTextNode("remove");
            i.appendChild(t);
            btn.appendChild(i);
            btn.setAttribute("title", "Remove");
            btn.addEventListener("click", function () { remove_playlist_item(view, playlist_item.Name); });
            td.appendChild(btn);

            btn = document.createElement("BUTTON");
            i = document.createElement("i");
            i.className = "md-icon";
            t = document.createTextNode("edit");
            i.appendChild(t);
            btn.appendChild(i);
            btn.setAttribute("title", "Edit");
            btn.addEventListener("click", function () { edit_playlist_item(view, playlist_item.Name); });
            td.appendChild(btn);
            tr.appendChild(td);

            activity_playlist_list.appendChild(tr);
        });

    }

    function saveBackup(view) {
        var url = "user_usage_stats/save_backup?stamp=" + new Date().getTime();
        url = ApiClient.getUrl(url);
        ApiClient.getUserActivity(url).then(function (responce_message) {
            //alert("Loaded Data: " + JSON.stringify(usage_data));
            alert(responce_message[0]);

            ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {
                var backup_path_label = view.querySelector('#backup_path_label');
                backup_path_label.innerHTML = config.BackupPath;
            });

        });
    }

    function loadBackupFile(selectedFile, view) {
        var encoded_path = encodeURI(selectedFile); 
        var url = "user_usage_stats/load_backup?backupfile=" + encoded_path + "&stamp=" + new Date().getTime();
        url = ApiClient.getUrl(url);
        ApiClient.getUserActivity(url).then(function (responce_message) {
            //alert("Loaded Data Message : " + JSON.stringify(responce_message));
            alert(responce_message[0]);
        });
    }

    function showUserList(view) {

        var url = "user_usage_stats/user_list?stamp=" + new Date().getTime();
        url = ApiClient.getUrl(url);
        ApiClient.getUserActivity(url).then(function (user_list) {
            //alert("Loaded Data: " + JSON.stringify(user_list));
            var index = 0;
            var add_user_list = view.querySelector('#user_list_for_add');
            var options_html = "";
            var item_details;
            for (index = 0; index < user_list.length; ++index) {
                item_details = user_list[index];
                //if (item_details.in_list == false) {
                    options_html += "<option value='" + item_details.id + "'>" + item_details.name + "</option>";
                //}
            }
            add_user_list.innerHTML = options_html;

            var user_list_items = view.querySelector('#user_list_items');
            var list_html = "";
            for (index = 0; index < user_list.length; ++index) {
                item_details = user_list[index];
                if (item_details.in_list === true) {
                    list_html += "<li>" + item_details.name + "</li>";
                }
            }
            user_list_items.innerHTML = list_html;

        });
    }

    function remove_colour_item(view, colour_hash) {
        if (!confirm("Are you sure you want to remove this item? " + colour_hash)) {
            return;
        }

        var add_colour_button = view.querySelector('#add_colour_button');
        new_colour_text.value = colour_hash;
        original_colour = "";
        add_colour_button.innerHTML = "Add";

        ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {
            var index = config.ColourPalette.indexOf(colour_hash);
            while (index > -1) {
                console.log("Found item to remove : " + index);
                if (index > -1) {
                    config.ColourPalette.splice(index, 1);
                }
                index = config.ColourPalette.indexOf(colour_hash);
            }

            console.log("New Config Settings : " + JSON.stringify(config));
            ApiClient.updateNamedConfiguration('playback_reporting', config);
            setColourPalette(view, config);
        });

    }

    function move_colour_item(view, colour_hash, change) {

        ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {

            const index = config.ColourPalette.indexOf(colour_hash);
            console.log("Found item to remove : " + index);
            if (index > -1) {

                if (change < 0 && index === 0) {
                    return;
                }
                else if (change > 0 && index === config.ColourPalette.length - 1) {
                    return;
                }

                const temp_colour = config.ColourPalette[index + change];
                config.ColourPalette[index + change] = config.ColourPalette[index];
                config.ColourPalette[index] = temp_colour;
            }

            console.log("New Config Settings : " + JSON.stringify(config));
            ApiClient.updateNamedConfiguration('playback_reporting', config);
            setColourPalette(view, config);
        });

    }

    function edit_colour_item(view, colour_hash) {

        var new_colour_text = view.querySelector('#new_colour_text');
        var add_colour_button = view.querySelector('#add_colour_button');

        new_colour_text.value = colour_hash;

        if (original_colour === "" || original_colour !== colour_hash) {
            original_colour = colour_hash;
            add_colour_button.innerHTML = "Update";
        }
        else {
            original_colour = "";
            add_colour_button.innerHTML = "Add";
        }

        var event = new Event('input', {
            bubbles: true,
            cancelable: true
        });
        new_colour_text.dispatchEvent(event);
    }

    function setColourPalette(view, config) {

        console.log("setColourPalette");
        var colour_blocks = view.querySelector('#colour_blocks');

        while (colour_blocks.firstChild) {
            colour_blocks.removeChild(colour_blocks.firstChild);
        }

        var colour_list = config.ColourPalette;
        if (colour_list.length === 0) {
            colour_list = getDefautColours();
            config.ColourPalette = colour_list;
            ApiClient.updateNamedConfiguration('playback_reporting', config);
        }

        console.log("setColourPalette : " + JSON.stringify(colour_list));

        colour_list.forEach(function (colour_info, index) {
            var colour_span = document.createElement("SPAN");
            //colour_span.innerText = colour_info;
            colour_span.style = "text-align:center; width:75px; height:50px; display:inline-block; background:" + colour_info + ";";


            var del_txt = document.createElement("SPAN");
            del_txt.innerText = " - ";
            del_txt.style = "cursor: pointer;";
            del_txt.addEventListener("click", function () { remove_colour_item(view, colour_info); });

            var left_txt = document.createElement("SPAN");
            left_txt.innerText = " < ";
            left_txt.style = "cursor: pointer;";
            left_txt.addEventListener("click", function () { move_colour_item(view, colour_info, -1); });

            var right_txt = document.createElement("SPAN");
            right_txt.innerText = " > ";
            right_txt.style = "cursor: pointer;";
            right_txt.addEventListener("click", function () { move_colour_item(view, colour_info, 1); });

            var br = document.createElement("br");
            var colour_txt = document.createElement("SPAN");
            colour_txt.innerText = colour_info;
            colour_txt.style = "cursor: pointer;";
            colour_txt.addEventListener("click", function () { edit_colour_item(view, colour_info); });

            colour_span.appendChild(left_txt);
            colour_span.appendChild(del_txt);
            colour_span.appendChild(right_txt);
            colour_span.appendChild(br);
            colour_span.appendChild(colour_txt);


            //colour_span.addEventListener("click", function () { remove_item(view, colour_info); });

            colour_blocks.appendChild(colour_span);

        });
    }

    return function (view, params) {

        // init code here
        view.addEventListener('viewshow', function (e) {

            original_colour = "";
            mainTabsManager.setTabs(this, getTabIndex("playback_report_settings"), getTabs);

            var set_backup_path = view.querySelector('#set_backup_path');
            set_backup_path.addEventListener("click", setBackupPathPicker);

            var backup_data_now = view.querySelector('#backup_data_now');
            backup_data_now.addEventListener("click", function () { saveBackup(view); });

            var load_backup_data = view.querySelector('#load_backup_data');
            load_backup_data.addEventListener("click", loadBackupDataPicker);

            var max_data_age_select = view.querySelector('#max_data_age_select');
            max_data_age_select.addEventListener("change", setting_changed);

            var backup_files_to_keep = view.querySelector('#files_to_keep');
            backup_files_to_keep.addEventListener("change", files_to_keep_changed);

            var new_colour_text = view.querySelector('#new_colour_text');
            new_colour_text.addEventListener("input", colour_text_changed);
            var colour_test_block = view.querySelector('#colour_test_block');
            var add_colour_button = view.querySelector('#add_colour_button');
            add_colour_button.addEventListener("click", add_new_colour);

            var colour_slider_red = view.querySelector('#colour_slider_red');
            colour_slider_red.addEventListener("input", colour_slider_changed);
            var colour_slider_green = view.querySelector('#colour_slider_green');
            colour_slider_green.addEventListener("input", colour_slider_changed);
            var colour_slider_blue = view.querySelector('#colour_slider_blue');
            colour_slider_blue.addEventListener("input", colour_slider_changed);

            //playback activity lists
            var activity_playlist_add = view.querySelector('#activity_playlist_add');
            var activity_playlist_name = view.querySelector('#activity_playlist_name');
            var activity_playlist_type = view.querySelector('#activity_playlist_type');
            var activity_playlist_days = view.querySelector('#activity_playlist_days');
            var activity_playlist_size = view.querySelector('#activity_playlist_size');

            activity_playlist_add.addEventListener("click", function () {
                ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {

                    var new_playlist_name = activity_playlist_name.value.trim();
                    var newPlaylist_type = activity_playlist_type.options[activity_playlist_type.selectedIndex].value;
                    var new_playlist_days = parseInt(activity_playlist_days.value) || 0;
                    var new_playlist_size = parseInt(activity_playlist_size.value) || 0;

                    if (new_playlist_name === "" || new_playlist_days === 0 || new_playlist_size === 0) {
                        return;
                    }

                    var found_at = -1;
                    for (var x = 0; x < config.ActivityPlaylists.length; x++) {
                        var playlist_item = config.ActivityPlaylists[x];
                        if (new_playlist_name === playlist_item.Name) {
                            found_at = x;
                            break;
                        }
                    }

                    if (found_at !== -1) {
                        config.ActivityPlaylists[found_at] = { Name: new_playlist_name, Type: newPlaylist_type, Days: new_playlist_days, Size: new_playlist_size };
                    }
                    else {
                        config.ActivityPlaylists.push({ Name: new_playlist_name, Type: newPlaylist_type, Days: new_playlist_days, Size: new_playlist_size });
                    }

                    activity_playlist_name.value = "";
                    activity_playlist_type.value = "Movie";
                    activity_playlist_days.value = "";
                    activity_playlist_size.value = "";

                    console.log("New Config Settings : " + JSON.stringify(config));
                    ApiClient.updateNamedConfiguration('playback_reporting', config);
                    loadPage(view, config);
                });
            });

            function colour_text_changed() {

                var colour_hash = new_colour_text.value;
                //console.log("new_colour_text:" + colour_hash);

                if (colour_hash === "" || !colour_hash.startsWith("#") || colour_hash.length !== 7) {
                    return;
                }

                //console.log("new_colour_text: " + colour_hash.slice(1, 3) + " " + colour_hash.slice(3, 5) + " " + colour_hash.slice(5));

                var red_value = parseInt(colour_hash.slice(1, 3), 16);
                var green_value = parseInt(colour_hash.slice(3, 5), 16);
                var blue_value = parseInt(colour_hash.slice(5), 16);

                //console.log("new_colour_text: " + red_value + " " + green_value + " " + blue_value);

                colour_slider_red.value = red_value;
                colour_slider_green.value = green_value;
                colour_slider_blue.value = blue_value;

                colour_test_block.style.background = colour_hash;
            }

            function colour_slider_changed() {
                var red_int = parseInt(colour_slider_red.value);
                var green_int = parseInt(colour_slider_green.value);
                var blue_int = parseInt(colour_slider_blue.value);

                //console.log("colour_slider: " + red_int + " " + green_int + " " + blue_int);
                //console.log("colour_slider: " + red_int.toString(16) + " " + green_int.toString(16) + " " + blue_int.toString(16));

                var hex_red = ("0" + red_int.toString(16)).slice(-2);
                var hex_green = ("0" + green_int.toString(16)).slice(-2);
                var hex_blue = ("0" + blue_int.toString(16)).slice(-2);

                var colour_hash = "#" + hex_red + hex_green + hex_blue;
                colour_test_block.style.background = colour_hash;
                new_colour_text.value = colour_hash;
            }

            function add_new_colour() {
                ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {

                    const colour_hash = new_colour_text.value.toLowerCase().trim();
                    if (colour_hash === "" || colour_hash.length !== 7 || !colour_hash.startsWith("#")) {
                        return;
                    }
                    
                    if (original_colour !== "") {
                        const index = config.ColourPalette.indexOf(original_colour);
                        if (index > -1) {
                            config.ColourPalette[index] = colour_hash;
                        }
                        add_colour_button.innerHTML = "Add";
                        original_colour = "";
                    }
                    else {
                        const index = config.ColourPalette.indexOf(colour_hash);
                        if (index > -1) {
                            alert("Colour already in palette");
                            return;
                        }
                        config.ColourPalette.push(colour_hash);
                    }

                    console.log("New Config Settings : " + JSON.stringify(config));
                    ApiClient.updateNamedConfiguration('playback_reporting', config);
                    setColourPalette(view, config);
                });
            }

            function files_to_keep_changed() {
                var max_files = backup_files_to_keep.value;
                ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {
                    config.MaxBackupFiles = max_files;
                    console.log("New Config Settings : " + JSON.stringify(config));
                    ApiClient.updateNamedConfiguration('playback_reporting', config);
                });
            }

            function setting_changed() {
                var max_age = max_data_age_select.value;
                ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {
                    config.MaxDataAge = max_age;
                    console.log("New Config Settings : " + JSON.stringify(config));
                    ApiClient.updateNamedConfiguration('playback_reporting', config);
                });
            }

            function loadBackupDataPicker() {
                require(['directorybrowser'], function (directoryBrowser) {
                    var picker = new directoryBrowser();
                    picker.show({
                        includeFiles: true,
                        callback: function (selected) {
                            picker.close();
                            loadBackupFile(selected, view);
                        },
                        header: "Select backup file to load"
                    });
                });
            }

            function setBackupPathPicker() {
                require(['directorybrowser'], function (directoryBrowser) {
                    var picker = new directoryBrowser();
                    picker.show({
                        includeFiles: false,
                        callback: function (selected) {
                            picker.close();
                            setBackupPathCallBack(selected, view);
                        },
                        header: "Select backup path"
                    });
                });
            }

            // remove unknown users button
            var remove_unknown_button = view.querySelector('#remove_unknown_button');
            remove_unknown_button.addEventListener("click", function () {
                var url = "user_usage_stats/user_manage/remove_unknown/none" + "?stamp=" + new Date().getTime();
                url = ApiClient.getUrl(url);
                ApiClient.getUserActivity(url).then(function (result) {
                    alert("Activity with unknown users removed, " + result + " items removed.");
                });

            });            

            ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {
                loadPage(view, config);
            });

            // user list

            var add_button = view.querySelector('#add_user_to_list');
            add_button.addEventListener("click", function () {
                var add_user_list = view.querySelector('#user_list_for_add');
                var selected_user_id = add_user_list.options[add_user_list.selectedIndex].value;
                var url = "user_usage_stats/user_manage/add/" + selected_user_id + "?stamp=" + new Date().getTime();
                url = ApiClient.getUrl(url);
                ApiClient.getUserActivity(url).then(function (result) {
                    //alert(result);
                    showUserList(view);
                });
                
            });

            var remove_button = view.querySelector('#remove_user_from_list');
            remove_button.addEventListener("click", function () {
                var add_user_list = view.querySelector('#user_list_for_add');
                var selected_user_id = add_user_list.options[add_user_list.selectedIndex].value;
                var url = "user_usage_stats/user_manage/remove/" + selected_user_id + "?stamp=" + new Date().getTime();
                url = ApiClient.getUrl(url);
                ApiClient.getUserActivity(url).then(function (result) {
                    //alert(result);
                    showUserList(view);
                });
            });

            showUserList(view);

            ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {
                setColourPalette(view, config);
            });

        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});

