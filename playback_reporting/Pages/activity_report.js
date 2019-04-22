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

define(['libraryMenu', Dashboard.getConfigurationResourceUrl('helper_function.js')], function (libraryMenu) {
    'use strict';

    ApiClient.getActivity = function (url_to_get) {
        console.log("getActivity Url = " + url_to_get);
        return this.ajax({
            type: "GET",
            url: url_to_get,
            dataType: "json"
        });
    };

    function displayTime(ticks) {
        var ticksInSeconds = ticks / 10000000;
        var hh = Math.floor(ticksInSeconds / 3600);
        var mm = Math.floor((ticksInSeconds % 3600) / 60);
        var ss = Math.floor(ticksInSeconds % 60);

        return pad(hh, 2) + ":" + pad(mm, 2) + ":" + pad(ss, 2);
    }

    function pad(n, width) {
        n = n + '';
        return n.length >= width ? n : new Array(width - n.length + 1).join('0') + n;
    }

    return function (view, params) {

        // init code here
        view.addEventListener('viewshow', function (e) {

            libraryMenu.setTabs('playback_reporting', getTabIndex("activity_report"), getTabs);

            var style = document.createElement('style');
            style.innerHTML =
                '.tooltip {position: relative;display: inline-block;border-bottom: 1px dotted black;} ' +
                '.tooltip .tooltiptext {visibility: hidden; background-color: black; color: #fff; border-radius: 6px; padding: 5px 0; position: absolute;z-index: 1;} ' +
                '.tooltip:hover .tooltiptext {visibility: visible;}';
            var ref = document.querySelector('script');
            ref.parentNode.insertBefore(style, ref);


            process_click();

            function process_click() {

                var url = "user_usage_stats/session_list?stamp=" + new Date().getTime();
                url = ApiClient.getUrl(url);

                ApiClient.getActivity(url).then(function (activity_data) {

                    console.log("activity_data: " + JSON.stringify(activity_data));

                    var table_body = view.querySelector('#activity_report_results');
                    var row_html = "";

                    for (var index = 0; index < activity_data.length; ++index) {
                        var activity_info = activity_data[index];

                        row_html += "<tr class='detailTableBodyRow detailTableBodyRow-shaded'>";

                        var device_image = activity_info.app_icon;
                        row_html += "<td><img src='" + device_image + "' width='40px'></td>";

                        row_html += "<td>" + activity_info.device_name + "</td>";
                        row_html += "<td>" + activity_info.client_name + " (" + activity_info.app_version + ")</td>";

                        var user_image = "css/images/logindefault.png";
                        if (activity_info.has_image) {
                            user_image = "Users/" + activity_info.user_id + "/Images/Primary?width=50";
                            user_image = ApiClient.getUrl(user_image);
                        } 
                        row_html += "<td><img src='" + user_image + "' style='height:2.9em;border-radius:60px;margin-right:.5em;'></td>";

                        /*
                        var test_table = "<table>";
                        for (var test_x = 0; test_x < 20; test_x++) {
                            test_table += "<tr><td nowrap align='left'>Video Direct " + test_x + "</td><td nowrap align='left'>Some Test Data</td></tr>";
                        }
                        test_table += "</table>";
                        row_html += "<td><div class='tooltip'>" + activity_info.user_name + "<span class='tooltiptext'>" + test_table + "</span></div></td>";
                        */
                        row_html += "<td>" + activity_info.user_name + "</td>";

                        if (activity_info.NowPlayingItem) {

                            var item_name = activity_info.NowPlayingItem.Name;
                            if (activity_info.NowPlayingItem.Type === "Episode") {
                                item_name = activity_info.NowPlayingItem.SeriesName + " ";
                                item_name += "s" + pad(activity_info.NowPlayingItem.ParentIndexNumber, 2);
                                item_name += "e" + pad(activity_info.NowPlayingItem.IndexNumber, 2);
                                item_name += " " + activity_info.NowPlayingItem.Name;
                            }
                            row_html += "<td>" + item_name + "</td>";

                            var complete_percentage = (activity_info.PlayState.PositionTicks / activity_info.NowPlayingItem.RunTimeTicks) * 100;
                            complete_percentage = Math.round(complete_percentage);
                            var duration = displayTime(activity_info.NowPlayingItem.RunTimeTicks);
                            var current = displayTime(activity_info.PlayState.PositionTicks);
                            row_html += "<td>" + current + " / " + duration + " (" + complete_percentage + "%)</td>";

                            var play_method_details = "";
                            if (activity_info.PlayState.PlayMethod === "Transcode") {
                                play_method_details += "<table>";
                                play_method_details += "<tr><td nowrap align='left'>Video Direct</td><td nowrap align='left'>" + activity_info.TranscodingInfo.IsVideoDirect + "</td></tr>";
                                play_method_details += "<tr><td nowrap align='left'>Video Codec</td><td nowrap align='left'>" + activity_info.TranscodingInfo.VideoCodec + "</td></tr>";
                                play_method_details += "<tr><td nowrap align='left'>Video Size</td><td nowrap align='left'>" + activity_info.TranscodingInfo.Width + "x" + activity_info.TranscodingInfo.Height + "</td></tr>";
                                
                                if (activity_info.TranscodingInfo.VideoEncoderIsHardware) {
                                    play_method_details += "<tr><td nowrap align='left'>Video Encoder</td><td nowrap align='left'>" + activity_info.TranscodingInfo.VideoEncoderHwAccel + "</td></tr>";
                                    play_method_details += "<tr><td nowrap align='left'>Encoder Media</td><td nowrap align='left'>" + activity_info.TranscodingInfo.VideoEncoderMediaType + "</td></tr>";
                                }
                                else if (activity_info.TranscodingInfo.IsVideoDirect === false) {
                                    play_method_details += "<tr><td nowrap align='left'>Video Encoder</td><td nowrap align='left'>Software</td></tr>";
                                }

                                if (activity_info.TranscodingInfo.VideoDecoderIsHardware ) {
                                    play_method_details += "<tr><td nowrap align='left'>Video Decoder</td><td nowrap align='left'>" + activity_info.TranscodingInfo.VideoDecoderHwAccel + "</td></tr>";
                                    play_method_details += "<tr><td nowrap align='left'>Decoder Media</td><td nowrap align='left'>" + activity_info.TranscodingInfo.VideoDecoderMediaType + "</td></tr>";


                                }
                                else if (activity_info.TranscodingInfo.IsVideoDirect === false) {
                                    play_method_details += "<tr><td nowrap align='left'>Video Decoder</td><td nowrap align='left'>Software</td></tr>";
                                }

                                play_method_details += "<tr><td nowrap align='left'>Audio Direct</td><td nowrap align='left'>" + activity_info.TranscodingInfo.IsAudioDirect + "</td></tr>";
                                play_method_details += "<tr><td nowrap align='left'>Audio Codec</td><td nowrap align='left'>" + activity_info.TranscodingInfo.AudioCodec + "</td></tr>";
                                play_method_details += "<tr><td nowrap align='left'>Audio Channels</td><td nowrap align='left'>" + activity_info.TranscodingInfo.AudioChannels + "</td></tr>";

                                play_method_details += "<tr><td nowrap align='left'>Container</td><td nowrap align='left'>" + activity_info.TranscodingInfo.Container + "</td></tr>";
                                play_method_details += "<tr><td nowrap align='left'>Bitrate</td><td nowrap align='left'>" + activity_info.TranscodingInfo.Bitrate + "</td></tr>";

                                if (activity_info.TranscodingInfo.Framerate) {
                                    play_method_details += "<tr><td nowrap align='left'>Transcode FPS</td><td nowrap align='left'>" + activity_info.TranscodingInfo.Framerate + "</td></tr>";
                                }

                                if (activity_info.TranscodingInfo.TranscodingPositionTicks) {
                                    play_method_details += "<tr><td nowrap align='left'>Transcode State</td><td nowrap align='left'>" +
                                        displayTime(activity_info.TranscodingInfo.TranscodingPositionTicks) + "</td></tr>";
                                }
                                else {
                                    play_method_details += "<tr><td nowrap align='left'>Transcode State</td><td nowrap align='left'>Finished</td></tr>";
                                }

                                play_method_details += "</table>";
                            }

                            row_html += "<td><div class='tooltip'>" + activity_info.PlayState.PlayMethod + "<span class='tooltiptext'>" + play_method_details + "</span></div></td>";
                        }
                        else {
                            row_html += "<td>&nbsp;</td>";
                            row_html += "<td>&nbsp;</td>";
                            row_html += "<td>&nbsp;</td>";
                        }

                        row_html += "<td>" + activity_info.last_active + "</td>";
                        row_html += "<td>" + activity_info.remote_address + "</td>";

                        row_html += "</tr>";
                    }

                    table_body.innerHTML = row_html;

                });
            }
        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});