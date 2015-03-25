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


namespace HomeRunTV.TunerHelpers
{
    public class TunerServer
    {
        public string model { get; set; }
        public string deviceID { get; set; }
        public string firmware { get; set; }
        public string hostname { get; set; }
        public string port { get; set; }
        public List<LiveTvTunerInfo> tuners;
        public bool onlyLoadFavorites { get; set; }
        private void initialSetup()
        {
            model = "";
            deviceID = "";
            firmware = "";
            port = "5004";
            tuners = new List<LiveTvTunerInfo>();
        }

        public TunerServer()
        {
            initialSetup();
        }
        public TunerServer(string hostname)
        {
            initialSetup();
            this.hostname = hostname;
        }
        public string getWebUrl()
        {
            return "http://" + hostname;
        }
        public string getApiUrl()
        {
            return getWebUrl() + ":" + port;
        }
        public async Task GetDeviceInfo(ILogger _logger, IHttpClient _httpClient, HttpRequestOptions httpOptions)
        {           
            httpOptions.Url = string.Format("{0}/", getWebUrl());
            System.IO.Stream stream = await _httpClient.Get(httpOptions).ConfigureAwait(false);
            using (var sr = new StreamReader(stream, System.Text.Encoding.UTF8))
            {
                while (!sr.EndOfStream)
                {
                    string line = Xml.StripXML(sr.ReadLine());
                    if (line.StartsWith("Model:")) { model = line.Replace("Model: ", ""); }
                    if (line.StartsWith("Device ID:")) { deviceID = line.Replace("Device ID: ", ""); }
                    if (line.StartsWith("Firmware:")) { firmware = line.Replace("Firmware: ", ""); }
                }
                if (String.IsNullOrWhiteSpace(model))
                {
                    _logger.Error("[HomeRunTV] Failed to locate the tuner host");
                    throw new ApplicationException("Failed to locate the tuner host.");
                }
            }
        }
        public async Task<List<LiveTvTunerInfo>> GetTunersInfo(ILogger _logger, IHttpClient _httpClient, HttpRequestOptions httpOptions)
        {
            httpOptions.Url = string.Format("{0}/tuners.html", getWebUrl());
            System.IO.Stream stream = await _httpClient.Get(httpOptions).ConfigureAwait(false);
            CreateTuners(stream,_logger);
            return tuners;
        }
        private void CreateTuners(Stream tunersXML, ILogger _logger){
            int numberOfTuners = 3;
            while (tuners.Count() < numberOfTuners)
            {
                tuners.Add(new LiveTvTunerInfo() { Name = "Tunner " + tuners.Count , SourceType=model });
            }
            using (var sr = new StreamReader(tunersXML, System.Text.Encoding.UTF8))            
            {               
                while (!sr.EndOfStream)
                {
                    string line = Xml.StripXML(sr.ReadLine());
                    if (line.StartsWith("Tuner 0 Channel")) {CheckTuner(0, line);}
                    if (line.StartsWith("Tuner 1 Channel")) {CheckTuner(1, line); }
                    if (line.StartsWith("Tuner 2 Channel")) {CheckTuner(2, line); }                    
                }
                if (String.IsNullOrWhiteSpace(model))
                {
                    _logger.Error("[HomeRunTV] Failed to load tuner info");
                    throw new ApplicationException("Failed to load tuner info.");
                }
            }
        }
        private void CheckTuner(int tunerPos,string tunerInfo)
        {
            string currentChannel;
            LiveTvTunerStatus status;
            currentChannel = tunerInfo.Replace("Tuner "+tunerPos+" Channel", "");
            if (currentChannel != "none") {status = LiveTvTunerStatus.LiveTv;}else{status=LiveTvTunerStatus.Available;}
            tuners[tunerPos].ProgramName =currentChannel;
            tuners[tunerPos].Status = status;
        }

        public async Task<IEnumerable<ChannelInfo>> GetChannels(ILogger _logger, IHttpClient _httpClient, HttpRequestOptions httpOptions, IJsonSerializer json, IXmlSerializer xml)
        {
            List<ChannelInfo> ChannelList;
            httpOptions.Url = string.Format("{0}/lineup.json", getWebUrl());           
            System.IO.Stream stream = await _httpClient.Get(httpOptions).ConfigureAwait(false);
            var root = json.DeserializeFromStream<List<Rootobject>>(stream);
            _logger.Info("[HomeRunTV] Found "+ root.Count() + "channels on host: " + hostname);
            if(onlyLoadFavorites){root.RemoveAll(x => x.Favorite == false);}
            if (root != null)
            {
                ChannelList = root.Select(i => new ChannelInfo
                {
                    Name = i.GuideName,
                    Number = i.GuideNumber.ToString(),
                    Id = i.GuideNumber.ToString(),
                    ImageUrl = null,
                    HasImage = false
                }).ToList();
               
            }else{
                ChannelList = new  List<ChannelInfo>();
            }
            return ChannelList;
        }
        public string cleanChannelName(string channelName)
        {
            List<string> removeWordList = new List<string>() { "HD", "EAST","-DTV" }; 
            channelName =  Xml.StripBetweenChar(channelName,'(',')');;
            foreach (string word in removeWordList)
                {
                   channelName = channelName.Replace(word, "");
                }
            return channelName;
        }

        public bool isTvReleated(string description)
        {
            List<string> WordList = new List<string>() { "TV", "television","channel" }; 
            foreach (string word in WordList)
              {
                  if (description.IndexOf(word, StringComparison.CurrentCultureIgnoreCase) > 0) { return true; }
              }
            return false;
        }
        public string getChannelStreamInfo(string ChannelNumber)
        {
            return getApiUrl()+"/auto/v"+ChannelNumber;
        }
        public class Rootobject
        {
            public string GuideNumber { get; set; }
            public string GuideName { get; set; }
            public string URL { get; set; }
            public bool Favorite { get; set; }
            public bool DRM { get; set; }
        }
        

    }
}
