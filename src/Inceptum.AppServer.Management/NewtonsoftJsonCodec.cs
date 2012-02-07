using System.IO;
using Newtonsoft.Json;
using OpenRasta.Codecs;
using OpenRasta.TypeSystem;
using OpenRasta.Web;

namespace Inceptum.AppServer.Management
{
    [MediaType("application/json;q=0.5", "json")]
    public class NewtonsoftJsonCodec : IMediaTypeReader, IMediaTypeWriter
    {
        public object Configuration { get; set; }

        public object ReadFrom(IHttpEntity request, IType destinationType, string destinationName)
        {
            using (var streamReader = new StreamReader(request.Stream))
            {
                var ser = new JsonSerializer();
                return ser.Deserialize(streamReader, destinationType.StaticType);
            }
        }

        public void WriteTo(object entity, IHttpEntity response, string[] parameters)
        {
            if (entity == null)
                return;
            using (var textWriter = new StreamWriter(response.Stream))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(textWriter, entity);
            }
            response.Headers.Add("Access-Control-Allow-Origin","*");
        }
    }
}