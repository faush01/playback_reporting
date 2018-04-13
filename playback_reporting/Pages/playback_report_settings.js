define(['libraryMenu'], function (libraryMenu) {
    'use strict';

    function loadPage(view, config) {

        console.log("Settings Page Loaded With Config : " + JSON.stringify(config));
        var max_data_age_select = view.querySelector('#max_data_age_select');
        max_data_age_select.value = config.MaxDataAge;
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

