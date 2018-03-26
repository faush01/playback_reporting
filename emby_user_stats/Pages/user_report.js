define([], function () {
    'use strict';
	
    ApiClient.getUserActivity = function (url_to_get) {
        return this.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };	
	
    function populate_report(context, usage_data) {

        if (!usage_data) {
            alert("No Data!");
            return;
        }

        console.log("Processing User Report: " + JSON.stringify(usage_data));

        var table = document.getElementById("user_usage_report_table");

        for (var index = 0; index < usage_data.length; ++index) {

            var item_details = usage_data[index];
            var item_name = item_details.Name;

            var row = table.insertRow(0);
            var cell1 = row.insertCell(0);
            cell1.innerHTML = item_name;

            console.log("Processing User Report ADDING: " + item_name);
        }

	}
	
	
    return function (view, params) {

        // init code here

        view.addEventListener('viewshow', function (e) {

            var url = new URL(window.location.href);
            var user_id = url.searchParams.get("user");
            var date = url.searchParams.get("date");
            var url_to_get = "/emby/user_usage_stats/" + user_id + "/" + date + "/GetItems"
            console.log("User Report Details Url: " + url_to_get);

            ApiClient.getUserActivity(url_to_get).then(function (usage_data) {
				//alert("Loaded Data: " + JSON.stringify(usage_data));
				populate_report(view, usage_data);
			});

        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});	
	
	