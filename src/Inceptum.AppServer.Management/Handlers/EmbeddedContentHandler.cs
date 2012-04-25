using System;
using System.IO;
using Inceptum.AppServer.Management.Resources;
using OpenRasta.Web;

namespace Inceptum.AppServer.Management.Handlers
{
    public class EmbeddedContentHandler
    {
        private readonly ICommunicationContext m_Context;
        private const string FOLDER = @"x:\WORK\Finam\CODE\ConfigurationUI";
        public EmbeddedContentHandler(ICommunicationContext context)
        {
            m_Context = context;
        }


        public OperationResult Get(string path)
        {
            try
            {
                var fileName = path.Replace("---", "/");
                var file = Path.Combine(FOLDER, fileName);
                ContentFile responseResource;

                bool debug=false;
#if DEBUG
                //debug = true;
#endif

                if (debug && File.Exists(file))
                    responseResource = new ContentFile(FOLDER, fileName);
                else
                    responseResource = new ContentFile("Inceptum.AppServer.Management.UI." + path);

                m_Context.Response.Headers["Cache-Control"] = debug ? "no-cache" : "public, must-revalidate, max-age=2592000";





                return new OperationResult.OK
                           {
                               ResponseResource = responseResource
                           };
            }
            catch (FileNotFoundException)
            {
                return new OperationResult.NotFound();
            }
            catch(Exception)
            {
                return new OperationResult.BadRequest();
            }
        }
        /*

        public OperationResult Get(string folder , string key)
        {
            try
            {
                var responseResource = new ContentFile("Inceptum.AppServer.Management.Content." + folder + "." + key);
                m_Context.Response.Headers["Cache-Control"] = "public, must-revalidate, max-age=2592000";
                return new OperationResult.OK
                           {
                               ResponseResource = responseResource
                           };
            }
            catch (FileNotFoundException)
            {
                return new OperationResult.NotFound();
            }
            catch(Exception)
            {
                return new OperationResult.BadRequest();
            }
        }*/
    }
}
