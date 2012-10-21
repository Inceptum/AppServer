using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using Inceptum.AppServer.Configuration.Model;
using Newtonsoft.Json;

namespace Inceptum.AppServer.Configuration.Persistence
{
    public class FileSystemConfigurationPersister : IConfigurationPersister
    {
        public FileSystemConfigurationPersister(string configFolder)
        {
            if (configFolder == null)
                throw new ArgumentNullException("configFolder");
            ConfigFolder = Path.GetFullPath(configFolder);
            if (!Directory.Exists(ConfigFolder))
                Directory.CreateDirectory(ConfigFolder);
        }

        public string ConfigFolder { get; private set; }

        public BundleData[] Load()
        {
            var query = from config in Directory.GetDirectories(ConfigFolder)
                        let configName = Path.GetFileName(config)
                        from file in Directory.GetFiles(config)
                        let bundleName = Path.GetFileName(file)
                        orderby bundleName
                        select new BundleData {Configuration = configName, Name = bundleName, Content = File.ReadAllText(file)};
            return query.ToArray();
        }



        public void Save(IEnumerable<BundleData> data)
        {
            foreach (var bundleData in data)
            {
                if (bundleData.Action == BundleAction.None)
                    continue;

                var confDir = Path.Combine(ConfigFolder, bundleData.Configuration);
                if (!Directory.Exists(confDir))
                {
                    Directory.CreateDirectory(confDir);
                }

                var bundleFile = Path.Combine(confDir, bundleData.Name);
                if (File.Exists(bundleFile))
                    File.Delete(bundleFile);
                if (bundleData.Action == BundleAction.Create || bundleData.Action == BundleAction.Save)
                    File.WriteAllText(bundleFile, bundleData.Content);
            }
        }

        public string Create(string name)
        {
            if (!ValidationHelper.IsValidBundleName(name))
            {
                throw new ArgumentException(ValidationHelper.INVALID_NAME_MESSAGE, "name");
            }

            name = name.ToLower();
            var confDir = Path.Combine(ConfigFolder, name);

            if (Directory.Exists(confDir))
            {
                throw new ArgumentException("Configuration named " + name + " already exists");
            }

            Directory.CreateDirectory(confDir);

            return name;
        }

        public bool Delete(string name)
        {
            if (!ValidationHelper.IsValidBundleName(name))
            {
                throw new ArgumentException(ValidationHelper.INVALID_NAME_MESSAGE, "name");
            }
            name = name.ToLower();
            var confDir = Path.Combine(ConfigFolder, name);

            if (!Directory.Exists(confDir)) return false;

            Directory.Delete(confDir, true);
            return true;
        }

    }

    static class Utils
    {
        public static T CastByExample<T>(this object o, T example)
        {
            return (T)o;
        }
    }
}