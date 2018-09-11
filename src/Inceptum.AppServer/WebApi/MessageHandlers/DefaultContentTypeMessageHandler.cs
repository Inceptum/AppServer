using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Inceptum.AppServer.WebApi.MessageHandlers
{
    /// <summary>
    /// Sets default content type for request and response if the user has not provided any/both of them
    /// </summary>
    internal sealed class DefaultContentTypeMessageHandler : DelegatingHandler
    {
        private readonly string m_DefaultContentType;

        public DefaultContentTypeMessageHandler(string defaultContentType = "application/json")
        {
            if (defaultContentType == null) throw new ArgumentNullException("defaultContentType");
            m_DefaultContentType = defaultContentType;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                               CancellationToken cancellationToken)
        {
            if (!request.Headers.Accept.Any())
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(m_DefaultContentType));
            }
            if (request.Content != null && request.Content.Headers.ContentType == null)
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(m_DefaultContentType);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
