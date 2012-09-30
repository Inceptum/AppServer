using System;
using System.IO;
using System.Linq;
using Inceptum.AppServer.Management2.Resources;
using OpenRasta.Pipeline;
using OpenRasta.Web;

namespace Inceptum.AppServer.Management2.OpenRasta
{
    public class StaticContentPipelineContributor : IPipelineContributor 
    {
        private const string FOLDER = @"..\..\..\..\UiMockUps";

        public void Initialize(IPipeline pipelineRunner)
        {
            pipelineRunner.Notify(processStaticContent).Before<KnownStages.IUriMatching>();
        }

        private PipelineContinuation processStaticContent(ICommunicationContext context)
        {
            try
            {
                if(context.Request.Uri.Segments.Count()>=2 && context.Request.Uri.Segments[1].ToLower()=="api/")
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
                }
                else
                {
                    responseResource = new ContentFile("Inceptum.AppServer.Management2.Content." + resource);
                }
                context.Response.Headers["Cache-Control"] = debug ? "no-cache" : "public, must-revalidate, max-age=2592000";



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