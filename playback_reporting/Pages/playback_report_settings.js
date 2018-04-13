define(['libraryMenu'], function (libraryMenu) {
    'use strict';

    function loadPage(view, config) {

        console.log(JSON.stringify(config));
        var max_data_age_select = document.getElementById('max_data_age_select');  
        max_data_age_select.value = config.MaxDataAge;

    }

    function setting_changed() {

        var max_data_age_select = document.getElementById('max_data_age_select');  
        var max_age = max_data_age_select.value;
        console.log("Max Age: " + max_age);

        ApiClient.getPluginConfiguration('9e6eb40f-9a1a-4ca1-a299-62b4d252453e').then(function (config) {
            console.log("Selection Changed");

            config.MaxDataAge = max_age;

            ApiClient.updatePluginConfiguration('9e6eb40f-9a1a-4ca1-a299-62b4d252453e', config);

        });
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

    return function (view, params) {

        // init code here
        view.addEventListener('viewshow', function (e) {

            libraryMenu.setTabs('playback_reporting', 1, getTabs);

            var max_data_age_select = document.getElementById('max_data_age_select');           
            max_data_age_select.addEventListener("change", setting_changed);

            ApiClient.getPluginConfiguration('9e6eb40f-9a1a-4ca1-a299-62b4d252453e').then(function (config) {
                loadPage(view, config);
            });

        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});

