using System.Reflection;
using Inceptum.AppServer.Management.Handlers;
using Inceptum.AppServer.Management.Resources;
using Inceptum.AppServer.Management.Resources;
using OpenRasta.Codecs;
using OpenRasta.Codecs.Razor;
using OpenRasta.Configuration;
using OpenRasta.IO;

namespace Inceptum.AppServer.Management
{
    public class Configurator : IConfigurationSource
    {
        #region IConfigurationSource Members

        public void Configure()
        {
            using (OpenRastaConfiguration.Manual)
            {
                ResourceSpace.Uses.ViewsEmbeddedInTheAssembly(Assembly.GetExecutingAssembly(), "Inceptum.AppServer.Management.Views");
                //ICodec


                ResourceSpace.Has
                    .ResourcesOfType<IFile>()
                    .AtUri("{folder}/{key}")
                    .HandledBy<EmbeddedContentHandler>()
                    .TranscodedBy<ApplicationOctetStreamCodec>();

                ResourceSpace.Has.ResourcesOfType<ConfPage>()
                    .AtUri("/conf")
                    .And.AtUri("/")
                    .HandledBy<ConfPageHandler>()
                    .TranscodedBy<RazorCodec>(new
                                                  {
                                                      index = "Conf.cshtml"
                                                  });

                ResourceSpace.Has.ResourcesOfType<AppsPage>()
                    .AtUri("/apps")
                    .HandledBy<AppsPageHandler>()
                    .TranscodedBy<RazorCodec>(new
                                                  {
                                                      index = "Apps.cshtml"
                                                  });
            }
        }

        #endregion
    }
}