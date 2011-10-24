using System.IO;

namespace Inceptum.AppServer.Configuration.Providers
{
    public class FileSystemConfigurationProvider:ResourceConfigurationProviderBase
    {
        private readonly string m_ConfigFolder;

        public FileSystemConfigurationProvider(string configFolder)
        {
            m_ConfigFolder = configFolder;
            if (!Directory.Exists(configFolder))
                Directory.CreateDirectory(configFolder);

        }

        protected internal override string GetResourceName(string configuration,string bundleName, params string[] extraParams)
        {
            return Path.Combine(m_ConfigFolder,configuration, bundleName + (extraParams.Length > 0 ? "." : "") + string.Join(".", extraParams));
        }

        protected internal override string GetResourceContent(string name)
        {
            return File.ReadAllText(name);
        }

        protected internal virtual void StoreBundle(string configuration,string bundleName, string[] extraParams, string content)
        {
            NormalizeParams(ref configuration,ref bundleName, extraParams);
            var resourceName = GetResourceName(configuration, bundleName, extraParams);
            var path = Path.Combine(m_ConfigFolder, resourceName);
            var directoryName = Path.GetDirectoryName(path);
            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);
            if (File.Exists(path))
            {
                var fi = new FileInfo(path);
                if ((fi.Attributes & FileAttributes.ReadOnly) > 0)
                    fi.Attributes -= FileAttributes.ReadOnly;
                fi.Delete();
            }
            File.WriteAllText(path, content);
            
        }
    }
}
