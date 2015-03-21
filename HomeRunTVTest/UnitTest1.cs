using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HomeRunTV.Configuration;
using HomeRunTV.TunerHelpers;
using MediaBrowser.Common.Net;


using HomeRunTVTest.Interfaces;

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HomeRunTVTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            string url = "192.168.2.238";
            string fullURL = "http://" + url + ":" + "8866";
            PluginConfiguration testPluginConfig = new PluginConfiguration();

            Assert.AreEqual("http://192.168.2.238:8866",fullURL);
            return; 
        }
        [TestMethod]
        public async Task TestHttpGET()
        {
            CancellationToken cancelation = new CancellationToken();
            IHttpClient _httpClient = new HttpClient();
            Log _logger = new Log();
             var _httpOptions = new HttpRequestOptions { CancellationToken = cancelation };
             var baseUrl = "google.com";
            TunerServer tunerServer = new TunerServer(baseUrl);
             await tunerServer.GetTunerInfo(_logger, _httpClient, _httpOptions);
             Debug.WriteLine(tunerServer.model);
             Debug.WriteLine(tunerServer.firmware);
             Debug.WriteLine(tunerServer.deviceID);
             Debug.WriteLine(_logger.lastLog);
             Debug.WriteLine("last");
             Assert.AreEqual("HDHR3-CC", tunerServer.model);
         return;
        }
        [TestMethod]
        public void getHost()
        {
            var test = GetHostFromUrl("192.168.2.164");
            Debug.WriteLine(test);
        }
        private string GetHostFromUrl(string url)
        {
            var start = url.IndexOf("://", StringComparison.OrdinalIgnoreCase) + 3;
            Debug.WriteLine(start);
            if (start < 3) { start = 0; }
            var len = url.IndexOf('/', start) - start;
            Debug.WriteLine(len);
            if (len < 0) { len = url.Length - start; }
            Debug.WriteLine(len);
            return url.Substring(start, len);
        }

    }


}
