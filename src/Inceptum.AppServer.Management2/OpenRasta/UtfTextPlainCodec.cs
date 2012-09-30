using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenRasta.Codecs;
using OpenRasta.TypeSystem;
using OpenRasta.Web;

namespace Inceptum.AppServer.Management2.OpenRasta
{
    [SupportedType(typeof(string))]
    [MediaType("text/plain;q=1.0")]
    public class UtfTextPlainCodec : IMediaTypeWriter, IMediaTypeReader, ICodec
    {
        private readonly Dictionary<IHttpEntity, string> m_Values = new Dictionary<IHttpEntity, string>();
         
        public object Configuration { get; set; }

        public object ReadFrom(IHttpEntity request, IType destinationType, string destinationParameterName)
        {
            long? contentLength = request.ContentLength;
            if ((contentLength.GetValueOrDefault() != 0L ? 0 : (contentLength.HasValue ? 1 : 0)) != 0)
                return string.Empty;
            if (!m_Values.ContainsKey(request))
            {
                Encoding encoding = DetectTextEncoding(request);
                string str = new StreamReader(request.Stream, encoding).ReadToEnd();
                m_Values.Add(request, str);
            }
            return m_Values[request];
        }

        public void WriteTo(object entity, IHttpEntity response, string[] parameters)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(entity.ToString());
            response.ContentType = new MediaType("text/plain;charset=utf-8");
            response.ContentLength = bytes.Length;
            response.Stream.Write(bytes, 0, bytes.Length);
        }

        private Encoding DetectTextEncoding(IHttpEntity request)
        {
            try
            {
                return Encoding.GetEncoding(request.ContentType.CharSet);
            }
            catch
            {
                return Encoding.UTF8;
            }
        }
    }
}