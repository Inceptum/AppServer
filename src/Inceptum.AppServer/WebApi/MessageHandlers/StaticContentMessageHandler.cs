using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Inceptum.WebApi.Help;

namespace Inceptum.AppServer.WebApi.MessageHandlers
{
    public class StaticContentMessageHandler : DelegatingHandler
    {
        private readonly EmbeddedResourcesContentProvider m_ContentProvider;
        private const string FOLDER = @"..\..\..\Inceptum.AppServer\WebApi\Content";

        public StaticContentMessageHandler()
        {
            m_ContentProvider = new EmbeddedResourcesContentProvider(GetType().Assembly, "Inceptum.AppServer.WebApi.Content.");
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string resourceName = getResourceName(request);
            if (resourceName == "")
            {
                resourceName="index.html";
            }
            bool debug = false;
            var file = Path.Combine(Path.GetFullPath(FOLDER), resourceName);
#if DEBUG
            debug = true;
#endif

            StaticContent content;
            HttpResponseMessage result;
            if (debug && File.Exists(file))
            {
                result = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(File.ReadAllBytes(file))
                };
                result.Content.Headers.ContentType = new MediaTypeHeaderValue(GetContentTypeByResourceName(resourceName));
                return Task.FromResult(result);
            }


            content = m_ContentProvider.GetContent(resourceName);
            if (content == null)
                return base.SendAsync(request, cancellationToken);
            result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content.ContentBytes)
            };
            result.Content.Headers.ContentType = new MediaTypeHeaderValue(GetContentTypeByResourceName(resourceName));
            return Task.FromResult(result);
        }


        public static string GetContentTypeByResourceName(string resourcePath)
        {
            if (resourcePath == null)
                throw new ArgumentNullException("resourcePath");
            switch ((Path.GetExtension(resourcePath) ?? string.Empty).ToLowerInvariant())
            {
                case ".js":
                    return "application/javascript";
                case ".css":
                    return "text/css";
                case ".html":
                    return "text/html";
                default:
                    return "application/octet-stream";
            }
        }

        private string getResourceName(HttpRequestMessage request)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            return request.RequestUri.LocalPath.Trim('\\', '/').ToLowerInvariant();
        }
    }
}