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
using HomeRunTV.Interfaces;
using HomeRunTV.General_Helper;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Channels;


namespace HomeRunTV.TunerHelpers
{
    public class TunerServer
    {
        public string model { get; set; }
        public string deviceID { get; set; }
        public string firmware { get; set; }
        public string hostname { get; set; }
        public string port { get; set; }

        private void initialSetup()
        {
            model = "";
            deviceID = "";
            firmware = "";
            port = "5004";
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
        public async Task GetTunerInfo(ILogger _logger, IHttpClient _httpClient, HttpRequestOptions httpOptions)
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

        public async Task<IEnumerable<ChannelInfo>> GetChannels(ILogger _logger, IHttpClient _httpClient, HttpRequestOptions httpOptions, IJsonSerializer json, IXmlSerializer xml)
        {
            List<ChannelInfo> ChannelList;
            httpOptions.Url = string.Format("{0}/lineup.json", getWebUrl());           
            System.IO.Stream stream = await _httpClient.Get(httpOptions).ConfigureAwait(false);
            var root = json.DeserializeFromStream<List<Rootobject>>(stream);
            _logger.Info("[HomeRunTV] Found "+ root.Count() + "channels on host: " + hostname);
            root.RemoveAll(x => x.Favorite == false);
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

               foreach (ChannelInfo Channel in ChannelList){
                   
                    string wikiApi = "http://en.wikipedia.org/w/api.php?action=opensearch&format=xml&namespace=0&redirects=return&search=";
                    string curatedName =cleanChannelName(Channel.Name);
                    _logger.Info("[HomeRunTV] Finding image for: " + curatedName);
                    httpOptions.Url = string.Format("{0}{1}", wikiApi,curatedName);           
                    stream = await _httpClient.Get(httpOptions).ConfigureAwait(false);
                    SearchSuggestion wikiInfo = new SearchSuggestion();
                    wikiInfo = (SearchSuggestion) xml.DeserializeFromStream(wikiInfo.GetType(), stream);
                    _logger.Info("[HomeRunTV] Channel Matches: " + wikiInfo.Section.Count());
                   int count = wikiInfo.Section.Count();
                   int counter = 0;
                    while (counter < count)
                    {
                       
                        if ( (wikiInfo.Section[counter].Image != null) && (isTvReleated(wikiInfo.Section[counter].Description.Value)))
                        {
                            Channel.ImageUrl = wikiInfo.Section[counter].Image.source;
                            Channel.HasImage = true;
                            _logger.Info("[HomeRunTV] Found IMAGE for "+Channel.Name+" from: "+Channel.ImageUrl);
                            break;
                        }
                        counter++;
                    }
               }
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
