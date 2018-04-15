define(['libraryMenu'], function (libraryMenu) {
    'use strict';

    ApiClient.getUserActivity = function (url_to_get) {
        console.log("getUserActivity Url = " + url_to_get);
        return this.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };	

    function pickerCallBack(selectedDir, view) {
        ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {
            config.BackupPath = selectedDir;
            console.log("New Config Settings : " + JSON.stringify(config));
            ApiClient.updateNamedConfiguration('playback_reporting', config);

            var backup_path_label = view.querySelector('#backup_path_label');
            backup_path_label.innerHTML = selectedDir;
        });
    }

    function loadPage(view, config) {

        console.log("Settings Page Loaded With Config : " + JSON.stringify(config));
        var max_data_age_select = view.querySelector('#max_data_age_select');
        max_data_age_select.value = config.MaxDataAge;

        var backup_path_label = view.querySelector('#backup_path_label');
        backup_path_label.innerHTML = config.BackupPath;
    }

    function getTabs() {
        var tabs = [
            {
                href: Dashboard.getConfigurationPageUrl('user_playback_report'),
                name: 'User Playback Report'
            },
            {
                href: Dashboard.getConfigurationPageUrl('playback_report_settings'),
                name: 'Settings'
            }];
        return tabs;
    }

    function saveBackup(view) {
        var url = "/emby/user_usage_stats/save_backup?stamp=" + new Date().getTime();
        ApiClient.getUserActivity(url).then(function (responce_message) {
            //alert("Loaded Data: " + JSON.stringify(usage_data));
            alert(responce_message[0]);

            ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {
                var backup_path_label = view.querySelector('#backup_path_label');
                backup_path_label.innerHTML = config.BackupPath;
            });

        });
    }

    function loadBackup(view) {
        var url = "/emby/user_usage_stats/load_backup?stamp=" + new Date().getTime();
        ApiClient.getUserActivity(url).then(function (responce_message) {
            //alert("Loaded Data Message : " + JSON.stringify(responce_message));
            alert(responce_message[0]);
        });
    }

    return function (view, params) {

        // init code here
        view.addEventListener('viewshow', function (e) {

            libraryMenu.setTabs('playback_reporting', 1, getTabs);

            var set_backup_path = view.querySelector('#set_backup_path');
            set_backup_path.addEventListener("click", showFolderPicker);

            var backup_data_now = view.querySelector('#backup_data_now');
            backup_data_now.addEventListener("click", function () { saveBackup(view) });

            var load_backup_data = view.querySelector('#load_backup_data');
            load_backup_data.addEventListener("click", function () { loadBackup(view) });

            var max_data_age_select = view.querySelector('#max_data_age_select');
            max_data_age_select.addEventListener("change", setting_changed);

            function setting_changed() {
                var max_age = max_data_age_select.value;
                ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {
                    config.MaxDataAge = max_age;
                    console.log("New Config Settings : " + JSON.stringify(config));
                    ApiClient.updateNamedConfiguration('playback_reporting', config);
                });
            }

            function showFolderPicker() {
                require(['directorybrowser'], function (directoryBrowser) {
                    var picker = new directoryBrowser();
                    picker.show({
                        includeFiles: true,
                        callback: function (selected) {
                            picker.close();
                            pickerCallBack(selected, view);
                        },
                        header: "Select Backup Path"
                    });
                });
            }

            ApiClient.getNamedConfiguration('playback_reporting').then(function (config) {
                loadPage(view, config);
            });

        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});

