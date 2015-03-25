using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Model.Plugins;
using HomeRunTV.GuideData;
using HomeRunTV.GeneralHelpers;
namespace HomeRunTV.Configuration
{
    public class PluginConfiguration:BasePluginConfiguration
    {
        public string apiURL { get; set; }
        public string Port { get; set; }
        public bool loadOnlyFavorites { get; set; }
        public string hashPassword { get; set; }
        public string username { get; set; }
        public string tvLineUp { get; set; }
        public string avaliableLineups{get;set;}
        public string headendName { get; set; }
        public string headendValue { get; set; }
        public string zipCode { get; set; }

           


        public PluginConfiguration()
        {
            Port = "5004";
            apiURL = "localhost";
            loadOnlyFavorites = true;
            tvLineUp = "";
            username = "";
            hashPassword = "";
            avaliableLineups = "";
            headendName = "";
            headendValue = "";
            zipCode = "";

        }


    }
}
