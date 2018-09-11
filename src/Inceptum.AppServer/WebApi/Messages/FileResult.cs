using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace Inceptum.AppServer.WebApi.Messages
{
    class FileResult : IHttpActionResult
    {
        private readonly Stream m_Stream;
        private readonly string m_ContentType;
        private string m_FileName;

        public FileResult(Stream stream,string fileName, string contentType = null)
        {
            m_FileName = fileName;
            m_Stream = stream;
            if (stream == null) throw new ArgumentNullException("stream");

            m_ContentType = contentType;
        }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(m_Stream)
                };

                var contentType = m_ContentType ?? MimeMapping.GetMimeMapping(Path.GetExtension(m_FileName));
                response.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = m_FileName
                };
                return response;

            }, cancellationToken);
        }
    }
}