using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Net;
using MediaBrowser.Common.Net;
using HomeRunTV.General_Helper;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.LiveTv;
using HomeRunTV.GuideData.Responses;
using HomeRunTV.GeneralHelpers;

namespace HomeRunTV.GuideData
{
    class SchedulesDirect:ITvGuideSupplier
    {
        public string username;
        public string lineup;
        private string password;
        private string token;
        private string apiUrl;
        private Dictionary<string, ScheduleDirect.Station> channelPair;

        public SchedulesDirect(string username, string password, string lineup)
        {
            this.username = username;
            this.password = password;
            this.lineup = lineup;
            apiUrl = "https://json.schedulesdirect.org/20140530";
        }
        public async Task getToken(HttpClientHelper httpHelper)
        {
            if (!(await getStatus(httpHelper)) && username.Length>0 && password.Length>0)
            {
                httpHelper.httpOptions = new HttpRequestOptions()
                {
                    Url = apiUrl + "/token",
                    UserAgent = "Emby-Server",
                    RequestContent = "{\"username\":\"" + username + "\",\"password\":\"" + password + "\"}",
                };
                httpHelper.useCancellationToken();
                httpHelper.logger.Info("[HomeRunTV] Obtaining token from Schedules Direct from addres: " + httpHelper.httpOptions.Url + " with body " + httpHelper.httpOptions.RequestContent);
                try
                {
                    Stream responce = await httpHelper.Post();
                    var root = httpHelper.jsonSerializer.DeserializeFromStream<ScheduleDirect.Token>(responce);
                    if (root.message == "OK") { token = root.token; httpHelper.logger.Info("[HomeRunTV] Authenticated with Schedules Direct token: " + token); }
                    else { httpHelper.logger.Error("[HomeRunTV] Could not authenticate with Schedules Direct Error: " + root.message); }
                }
                catch
                {
                    httpHelper.logger.Error("[HomeRunTV] Could not authenticate with Schedules Direct");
                }
            }
        }

        public async Task<bool> getStatus(HttpClientHelper httpHelper)
        {
            //await getToken(httpHelper); 
            httpHelper.httpOptions = new HttpRequestOptionsMod()
            {
                Url = apiUrl + "/status",
                UserAgent = "Emby-Server",
                Token = token
            };
            httpHelper.useCancellationToken();
            httpHelper.logger.Info("[HomeRunTV] Obtaining Client Status from Schedules Direct from addres: " + httpHelper.httpOptions.Url);
            try
            {
                Stream responce = await httpHelper.Get().ConfigureAwait(false);
                var root = httpHelper.jsonSerializer.DeserializeFromStream<ScheduleDirect.Status>(responce);
                if (root.systemStatus[0] != null) { httpHelper.logger.Info("[HomeRunTV] Schedules Direct status: " + root.systemStatus[0].status); }
                return true;
            }
            catch
            {
                httpHelper.logger.Error("[HomeRunTV] Error Determininig Schedule Direct Status");
            }
            return false;
        }

        public async Task<IEnumerable<ChannelInfo>> getChannelInfo(HttpClientHelper httpHelper, IEnumerable<ChannelInfo> channelsInfo)
        {
            await getToken(httpHelper);
            httpHelper.httpOptions = new HttpRequestOptionsMod()
            {
                Url = apiUrl + "/lineups/"+lineup,
                UserAgent = "Emby-Server",
                Token = token
            };
            channelPair = new Dictionary<string,ScheduleDirect.Station>();
            var response = await httpHelper.Get();
            var root = httpHelper.jsonSerializer.DeserializeFromStream<ScheduleDirect.Channel>(response);
            httpHelper.logger.Info("[HomeRunTV] Found "+root.map.Count()+" channels on the lineup on ScheduleDirect");
            foreach (ScheduleDirect.Map map in root.map)
            {
               
                channelPair.Add(map.channel,root.stations.First(item => item.stationID == map.stationID));
                httpHelper.logger.Info("[HomeRunTV] Added " + map.channel + " " + channelPair[map.channel].name + " " + channelPair[map.channel].stationID);
            }
            httpHelper.logger.Info("[HomeRunTV] Added " + channelPair.Count() + " channels to the dictionary");
            string channelName;
            foreach (ChannelInfo channel in channelsInfo){
                httpHelper.logger.Info("[HomeRunTV] Modifyin channel " + channel.Number);
                if (channelPair[channel.Number] != null) {
                    if(channelPair[channel.Number].logo != null){channel.ImageUrl= channelPair[channel.Number].logo.URL;channel.HasImage=true;}
                    if(channelPair[channel.Number].affiliate != null){channelName=channelPair[channel.Number].affiliate;}
                    else{channelName=channelPair[channel.Number].name;}
                    channel.Name = channelName;
                    //channel.Id = channelPair[channel.Number].stationID;
                }
            }
            return channelsInfo;
        }

        public async Task<IEnumerable<ProgramInfo>> getTvGuideForChannel(HttpClientHelper httpHelper, string channelNumber)
        {
            await getToken(httpHelper);
            httpHelper.httpOptions = new HttpRequestOptionsMod()
            {
                Url = apiUrl + "/schedules",
                UserAgent = "Emby-Server",
                Token = token
            };
            long id = 0;
            string stationID = channelPair[channelNumber].stationID;
            List<ScheduleDirect.RequestScheduleForChannel> requestList = new List<ScheduleDirect.RequestScheduleForChannel>() { new ScheduleDirect.RequestScheduleForChannel() { stationID = stationID, date = new List<string>() { "2015-03-23", "2015-03-24" } } };
            httpHelper.httpOptions.RequestContent = httpHelper.jsonSerializer.SerializeToString(requestList);
            var response = await httpHelper.Post();
            var root = httpHelper.jsonSerializer.DeserializeFromStream<List<ScheduleDirect.Schedules>>(response);
           // httpHelper.logger.Info("[HomeRunTV] Found " + root.Count() + " programs on "+channelNumber +" ScheduleDirect");
            List<ProgramInfo> programsInfo = new List<ProgramInfo>();
            foreach (ScheduleDirect.Program schedule in root[0].programs)
            {
               // httpHelper.logger.Info("[HomeRunTV] Proccesing Schedule for statio ID " +stationID+" which corresponds to channel" +channelNumber+" and program id "+ schedule.programID);
                
                programsInfo.Add(GetProgram(channelNumber, schedule,httpHelper.logger));
            }
            return programsInfo;
        }
        private ProgramInfo GetProgram(string channel, ScheduleDirect.Program programInfo,ILogger logger)
        {
            DateTime startAt = DateTime.Parse(programInfo.airDateTime);
         //   logger.Info("[HomeRunTV] Proccesing Schedule info" + startAt);
            DateTime endAt = startAt.AddSeconds(programInfo.duration);
          //  logger.Info("[HomeRunTV] Proccesing Schedule info" + endAt);
            ProgramAudio audioType = ProgramAudio.Mono;
            bool hdtv = false;
            bool repeat = (programInfo.@new == null);
            string newID = programInfo.programID + "T" + startAt.Ticks + "C" + channel;

            logger.Info("[HomeRunTV] Proccesing Schedule ID" + newID);
            if (programInfo.audioProperties != null) { if (programInfo.audioProperties.Exists(item => item == "stereo")) { audioType = ProgramAudio.Stereo; } else { audioType = ProgramAudio.Mono; } }
           // logger.Info("[HomeRunTV] Proccesing Schedule info" + audioType.ToString());
           // logger.Info("[HomeRunTV] Proccesing Schedule info video null " + (programInfo.videoProperties == null));
            if ((programInfo.videoProperties != null)) { hdtv = programInfo.videoProperties.Exists(item => item == "hdtv"); }
            
           // logger.Info("[HomeRunTV] Proccesing Schedule info" + hdtv.ToString());
            var info = new ProgramInfo
            {
                ChannelId = channel,
                Id = newID,
                Overview = "",
                StartDate = startAt,
                EndDate = endAt,
                Genres = null,
                OriginalAirDate = null,
                Name = "test",
                OfficialRating = null,
                CommunityRating = null,
                EpisodeTitle = null,
                Audio = audioType,
                IsHD = hdtv,
                IsRepeat = repeat,
                IsSeries = true,
                ImageUrl = null,
                HasImage = null,
                IsNews = false,
                IsMovie = false,
                IsKids = false,
                IsSports = false
            };

            return info;
        }
    }
}





