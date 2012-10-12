using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Castle.Core.Logging;
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
        private ILogger m_Logger;

        public ConfigurationsHandler(IManageableConfigurationProvider provider, ILogger logger)
        {
            m_Logger = logger;
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

        public object Get(string configuration)
        {
            Config c = m_Provider.GetConfiguration(configuration);

            return new
                       {
                           id = c.Name,
                           bundles = getBundles(c, c.Name)
                       };
        }

   
 

        public void Delete(string configuration)
        {
            throw new NotImplementedException();
        }

        public object GetBundle(string configuration, string bundle)
        {
            var configurations = m_Provider.GetConfiguration(configuration);
            var b = configurations.Bundles.FirstOrDefault(x => x.Name == bundle);
            if (b == null)
                return new OperationResult.NotFound();

            return new BundleInfo()
                       {
                           id = b.Name,
                           Parent = b.Name,
                           Name = b.ShortName,
                           Content= b.Content,
                           Configuration=configuration
                       };
        }

        public object PutBundle(string configuration, string bundle, BundleInfo info)
        {
            m_Provider.CreateOrUpdateBundle(configuration, info.Name, info.Content);
            return GetBundle(configuration, info.Name);
        }

        public object PostBundle(string configuration, BundleInfo info)
        {
            string id = string.IsNullOrEmpty(info.Parent) ? info.Name : info.Parent + "." + info.Name;
            m_Provider.CreateOrUpdateBundle(configuration, id, info.Content);
            return GetBundle(configuration, id);
        }

        public void DeleteBundle(string configuration, string bundle)
        {
            m_Provider.DeleteBundle(configuration, bundle);
        }

        public OperationResult PostImport(string configuration, IFile file)
        {
            var memoryStream = new MemoryStream();
            file.OpenStream().CopyTo(memoryStream);
            Config config = m_Provider.GetConfiguration(configuration);
            var zipFile = new ZipFile(memoryStream);

            m_Provider.DeleteConfiguration(configuration);
            m_Provider.CreateConfiguration(configuration);
 
            int i = 0;
            foreach (ZipEntry bundleFile in zipFile)
            {
                i++;
                m_Logger.InfoFormat(bundleFile.Name);
                m_Provider.CreateOrUpdateBundle(configuration, bundleFile.Name, new StreamReader(zipFile.GetInputStream(bundleFile)).ReadToEnd());
            }
            m_Logger.InfoFormat("{0}",i);
            return new OperationResult.NoContent();
        }

         [HttpOperation(HttpMethod.POST, ForUriName = "export")]
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