using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Configuration.Model;
using OpenRasta.IO;
using OpenRasta.Web;

namespace Inceptum.AppServer.Management2.Handlers
{
    public class ConfigurationsHandler
    {
        private readonly IManageableConfigurationProvider m_Provider;

        public ConfigurationsHandler(IManageableConfigurationProvider provider)
        {
            m_Provider = provider;
        }

        public object Get()
        {
            var configurations = m_Provider.GetConfigurations();

            return configurations.Select(c=>new {
                        id=c.Name,
                        bundles=getBundles(c,c.Name)}
                        ).ToArray();
        }   
        
        public object GetBundle(string configuration,string bundle)
        {
            var configurations = m_Provider.GetConfiguration(configuration);
            var b = configurations.Bundles.FirstOrDefault(x => x.Name == bundle);
            if (b == null)
                return new OperationResult.NotFound();

            return new
                       {
                           id = b.Name,
                           name = b.ShortName,
                           content=b.Content
                       };
        }


        public OperationResult GetExport(string configuration)
        {
            try
            {
                var config = m_Provider.GetConfiguration(configuration);
                var file = new InMemoryDownloadableFile
                               {
                                   ContentType = MediaType.ApplicationOctetStream,
                                   FileName = config.Name + ".zip", 
                                   Options = DownloadableFileOptions.Save
                               };
                var outputStream = file.OpenStream();
                var zipStream = new ZipOutputStream(outputStream);

                zipStream.SetLevel(3); //0-9, 9 being the highest level of compression

                foreach (var bundle in config.Bundles)
                {
                    var newEntry = new ZipEntry(bundle.Name);
                    newEntry.DateTime = DateTime.Now;

                    zipStream.PutNextEntry(newEntry);
                    var memStreamIn = new MemoryStream(Encoding.UTF8.GetBytes(bundle.Content));
                    StreamUtils.Copy(memStreamIn, zipStream, new byte[4096]);
                    zipStream.CloseEntry();
                }

                zipStream.IsStreamOwner = false;    // False stops the Close also Closing the underlying stream.
                zipStream.Close();          // Must finish the ZipOutputStream before using outputMemStream.
                outputStream.Position = 0;
                file.Length = outputStream.Length;
                return new OperationResult.OK { ResponseResource = file };
            }
            catch (Exception e)
            {
                return new OperationResult.InternalServerError { Description = e.Message, ResponseResource = e.ToString(), Title = "Error" };
            }
        }

        private IEnumerable<object> getBundles(IEnumerable<Bundle> bundles, string configuration)
        {
            return bundles.Select(b => new
                                    {
                                        id = b.Name,
                                        name = b.ShortName,
                                        bundles = getBundles(b,configuration),
                                        configuration
                                    }).ToArray();
        }
    }
}