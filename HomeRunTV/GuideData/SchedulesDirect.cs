using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Globalization;
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
    class SchedulesDirect : ITvGuideSupplier
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

            if (!(await getStatus(httpHelper)) && username.Length > 0 && password.Length > 0)
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
        public async Task refreshToken(HttpClientHelper httpHelper)
        {

            if (username.Length > 0 && password.Length > 0)
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
                httpHelper.logger.Info("[HomeRunTV] Lineups on account");
                if (root.lineups != null)
                {
                    foreach (ScheduleDirect.Lineup lineup in root.lineups)
                    {
                        httpHelper.logger.Info("[HomeRunTV] Lineups ID: "+lineup.ID);
                    }
                }
                else
                {
                    httpHelper.logger.Info("[HomeRunTV] No lineups on account");
                }
                return true;
            }
            catch
            {
                httpHelper.logger.Error("[HomeRunTV] Error Determininig Schedule Direct Status");
                return false;
            }
            return false;
        }

        public async Task<IEnumerable<ChannelInfo>> getChannelInfo(HttpClientHelper httpHelper, IEnumerable<ChannelInfo> channelsInfo)
        {
            if (apiUrl != "https://json.schedulesdirect.org/20140530") { apiUrl = "https://json.schedulesdirect.org/20140530"; await refreshToken(httpHelper); }
            else { await getToken(httpHelper); }
            if (lineup != "Not Selected")
            {
                httpHelper.httpOptions = new HttpRequestOptionsMod()
                {
                    Url = apiUrl + "/lineups/" + lineup,
                    UserAgent = "Emby-Server",
                    Token = token
                };
                channelPair = new Dictionary<string, ScheduleDirect.Station>();
                var response = await httpHelper.Get();
                var root = httpHelper.jsonSerializer.DeserializeFromStream<ScheduleDirect.Channel>(response);
                httpHelper.logger.Info("[HomeRunTV] Found " + root.map.Count() + " channels on the lineup on ScheduleDirect");
                foreach (ScheduleDirect.Map map in root.map)
                {
                    channelPair.Add(map.channel, root.stations.First(item => item.stationID == map.stationID));
                    // httpHelper.logger.Info("[HomeRunTV] Added " + map.channel + " " + channelPair[map.channel].name + " " + channelPair[map.channel].stationID);
                }
                //httpHelper.logger.Info("[HomeRunTV] Added " + channelPair.Count() + " channels to the dictionary");
                string channelName;
                foreach (ChannelInfo channel in channelsInfo)
                {
                    //  httpHelper.logger.Info("[HomeRunTV] Modifyin channel " + channel.Number);
                    if (channelPair[channel.Number] != null)
                    {
                        if (channelPair[channel.Number].logo != null) { channel.ImageUrl = channelPair[channel.Number].logo.URL; channel.HasImage = true; }
                        if (channelPair[channel.Number].affiliate != null) { channelName = channelPair[channel.Number].affiliate; }
                        else { channelName = channelPair[channel.Number].name; }
                        channel.Name = channelName;
                        //channel.Id = channelPair[channel.Number].stationID;
                    }
                }
            }
            return channelsInfo;
        }

        public async Task<IEnumerable<ProgramInfo>> getTvGuideForChannel(HttpClientHelper httpHelper, string channelNumber, DateTime start, DateTime end)
        {
            if (lineup != "Not Selected")
            {

                if (apiUrl != "https://json.schedulesdirect.org/20141201") { apiUrl = "https://json.schedulesdirect.org/20141201"; await refreshToken(httpHelper); }
                else { await getToken(httpHelper); }
                HttpRequestOptionsMod httpOptions = new HttpRequestOptionsMod()
                {
                    Url = apiUrl + "/schedules",
                    UserAgent = "Emby-Server",
                    Token = token
                };

                httpHelper.httpOptions = httpOptions;
                List<string> dates = new List<string>();
                double numberOfDay = 0;
                DateTime lastEntry = start;
                while (lastEntry != end)
                {
                    lastEntry = start.AddDays(numberOfDay);
                    dates.Add(lastEntry.ToString("yyyy-MM-dd"));
                    numberOfDay++;
                }
                string stationID = channelPair[channelNumber].stationID;
                List<ScheduleDirect.RequestScheduleForChannel> requestList = new List<ScheduleDirect.RequestScheduleForChannel>() { 
                new ScheduleDirect.RequestScheduleForChannel() { 
                   stationID = stationID, date = dates } };
                httpHelper.logger.Info("[HomeRunTV] Request string for schedules is: " + httpHelper.jsonSerializer.SerializeToString(requestList));
                httpHelper.httpOptions.RequestContent = httpHelper.jsonSerializer.SerializeToString(requestList);
                var response = await httpHelper.Post();
                StreamReader reader = new StreamReader(response);
                string responseString = reader.ReadToEnd();
                responseString = "{ \"days\":" + responseString + "}";
                var root = httpHelper.jsonSerializer.DeserializeFromString<ScheduleDirect.Schedules>(responseString);
                // httpHelper.logger.Info("[HomeRunTV] Found " + root.Count() + " programs on "+channelNumber +" ScheduleDirect");
                List<ProgramInfo> programsInfo = new List<ProgramInfo>();
                httpOptions = new HttpRequestOptionsMod()
                {
                    Url = apiUrl + "/programs",
                    UserAgent = "Emby-Server",
                    Token = token
                };
                httpOptions.SetRequestHeader("Accept-Encoding", "deflate,gzip");
                httpHelper.httpOptions = httpOptions;
                string requestBody = "";
                List<string> programsID = new List<string>();
                List<string> imageID = new List<string>();
                foreach (ScheduleDirect.Day day in root.days)
                {
                    foreach (ScheduleDirect.Program schedule in day.programs)
                    {
                        programsID.Add(schedule.programID);
                        imageID.Add(schedule.programID.Substring(0, 10));
                    }
                }

                programsID = programsID.Distinct().ToList();
                imageID = imageID.Distinct().ToList();

                requestBody = "[\"" + string.Join("\", \"", programsID.ToArray()) + "\"]";
                httpHelper.httpOptions.RequestContent = requestBody;
                response = await httpHelper.Post();

                GZipStream ds = new GZipStream(response, CompressionMode.Decompress);
                reader = new StreamReader(ds);
                responseString = reader.ReadToEnd();
                responseString = "{ \"result\":" + responseString + "}";

                var programDetails = httpHelper.jsonSerializer.DeserializeFromString<ScheduleDirect.ProgramDetailsResilt>(responseString);
                Dictionary<string, ScheduleDirect.ProgramDetails> programDict = programDetails.result.ToDictionary(p => p.programID, y => y);

                /*
                httpOptions = new HttpRequestOptionsMod()
                {
                    Url = apiUrl + "https://json.schedulesdirect.org/20140530/metadata/programs/",
                    UserAgent = "Emby-Server",
                };              
                requestBody = "[\"" + string.Join("\", \"", imageID.ToArray()) + "\"]";
                httpHelper.httpOptions.RequestContent = requestBody;
                response = await httpHelper.Post();
                reader = new StreamReader(response);
                responseString = reader.ReadToEnd();
                responseString = "{ \"results\":" + responseString + "}";
            
                */

                foreach (ScheduleDirect.Day day in root.days)
                {
                    foreach (ScheduleDirect.Program schedule in day.programs)
                    {
                        // httpHelper.logger.Info("[HomeRunTV] Proccesing Schedule for statio ID " +stationID+" which corresponds to channel" +channelNumber+" and program id "+ schedule.programID);
                        programsInfo.Add(GetProgram(channelNumber, schedule, httpHelper.logger, programDict[schedule.programID]));
                    }
                }
                httpHelper.logger.Info("Finished with TVData");
                return programsInfo;
            }
            else
            {
                return (IEnumerable<ProgramInfo>)new List<ProgramAudio>();
            }
        }
        private ProgramInfo GetProgram(string channel, ScheduleDirect.Program programInfo, ILogger logger, ScheduleDirect.ProgramDetails details)
        {
            DateTime startAt = DateTime.ParseExact(programInfo.airDateTime, "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'", CultureInfo.InvariantCulture);
            //logger.Info("Date enter as: " + programInfo.airDateTime + " Date read as: " + startAt);
            //   logger.Info("[HomeRunTV] Proccesing Schedule info" + startAt);
            DateTime endAt = startAt.AddSeconds(programInfo.duration);
            //  logger.Info("[HomeRunTV] Proccesing Schedule info" + endAt);
            ProgramAudio audioType = ProgramAudio.Mono;
            bool hdtv = false;
            bool repeat = (programInfo.@new == null);
            string newID = programInfo.programID + "T" + startAt.Ticks + "C" + channel;

            //logger.Info("[HomeRunTV] Proccesing Schedule ID" + newID);
            if (programInfo.audioProperties != null) { if (programInfo.audioProperties.Exists(item => item == "stereo")) { audioType = ProgramAudio.Stereo; } else { audioType = ProgramAudio.Mono; } }
            // logger.Info("[HomeRunTV] Proccesing Schedule info" + audioType.ToString());
            // logger.Info("[HomeRunTV] Proccesing Schedule info video null " + (programInfo.videoProperties == null));
            if ((programInfo.videoProperties != null)) { hdtv = programInfo.videoProperties.Exists(item => item == "hdtv"); }

            // logger.Info("[HomeRunTV] Proccesing Schedule info" + hdtv.ToString());
            string desc = "";
            if (details.descriptions != null)
            {
                if (details.descriptions.description1000 != null) { desc = details.descriptions.description1000[0].description; }
                else if (details.descriptions.description100 != null) { desc = details.descriptions.description100[0].description; }
            }
            ScheduleDirect.Gracenote gracenote;
            string EpisodeTitle = "";
            if (details.metadata != null)
            {
                gracenote = details.metadata.Find(x => x.Gracenote != null).Gracenote;
                if (details.eventDetails.subType == "Series") { EpisodeTitle = "Season: " + gracenote.season + " Episode: " + gracenote.episode; }
                if (details.episodeTitle150 != null) { EpisodeTitle = EpisodeTitle+" "+details.episodeTitle150; }
            }
            if (details.episodeTitle150 != null) { EpisodeTitle = EpisodeTitle + " " + details.episodeTitle150; }
            DateTime date;
            bool hasImage = false;
            //if (details.hasImageArtwork != null) { hasImage = true; }
            var info = new ProgramInfo
            {
                ChannelId = channel,
                Id = newID,
                Overview = desc,
                StartDate = startAt,
                EndDate = endAt,
                Genres = new List<string>(){"N/A"},
                Name = details.titles[0].title120 ?? "Unkown",
                OfficialRating = "0",
                CommunityRating = null,
                EpisodeTitle = EpisodeTitle,
                Audio = audioType,
                IsHD = hdtv,
                IsRepeat = repeat,
                IsSeries = (details.eventDetails.subType == "Series"),
                ImageUrl = "",
                HasImage = false,
                IsNews = false,
                IsKids = false,
                IsSports = false,
                IsLive = false,
                IsMovie = false,
                IsPremiere = false,
                
            };
            //logger.Info("Done init");
            if (null != details.originalAirDate)
            {
                info.OriginalAirDate = DateTime.Parse(details.originalAirDate);
            }

            if (details.genres != null)
            {
                info.Genres = details.genres.Where(g => !string.IsNullOrWhiteSpace(g)).ToList();
                info.IsNews = details.genres.Contains("news", StringComparer.OrdinalIgnoreCase);
                info.IsMovie = details.genres.Contains("Feature Film", StringComparer.OrdinalIgnoreCase) || (details.movie != null); 
                info.IsKids = false;
                info.IsSports = details.genres.Contains("sports", StringComparer.OrdinalIgnoreCase) ||
                    details.genres.Contains("Sports non-event", StringComparer.OrdinalIgnoreCase) ||
                    details.genres.Contains("Sports event", StringComparer.OrdinalIgnoreCase) ||
                    details.genres.Contains("Sports talk", StringComparer.OrdinalIgnoreCase) ||
                    details.genres.Contains("Sports news", StringComparer.OrdinalIgnoreCase);
            }
            return info;
        }
        public bool checkExist(object obj)
        {
            if (obj != null)
            {
                return true;
            }
            return false;
        }
        public async Task<string> getLineups(HttpClientHelper httpHelper)
        {
            if (apiUrl != "https://json.schedulesdirect.org/20141201") { apiUrl = "https://json.schedulesdirect.org/20140530"; await refreshToken(httpHelper); }
            httpHelper.httpOptions = new HttpRequestOptionsMod()
            {
                Url = apiUrl + "/status",
                UserAgent = "Emby-Server",
                Token = token
            };
            httpHelper.useCancellationToken();
            string Lineups ="";
            try
            {
                Stream responce = await httpHelper.Get().ConfigureAwait(false);
                var root = httpHelper.jsonSerializer.DeserializeFromStream<ScheduleDirect.Status>(responce);

                if (root.systemStatus[0] != null) { httpHelper.logger.Info("[HomeRunTV] Schedules Direct status: " + root.systemStatus[0].status); }
                httpHelper.logger.Info("[HomeRunTV] Lineups on account");
                if (root.lineups != null)
                {
                    foreach (ScheduleDirect.Lineup lineup in root.lineups)
                    {
                        httpHelper.logger.Info("[HomeRunTV] Lineups ID: " + lineup.ID);
                        if (String.IsNullOrWhiteSpace(Lineups))
                        {
                            Lineups = lineup.ID;
                        }
                        else { Lineups = Lineups + "," + lineup.ID; }
                    }
                }
                else
                {
                    httpHelper.logger.Info("[HomeRunTV] No lineups on account");
                }
            }
            catch
            {
                httpHelper.logger.Error("[HomeRunTV] Error Determininig Schedule Direct Status");
                return Lineups;
            }
            return Lineups;
        }
    }
}





