define([], function () {
    'use strict';

    function draw_graph(context, local_d3) {

        if (!local_d3) {
            alert("No D3 : " + local_d3);
            return;
        }

        var svg = local_d3.select("svg");
        var margin = { top: 20, right: 20, bottom: 30, left: 50 };
        var width = +svg.attr("width") - margin.left - margin.right;
        var height = +svg.attr("height") - margin.top - margin.bottom;
        var g = svg.append("g").attr("transform", "translate(" + margin.left + "," + margin.top + ")");

        var parseTime = local_d3.timeParse("%Y-%m-%d");

        var x = local_d3.scaleTime()
            .rangeRound([0, width]);

        var y = local_d3.scaleLinear()
            .rangeRound([height, 0]);

        var line = local_d3.line()
            .x(function (d) { return x(d.Date); })
            .y(function (d) { return y(d.Count); });

        local_d3.json("/emby/user_usage_stats/all/30/Activity", function (error, data) {
            if (error) throw error;

            for (var index = 0; index < data.length; ++index) {
                data[index].Date = parseTime(data[index].Date)
                console.log("Graph Data : " + data[index].Date + " - " + data[index].Count);
            }

            x.domain(local_d3.extent(data, function (d) { return d.Date; }));
            y.domain(local_d3.extent(data, function (d) { return d.Count; }));

            g.append("g")
                .attr("transform", "translate(0," + height + ")")
                .call(local_d3.axisBottom(x))
                .select(".domain")
                .remove();

            g.append("g")
                .call(local_d3.axisLeft(y))
                .append("text")
                .attr("fill", "#000")
                .attr("transform", "rotate(-90)")
                .attr("y", 6)
                .attr("dy", "0.71em")
                .attr("text-anchor", "end")
                .text("Play Counts");

            g.append("path")
                .datum(data)
                .attr("fill", "none")
                .attr("stroke", "steelblue")
                .attr("stroke-linejoin", "round")
                .attr("stroke-linecap", "round")
                .attr("stroke-width", 1.5)
                .attr("d", line);

            console.log("Graph Done");
        });
    }

    return function (view, params) {

        // init code here

        view.addEventListener('viewshow', function (e) {

            require([Dashboard.getConfigurationResourceUrl('d3.v4.min.js')], function (d3) {
                draw_graph(view, d3);
            });

        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});