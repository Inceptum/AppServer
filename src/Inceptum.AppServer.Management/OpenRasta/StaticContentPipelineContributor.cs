using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Inceptum.AppServer.Management.Resources;
using OpenRasta.Pipeline;
using OpenRasta.Web;

namespace Inceptum.AppServer.Management.OpenRasta
{
    public class StaticContentPipelineContributor : IPipelineContributor 
    {
        private const string FOLDER = @"..\..\..\Inceptum.AppServer.Management\Content";

        public void Initialize(IPipeline pipelineRunner)
        {
            pipelineRunner.Notify(processStaticContent).Before<KnownStages.IUriMatching>();
        }

        private PipelineContinuation processStaticContent(ICommunicationContext context)
        {
            try
            {
                if(context.Request.Uri.Segments.Count()>=2 && (context.Request.Uri.Segments[1].ToLower()=="api/" || context.Request.Uri.Segments[1].ToLower()=="configuration/"))
                    return PipelineContinuation.Continue;

                var path = context.Request.Uri.AbsolutePath.TrimStart(new []{'/','\\'});
                if (path == "") path = "index.htm";
                var resource = path.Replace('/', '.').Replace('\\', '.').TrimStart('.');
                var file = Path.Combine(Path.GetFullPath(FOLDER), path);
                ContentFile responseResource;

                bool debug=false;
#if DEBUG
                debug = true;
#endif

                if (debug && File.Exists(file))
                {
                    responseResource = new ContentFile(FOLDER, file);
                    context.Response.Headers["date"] = File.GetCreationTime(Path.Combine(FOLDER, file)).ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture);
                    context.Response.Headers["last-modified"] = File.GetLastWriteTime(Path.Combine(FOLDER, file)).ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture);

                }
                else
                {
                    responseResource = new ContentFile("Inceptum.AppServer.Management.Content." + resource);
                    context.Response.Headers["date"] = File.GetCreationTime(GetType().Assembly.Location).ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture);
                    context.Response.Headers["last-modified"] = File.GetLastWriteTime(GetType().Assembly.Location).ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture);
                }
                //context.Response.Headers["Cache-Control"] = debug ? "no-cache" : "public, must-revalidate, max-age=0";
                context.Response.Headers["Cache-Control"] = "no-cache";

                


                context.OperationResult= new OperationResult.OK
                {
                    ResponseResource = responseResource
                };
            }
            catch (FileNotFoundException)
            {
                context.OperationResult = new OperationResult.NotFound();
            }
            catch (Exception)
            {
                context.OperationResult = new OperationResult.BadRequest();
            }
            return PipelineContinuation.RenderNow;
        }
    }
}