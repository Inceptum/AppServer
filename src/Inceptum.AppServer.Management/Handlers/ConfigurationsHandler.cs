using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Castle.Core.Logging;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Configuration.Model;
using OpenRasta.IO;
using OpenRasta.Web;

namespace Inceptum.AppServer.Management.Handlers
{
    public class ConfigurationsHandler
    {
        private readonly IManageableConfigurationProvider m_Provider;
        private readonly ILogger m_Logger;

        public ConfigurationsHandler(IManageableConfigurationProvider provider, ILogger logger)
        {
            m_Logger = logger;
            m_Provider = provider;
        }

        public ConfigurationInfo[] Get()
        {
            return  m_Provider.GetConfigurations();
        }

        public ConfigurationInfo Get(string configuration)
        {
            return  m_Provider.GetConfiguration(configuration);
        }

        public void Delete(string configuration)
        {
            m_Provider.DeleteConfiguration(configuration);
        }

        public BundleInfo PutBundle(string configuration, string bundle, BundleInfo info)
        {
            m_Provider.CreateOrUpdateBundle(configuration, info.id, info.PureContent);
            return m_Provider.GetBundleInfo(configuration, bundle);
        }

        public object PostBundle(string configuration, BundleInfo info)
        {
            info.id = string.IsNullOrEmpty(info.Parent) ? info.Name : info.Parent + "." + info.Name;
            m_Provider.CreateOrUpdateBundle(configuration, info.id, info.PureContent);
            return m_Provider.GetBundleInfo(configuration, info.id);

        }

        public object Post(ConfigurationInfo info)
        {
            m_Provider.CreateConfiguration(info.Name);
            return m_Provider.GetConfiguration(info.Name);
        }

        public void DeleteBundle(string configuration, string bundle)
        {
            m_Provider.DeleteBundle(configuration, bundle);
        }

        [HttpOperation(HttpMethod.POST, ForUriName = "import")]
        public OperationResult PostImport(string configuration, IFile file)
        {
            var memoryStream = new MemoryStream();
            file.OpenStream().CopyTo(memoryStream);
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


                Func<BundleInfo, BundleInfo[]> select=null;
                select = bundle => new[] { bundle }.Concat(bundle.Bundles.SelectMany(b => select(b))).ToArray();
                IEnumerable<BundleInfo> bundles = config.Bundles.SelectMany(b => @select(b));

                foreach (var bundle in bundles)
                {
                    var newEntry = new ZipEntry(bundle.id);
                    newEntry.DateTime = DateTime.Now;

                    zipStream.PutNextEntry(newEntry);
                    var memStreamIn = new MemoryStream(Encoding.UTF8.GetBytes(bundle.PureContent));
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

        [HttpOperation(HttpMethod.GET, ForUriName = "configBundle")]
        public OperationResult Get(string configuration, string bundle)
        {
            return Get(configuration, bundle,null);
        }


        [HttpOperation(HttpMethod.GET, ForUriName = "configBundleWithOverrides")]
         public OperationResult Get(string configuration, string bundle, [Optional, DefaultParameterValue(null)]string overrides)
         {
             try
             {
                 var bundleContent = m_Provider.GetBundle(configuration, bundle, overrides == null ? new string[0] : overrides.Split(new[] { ':' }));
                 return new OperationResult.OK { ResponseResource = bundleContent };
             }
             catch (BundleNotFoundException e)
             {
                 return new OperationResult.NotFound { Description = e.Message, ResponseResource = e.Message, Title = "Error" };
             }
             catch (Exception e)
             {
                 return new OperationResult.InternalServerError { Description = e.Message, ResponseResource = e.Message, Title = "Error" };
             }
         }

    }
}