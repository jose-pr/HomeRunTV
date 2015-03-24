﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;

namespace HomeRunTV.GeneralHelpers
{
    public class HttpClientHelper
    {
        public IHttpClient httpClient{get;set;}
        public HttpRequestOptions httpOptions{get;set;}
        public CancellationToken cancellationToken{get;set;}
        public IJsonSerializer jsonSerializer{get;set;}
        public IXmlSerializer xmlSerializer{get;set;}
        public ILogger logger{get;set;}
        public HttpClientHelper(){
        
        }
        
        public Stream response;
        public void useCancellationToken()     
        {
            httpOptions.CancellationToken=cancellationToken;
        }
        public async Task<Stream> Get(){
            response = await httpClient.Get(httpOptions).ConfigureAwait(false);
            return response;
        }
        public async Task<Stream> Post(){
           HttpResponseInfo httpRespInfo = await httpClient.Post(httpOptions).ConfigureAwait(false);
           response = httpRespInfo.Content;
           return response;
        }
    }
    /// <summary>
    /// Class HttpRequestOptions
    /// </summary>
    public class HttpRequestOptionsMod:HttpRequestOptions
    {

      
        public string Token
        {
            get { return GetHeaderValue("token"); }
            set
            {
                RequestHeaders["token"] = value;
            }
        }
        public void SetRequestHeader(string headerName, string value)
        {
                  RequestHeaders[headerName] = value;
        }
        private string GetHeaderValue(string name)
        {
            string value;

            RequestHeaders.TryGetValue(name, out value);

            return value;
        }
         /// <summary>
        /// Initializes a new instance of the <see cref="HttpRequestOptions"/> class.
        /// </summary>
        public HttpRequestOptionsMod():base()
        {
        }

    }

    public enum CacheMode
    {
        None = 0,
        Unconditional = 1
    }
}

