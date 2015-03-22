using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Serialization;

namespace HomeRunTV.GuideData.Responses
{
    class ScheduleDirect
    {
        public class Token
        {
            public int code { get; set; }
            public string message { get; set; }
            public string serverID { get; set; }
            public string token { get; set; }
        }

        public class Status
        {
            public Account account { get; set; }
            public Lineup[] lineups { get; set; }
            public DateTime lastDataUpdate { get; set; }
            public object[] notifications { get; set; }
            public Systemstatu[] systemStatus { get; set; }
            public string serverID { get; set; }
            public int code { get; set; }
        }

        public class Account
        {
            public DateTime expires { get; set; }
            public object[] messages { get; set; }
            public int maxLineups { get; set; }
            public DateTime nextSuggestedConnectTime { get; set; }
        }

        public class Lineup
        {
            public string ID { get; set; }
            public DateTime modified { get; set; }
            public string uri { get; set; }
            public bool isDeleted { get; set; }
        }

        public class Systemstatu
        {
            public DateTime date { get; set; }
            public string status { get; set; }
            public string details { get; set; }
        }


        public class Channel
        {
            public Map[] map { get; set; }
            public Station[] stations { get; set; }
            public Metadata metadata { get; set; }
        }

        public class Metadata
        {
            public string lineup { get; set; }
            public DateTime modified { get; set; }
            public string transport { get; set; }
        }

        public class Map
        {
            public string stationID { get; set; }
            public string channel { get; set; }
        }

        public class Station
        {
            public string callsign { get; set; }
            public string name { get; set; }
            public string broadcastLanguage { get; set; }
            public string descriptionLanguage { get; set; }
            public string stationID { get; set; }
            public Logo logo { get; set; }
            public string affiliate { get; set; }
            public bool isCommercialFree { get; set; }
        }

        public class Logo
        {
            public string URL { get; set; }
            public int height { get; set; }
            public int width { get; set; }
            public string md5 { get; set; }
        }

        public class RequestScheduleForChannel
        {
            public string stationID { get; set; }
            public List<string> date { get; set; }
        }


public class Rating
{
    public string body { get; set; }
    public string code { get; set; }
}

public class Multipart
{
    public int partNumber { get; set; }
    public int totalParts { get; set; }
}

public class Program
{
    public string programID { get; set; }
    public string airDateTime { get; set; }
    public int duration { get; set; }
    public string md5 { get; set; }
    public List<string> audioProperties { get; set; }
    public List<string> videoProperties { get; set; }
    public List<Rating> ratings { get; set; }
    public bool? @new { get; set; }
    public Multipart multipart { get; set; }
}

public class MetadataSchedule
{
    public string modified { get; set; }
    public string md5 { get; set; }
    public string startDate { get; set; }
    public string endDate { get; set; }
    public int days { get; set; }
}

        public class Schedules
        {
            public string stationID { get; set; }
            public List<Program> programs { get; set; }
            public MetadataSchedule metadata { get; set; }
        }
    }
}
