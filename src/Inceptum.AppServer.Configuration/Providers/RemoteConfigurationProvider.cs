using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace Inceptum.AppServer.Configuration.Providers
{
    public class RemoteConfigurationProvider : ResourceConfigurationProviderBase
    {
        private const int DEFAULT_HELPROX_TIMEOUT = 30000;

        private readonly int m_TimeoutInMs;

        public RemoteConfigurationProvider(string configurationServiceUrl)
            : this(configurationServiceUrl,  DEFAULT_HELPROX_TIMEOUT)
        {
        }

        public RemoteConfigurationProvider(string configurationServiceUrl,  int timeoutInMs)
        {
            if (configurationServiceUrl == null) throw new ArgumentNullException("configurationServiceUrl");
            if (!ValidationHelper.IsValidUrl(configurationServiceUrl)) throw new ArgumentException("Wrong URL format", "configurationServiceUrl");

            m_TimeoutInMs = timeoutInMs;
            configurationServiceUrl = configurationServiceUrl.Trim();
            configurationServiceUrl = configurationServiceUrl.EndsWith("/") ? configurationServiceUrl : configurationServiceUrl + "/";
            ConfifurationServiceUrl = new Uri(configurationServiceUrl);
        }

        public Uri ConfifurationServiceUrl { get; private set; }


        protected internal override string GetResourceName(string configuration, string bundleName, params string[] extraParams)
        {
            return configuration+"/"+bundleName + "/" + string.Join("/", extraParams) + (extraParams.Length > 0 ? "/" : "");
        }

        protected internal override string GetResourceContent(string name)
        {
            var request = createRequest(name);

            WebResponse response = null;
            try
            {
                response = request.GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    Debug.Assert(stream != null);

                    using (var streamReader = new StreamReader(stream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
            catch (WebException webException)
            {
                response = webException.Response;
            }
            finally
            {
                if (response != null)
                    response.Close();
            }
            return null;
        }

      
        private WebRequest createRequest(string resourecName)
        {
            var uri = new Uri(ConfifurationServiceUrl, resourecName);
            var request = WebRequest.Create(uri);
            request.Proxy = new WebProxy();
            request.Timeout = m_TimeoutInMs;

            return request;
        }
    }
}