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

define(['mainTabsManager', 'appRouter', 'emby-linkbutton', Dashboard.getConfigurationResourceUrl('helper_function.js')], function (mainTabsManager, appRouter) {
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

            mainTabsManager.setTabs(this, getTabIndex("activity_report"), getTabs);

            var style = document.createElement('style');
            style.innerHTML =
                '.tooltip {position: relative;display: inline-block;border-bottom: 1px dotted black;} ' +
                '.tooltip .tooltiptext {visibility: hidden; background-color: black; color: #fff; border-radius: 6px; padding: 5px 0; position: absolute;z-index: 1;} ' +
                '.tooltip:hover .tooltiptext {visibility: visible;} ' +
                '.info_cell {white-space: nowrap; padding-left:45px; padding-right:20px; font-size:smaller;}' +
                '.info_cell_heading {white-space: nowrap; padding-left:20px; padding-right:20px;font-size:smaller;}';
            var ref = document.querySelector('script');
            ref.parentNode.insertBefore(style, ref);

            process_click();

            function process_click() {

                var url = "user_usage_stats/session_list?stamp=" + new Date().getTime();
                url = ApiClient.getUrl(url);

                var load_status = view.querySelector('#activity_report_status');
                load_status.innerHTML = "Loading Data...";

                ApiClient.getActivity(url).then(function (activity_data) {
                    load_status.innerHTML = "&nbsp;";
                    console.log("activity_data: " + JSON.stringify(activity_data));

                    var table_body = view.querySelector('#activity_report_results');
                    var row_html = "";

                    for (var index = 0; index < activity_data.length; ++index) {
                        var activity_info = activity_data[index];

                        var row_bg_col = "#99999900";
                        if (index % 2 == 0) {
                            row_bg_col = "#9999991c";
                        }

                        row_html += "<tr style='background:" + row_bg_col + ";'>";

                        // add user info
                        var user_image = "<i class='md-icon' style='font-size:30px;'></i>";
                        if (activity_info.has_image) {
                            var user_img = "Users/" + activity_info.user_id + "/Images/Primary?height=152&&quality=90";
                            user_img = ApiClient.getUrl(user_img);
                            user_image = "<img src='" + user_img + "' style='object-fit:cover;width:30px;height:30px;border-radius:1000px;vertical-align:top;'>";
                        }
                        row_html += "<td>";
                        row_html += "<table>";
                        row_html += "<tr>";
                        row_html += "<td style='vertical-align: middle; width:35px;' align='center'>" + user_image + "</td>";
                        row_html += "<td style='vertical-align: middle;'>" + activity_info.user_name + "</td>";
                        row_html += "</tr>";
                        row_html += "</table>";
                        row_html += "</td>";

                        // add device info
                        row_html += "<td>";
                        row_html += "<table style='line-height: 1; font-size: 80%;'>";
                        row_html += "<tr>";
                        if (activity_info.app_icon) {
                            row_html += "<td rowspan='2'><img src='" + activity_info.app_icon + "' width='30px'></td>";
                        }
                        else {
                            row_html += "<td rowspan='2'><img src='' width='30px'></td>";
                        }
                        row_html += "<td>" + activity_info.device_name + "</td>";
                        row_html += "</tr>";
                        row_html += "<tr>";
                        row_html += "<td>" + activity_info.client_name + " (" + activity_info.app_version + ")</td>";
                        row_html += "</tr>";
                        row_html += "</table>";
                        row_html += "</td>";



                        // add now playing info
                        if (activity_info.NowPlayingItem) {

                            // add item name
                            var item_name = activity_info.NowPlayingItem.Name;
                            if (activity_info.NowPlayingItem.Type === "Episode") {
                                item_name = activity_info.NowPlayingItem.SeriesName + " ";
                                item_name += "s" + pad(activity_info.NowPlayingItem.ParentIndexNumber, 2);
                                item_name += "e" + pad(activity_info.NowPlayingItem.IndexNumber, 2);
                                item_name += " " + activity_info.NowPlayingItem.Name;
                            }

                            // add playback item info
                            var complete_percentage = (activity_info.PlayState.PositionTicks / activity_info.NowPlayingItem.RunTimeTicks) * 100;
                            complete_percentage = Math.round(complete_percentage);
                            var duration = displayTime(activity_info.NowPlayingItem.RunTimeTicks);
                            var current = displayTime(activity_info.PlayState.PositionTicks);

                            var name_link = appRouter.getRouteUrl({ Id: activity_info.NowPlayingItem.Id, ServerId: ApiClient._serverInfo.Id });
                            var item_link = "<a href='" + name_link + "' is='emby-linkbutton' class='button-link' title='View Emby item'>" + item_name + "</a>";

                            var direct_name_link = "/web/index.html#!/item?id=" + activity_info.NowPlayingItem.Id + "&serverId=" + ApiClient._serverInfo.Id;
                            var new_window = "<i class='md-icon' style='cursor: pointer; font-size:100%;' onClick='window.open(\"" + direct_name_link + "\");' title='Open Emby item in new window'>launch</i>"

                            var item_name_link = item_link + "&nbsp;&nbsp;" + new_window;

                            row_html += "<td>";
                            row_html += item_name_link;
                            row_html += "<br />";
                            row_html += complete_percentage + "% (" + current + " / " + duration + ")";
                            row_html += "</td>";

                            // add playback details
                            var play_method_details = "";
                            play_method_details += "<table cellpadding='0' cellspacing='0'>";

                            if (activity_info.NowPlayingItem.MediaStreams && activity_info.NowPlayingItem.MediaStreams.length > 0) {
                                for (var media_index = 0; media_index < activity_info.NowPlayingItem.MediaStreams.length; media_index++) {
                                    var media = activity_info.NowPlayingItem.MediaStreams[media_index];
                                    if (media.Type === "Video") {
                                        play_method_details += "<tr><td class='info_cell_heading'>Original Video</td></tr>";
                                        play_method_details += "<tr><td class='info_cell'>Codec: " + media.Codec + "</td></tr>";
                                        play_method_details += "<tr><td class='info_cell'>Size: " + media.Width + "x" + media.Height + "</td></tr>";
                                        play_method_details += "<tr><td class='info_cell'>Framerate: " + media.RealFrameRate + "</td></tr>";
                                        play_method_details += "<tr><td class='info_cell'>Aspect Ratio: " + media.AspectRatio + "</td></tr>";
                                        play_method_details += "<tr><td class='info_cell'>Bitrate: " + media.BitRate + "</td></tr>";
                                        play_method_details += "<tr><td class='info_cell'>Interlaced: " + media.IsInterlaced + "</td></tr>";
                                    }
                                    if (media.Type === "Audio") {
                                        play_method_details += "<tr><td class='info_cell_heading'>Original Audio</td></tr>";
                                        play_method_details += "<tr><td class='info_cell'>Codec: " + media.Codec + "</td></tr>";
                                        play_method_details += "<tr><td class='info_cell'>Language: " + media.DisplayLanguage + "</td></tr>";
                                        play_method_details += "<tr><td class='info_cell'>Channels: " + media.Channels + "</td></tr>";
                                    }
                                }
                            }

                            if (activity_info.TranscodingInfo) {

                                play_method_details += "<tr><td class='info_cell_heading'>Transcoded Video</td></tr>";
                                play_method_details += "<tr><td class='info_cell'>Direct: " + activity_info.TranscodingInfo.IsVideoDirect + "</td></tr>";
                                play_method_details += "<tr><td class='info_cell'>Codec: " + activity_info.TranscodingInfo.VideoCodec + "</td></tr>";
                                play_method_details += "<tr><td class='info_cell'>Size: " + activity_info.TranscodingInfo.Width + "x" + activity_info.TranscodingInfo.Height + "</td></tr>";

                                if (activity_info.TranscodingInfo.VideoEncoderIsHardware) {
                                    var video_encoder_info = activity_info.TranscodingInfo.VideoEncoderHwAccel + " - " + activity_info.TranscodingInfo.VideoEncoderMediaType;
                                    play_method_details += "<tr><td class='info_cell'>Encoder: " + video_encoder_info + "</td></tr>";
                                }
                                else if (activity_info.TranscodingInfo.IsVideoDirect === false) {
                                    play_method_details += "<tr><td class='info_cell'>Encoder: Software</td></tr>";
                                }

                                if (activity_info.TranscodingInfo.VideoDecoderIsHardware) {
                                    var audio_encoder_info = activity_info.TranscodingInfo.VideoDecoderHwAccel + " - " + activity_info.TranscodingInfo.VideoDecoderMediaType;
                                    play_method_details += "<tr><td class='info_cell'>Decoder: " + audio_encoder_info + "</td></tr>";
                                }
                                else if (activity_info.TranscodingInfo.IsVideoDirect === false) {
                                    play_method_details += "<tr><td class='info_cell'>Decoder: Software</td></tr>";
                                }

                                play_method_details += "<tr><td class='info_cell_heading'>Transcoded Audio</td></tr>";
                                play_method_details += "<tr><td class='info_cell'>Direct: " + activity_info.TranscodingInfo.IsAudioDirect + "</td></tr>";
                                play_method_details += "<tr><td class='info_cell'>Codec: " + activity_info.TranscodingInfo.AudioCodec + "</td></tr>";
                                play_method_details += "<tr><td class='info_cell'>Channels: " + activity_info.TranscodingInfo.AudioChannels + "</td></tr>";

                                play_method_details += "<tr><td class='info_cell_heading'>Transcode Info</td></tr>";
                                play_method_details += "<tr><td class='info_cell'>Container: " + activity_info.TranscodingInfo.Container + "</td></tr>";
                                play_method_details += "<tr><td class='info_cell'>Bitrate: " + activity_info.TranscodingInfo.Bitrate + "</td></tr>";

                                if (activity_info.TranscodingInfo.Framerate) {
                                    play_method_details += "<tr><td class='info_cell'>Speed: " + activity_info.TranscodingInfo.Framerate + " fps</td></tr>";
                                }

                                if (activity_info.TranscodingInfo.TranscodingPositionTicks) {

                                    var trans_complete_percentage = (activity_info.TranscodingInfo.TranscodingPositionTicks / activity_info.NowPlayingItem.RunTimeTicks) * 100;
                                    trans_complete_percentage = Math.round(trans_complete_percentage);
                                    var trans_duration = displayTime(activity_info.NowPlayingItem.RunTimeTicks);
                                    var trans_current = displayTime(activity_info.TranscodingInfo.TranscodingPositionTicks);

                                    play_method_details += "<tr><td class='info_cell'>Position: " +
                                        trans_complete_percentage + "% (" + trans_current + " / " + trans_duration + ")</td></tr>";
                                }
                                else {
                                    play_method_details += "<tr><td class='info_cell'>Position: Finished</td></tr>";
                                }

                                if (activity_info.TranscodingInfo.TranscodeReasons && activity_info.TranscodingInfo.TranscodeReasons.length > 0) {

                                    play_method_details += "<tr><td class='info_cell_heading'>Transcode Reasons</td></tr>";
                                    for (var reason_index = 0; reason_index < activity_info.TranscodingInfo.TranscodeReasons.length; reason_index++) {
                                        play_method_details += "<tr><td class='info_cell'>" + activity_info.TranscodingInfo.TranscodeReasons[reason_index] + "</td></tr>";
                                    }
                                }
                            }

                            play_method_details += "</table>";
                            row_html += "<td>";
                            row_html += "<div class='tooltip'>";
                            row_html += activity_info.PlayState.PlayMethod;
                            row_html += "<br />";
                            row_html += "<span class='tooltiptext'>" + play_method_details + "</span>";
                            row_html += "</div>";
                            row_html += "</td>";
                        }
                        else {
                            row_html += "<td>&nbsp;</td>";
                            row_html += "<td>&nbsp;</td>";
                        }

                        row_html += "<td>";
                        row_html += activity_info.last_active;
                        row_html += "<br />";
                        row_html += activity_info.remote_address;
                        row_html += "</td>";

                        row_html += "</tr>";
                    }

                    table_body.innerHTML = row_html;

                }, function (response) { load_status.innerHTML = response.status + ":" + response.statusText; });
            }
        });

        view.addEventListener('viewhide', function (e) {

        });

        view.addEventListener('viewdestroy', function (e) {

        });
    };
});