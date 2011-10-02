using System;
using System.IO;
using Inceptum.AppServer.Management.Resources;
using OpenRasta.Web;

namespace Inceptum.AppServer.Management.Handlers
{
    public class EmbeddedContentHandler
    {
        public OperationResult Get(string folder , string key)
        {
            try
            {
                var responseResource = new ContentFile("Inceptum.AppServer.Management.Content." + folder + "." + key);
                return new OperationResult.OK
                           {
                               ResponseResource = responseResource
                           };
            }
            catch (FileNotFoundException e)
            {
                return new OperationResult.NotFound();
            }
            catch(Exception e)
            {
                return new OperationResult.BadRequest();
            }
        }
    }
}