using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Inceptum.AppServer.Management.Handlers;
using Inceptum.AppServer.Management.Resources;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
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
                ResourceSpace.Uses.UriDecorator<SplitParamsUriDecorator>();

                ResourceSpace.Has.ResourcesOfType<string>().WithoutUri.TranscodedBy<UtfTextPlainCodec>();


                ResourceSpace.Has
                    .ResourcesOfType<IFile>()
                    .AtUri("Content/{path}")
                    .HandledBy<EmbeddedContentHandler>()
                    .TranscodedBy<ApplicationOctetStreamCodec>();

                ResourceSpace.Has.ResourcesOfType<ServerPage>()
                    .AtUri("/server")
                    .HandledBy<ServerPageHandler>()
                    .TranscodedBy<RazorCodec>(new
                                                  {
                                                      index = "Server.cshtml"
                                                  });

                ResourceSpace.Has.ResourcesOfType<ConfPage>()
                    .AtUri("/conf")
                    .HandledBy<ConfPageHandler>()
                    .TranscodedBy<RazorCodec>(new
                                                  {
                                                      index = "Conf.cshtml"
                                                  });

                ResourceSpace.Has.ResourcesOfType<AppsPage>()
                    .AtUri("/apps").
                    And.AtUri("/")
                    .And.AtUri("/apps/switch")
                    .HandledBy<AppsPageHandler>()
                    .TranscodedBy<RazorCodec>(new
                                                  {
                                                      index = "Apps.cshtml"
                                                  });

                ResourceSpace.Has.ResourcesOfType<string>()
                    .AtUri("/configuration/{configuration}/{bundle}/{overrides}")
                    .And.AtUri("/configuration/{configuration}/{bundle}")
                    .HandledBy<BundleHandler>()
                    .TranscodedBy<RazorCodec>(new
                    {
                        index = "Bundle.cshtml"
                    });

                ResourceSpace.Has.ResourcesOfType<object>()
                    .AtUri("/configurations/")
                    .And.AtUri("/configurations/{configuration}")
                    .And.AtUri("/configurations/{configuration}/{bundle}")
                    .And.AtUri("/configurations/{configuration}/{bundle}/{overrides}")
                    .HandledBy<BundleHandler>()
                    /*.TranscodedBy<RazorCodec>(new
                    {
                        index = "Configurations.cshtml"
                    }).And*/.TranscodedBy<NewtonsoftJsonCodec>();            
/*
                
                ResourceSpace.Has.ResourcesOfType<IEnumerable<string>>()
                    .AtUri("/configuration/{configuration}")
                    .HandledBy<BundleHandler>()
                    .TranscodedBy<RazorCodec>(new
                    {
                        index = "Configuration.cshtml"
                    });
*/

            }
        }

        #endregion
    }
}