using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Inceptum.AppServer.WebApi.MessageHandlers
{
    /// <summary>
    /// Globally adds default headers to http response messages
    /// </summary>
    internal sealed class DefaultHeadersMessageHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                               CancellationToken cancellationToken)
        {
            return base.SendAsync(request, cancellationToken).ContinueWith(t =>
            {
                HttpResponseMessage response = t.Result;

                if (response == null) return null; // Response is null on errors - do nothing

                // No cache headers 
                response.Headers.Add("Cache-Control", "no-cache, must-revalidate"); // HTTP 1.1
                response.Headers.Add("Pragma", "no-cache"); // HTTP 1.0            

                // Info headers
                if (response.Headers.Contains("Server")) response.Headers.Remove("Server");
                response.Headers.Add("Server", "WebApiHost-" + GetType().Assembly.GetName().Version);
                response.Headers.Add("X-Api-Version", GetType().Assembly.GetName().Version.ToString());

                // No cache headers on content
                if (response.Content != null)
                {
                    response.Content.Headers.Expires = DateTime.Now.AddYears(-1);
                }

                return response;
            });
        }
    }
}