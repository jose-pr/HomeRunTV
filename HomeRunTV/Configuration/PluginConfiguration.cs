using System;
using MediaBrowser.Model.Plugins;

namespace HomeRunTV.Configuration
{
    public class PluginConfiguration:BasePluginConfiguration
    {
        public string apiURL { get; set; }
        public string Port { get; set; }
        public bool loadOnlyFavorites { get; set; }

        public PluginConfiguration()
        {
            Port = "5004";
            apiURL = "localhost";
            loadOnlyFavorites = true;
        }
    }
}
