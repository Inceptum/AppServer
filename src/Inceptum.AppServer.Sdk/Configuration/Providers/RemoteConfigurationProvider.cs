using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Inceptum.AppServer.Configuration.Providers
{
    public class RemoteConfigurationProvider : ResourceConfigurationProviderBase, IManageableConfigurationProvider
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
            ConfigurationServiceUrl = new Uri(configurationServiceUrl);
        }

        public Uri ConfigurationServiceUrl { get; private set; }


        protected internal override string GetResourceName(string configuration, string bundleName, params string[] extraParams)
        {
            return "configuration/"+configuration+"/"+bundleName + "/" + string.Join("/", extraParams) + (extraParams.Length > 0 ? "/" : "");
        }

        protected internal override string GetResourceContent(string name)
        {
            using (var client = createClient(false))
            {
                var response = client.GetAsync(new Uri(name, UriKind.Relative)).ConfigureAwait(false).GetAwaiter().GetResult();
                switch (response.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        throw new ConfigurationErrorsException(string.Format("Faled to get resource '{0}': {1}",name,response.StatusCode));
                    case HttpStatusCode.NotFound:
                        return null;
                }
                response.EnsureSuccessStatusCode();
                var stream = response.Content.ReadAsStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                using (var streamReader = new StreamReader(stream))
                {
                    return streamReader.ReadToEnd();
                }
            }

/*
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
            return null;*/
        }

      
        private WebRequest createRequest(string resourecName)
        {
            var uri = new Uri(ConfigurationServiceUrl, resourecName);
            var request = WebRequest.Create(uri);
            request.Proxy = new WebProxy();
            request.Timeout = m_TimeoutInMs;

            return request;
        }

        private HttpClient createClient(bool json=true)
        {
            var client = new HttpClient { BaseAddress = ConfigurationServiceUrl };
           /* if (json)
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }*/
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("utf-8"));
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(GetType().Name + "-" + GetType().Assembly.GetName().Version);
            
            client.Timeout = TimeSpan.FromMilliseconds(m_TimeoutInMs);
            return client;
        }

        private T getResource<T>(string uri)
            where T : class
        {
            using (var client = createClient())
            {
                
                Task<HttpResponseMessage> task=client.GetAsync(new Uri(uri, UriKind.Relative));
                var response = task.ConfigureAwait(false).GetAwaiter().GetResult();
                switch (response.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        throw new ConfigurationErrorsException(string.Format("Faled to get resource '{0}': {1}", uri, response.StatusCode));
                    case HttpStatusCode.NotFound:
                        return null;
                }
                response.EnsureSuccessStatusCode();
                var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                return JsonConvert.DeserializeObject<T>(result);
            }
        }
        private void deleteResource(string uri)
        {
            using (var client = createClient())
            {
                
                Task<HttpResponseMessage> task=client.DeleteAsync(new Uri(uri, UriKind.Relative));
                var response = task.ConfigureAwait(false).GetAwaiter().GetResult();
                switch (response.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        throw new ConfigurationErrorsException(string.Format("Faled to get resource '{0}': {1}", uri, response.StatusCode));
                    case HttpStatusCode.NotFound:
                        return;
                }
                response.EnsureSuccessStatusCode();
            }
        }

        private T sendResource<T>(string uri, T content, HttpMethod method = null)
            where T : class
        {
            using (var client = createClient())
            {

                Task<HttpResponseMessage> task;
                var httpMethod = method ?? HttpMethod.Post;

                if (httpMethod == HttpMethod.Post)
                    task = client.PostAsync(new Uri(uri, UriKind.Relative), createObjectContent(content));
                else if (httpMethod == HttpMethod.Put)
                    task = client.PutAsync(new Uri(uri, UriKind.Relative), createObjectContent(content));
                else
                    throw new ArgumentException("Method should be Put or Post", "method");
                var response = task.ConfigureAwait(false).GetAwaiter().GetResult();
                switch (response.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        throw new ConfigurationErrorsException(string.Format("Faled to get resource '{0}': {1}", uri, response.StatusCode));
                    case HttpStatusCode.NotFound:
                        return null;
                }
                response.EnsureSuccessStatusCode();
                var result = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                return JsonConvert.DeserializeObject<T>(result);
            }
        }

        private static StringContent createObjectContent<T>(T item)
        {
            var serializeObject = JsonConvert.SerializeObject(item);
            var content = new StringContent(serializeObject);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return content;
            //      return new ObjectContent<T>(item, new JsonMediaTypeFormatter { SerializerSettings = new JsonSerializerSettings() });
        }

        public ConfigurationInfo[] GetConfigurations ()
        {
            return getResource<ConfigurationInfo[]>("api/configurations");
        }

        public ConfigurationInfo GetConfiguration(string configuration)
        {
            return getResource<ConfigurationInfo>("api/configurations/"+configuration);
        }

        public void DeleteBundle(string configuration, string bundleName)
        {
            deleteResource("api/configurations/" + configuration + "/bundles/" + bundleName);
        }

        public BundleInfo CreateOrUpdateBundle(string configuration, string bundleName, string content)
        {
            return sendResource("api/configurations/" + configuration + "/bundles/" + bundleName,new BundleInfo()
            {
                Configuration = configuration,
                id = bundleName,
                PureContent= content
            },HttpMethod.Put);
        }

        public void CreateConfiguration(string configuration)
        {
            sendResource("api/configurations/",new ConfigurationInfo
            {
                Name = configuration
            },HttpMethod.Post);
        }

        public void DeleteConfiguration(string configuration)
        {
            deleteResource("api/configurations/" + configuration);
        }

       
    }
}