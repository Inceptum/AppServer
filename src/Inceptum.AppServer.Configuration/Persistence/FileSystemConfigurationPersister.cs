using System;
using System.Collections;
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


        public Config Load(string name, IContentProcessor contentProcessor)
        {
            if(!ValidationHelper.IsValidBundleName(name))
            {
                throw new ArgumentException(ValidationHelper.INVALID_NAME_MESSAGE, "name");
            }
            name = name.ToLower();
            var confDir = Path.Combine(ConfigFolder,name);
            if(!Directory.Exists(confDir))
            {
//                throw new ConfigurationErrorsException("Configuration not found");
                Directory.CreateDirectory(confDir);
            }

            var conf = new Config(contentProcessor, name);
            var files = Directory.EnumerateFiles(confDir).Select(f => new { path = f, name = Path.GetFileName(f).ToLower() }).OrderBy(x => x.name);
            var enumerator = files.GetEnumerator();
            if (enumerator.MoveNext()) 
                createBundles(conf, enumerator,true);
            return conf;
        }

        private static bool createBundles(BundleCollectionBase collection,IEnumerator enumerator,bool isRootLevel=false)
        {
            while (true)
            {
                var file = enumerator.Current.CastByExample(new {path = "", name = ""});
                if (!isRootLevel && !file.name.StartsWith(collection.Name+'.'))
                    return true;


                var name = isRootLevel ? file.name : file.name.Substring(collection.Name.Length+1);
                if (name.Contains('.'))
                {
                    //Implicitly defined bundle. e.g. bundl1.bundle2 file exists but bundle1 file does not. bundle1 should be created but with empty content
                    var intermidiate = collection.CreateBundle(name.Split('.').First());
                    if (!createBundles(intermidiate, enumerator))
                        return false;
                }
                else
                {
                    
                    Bundle bundle;
                    try
                    {
                        bundle = collection.CreateBundle(name, File.ReadAllText(file.path));
                    }
/*                    catch (JsonReaderException e)
                    {
                        throw new ConfigurationErrorsException(string.Format("Failed to parse bundle {0}.\r\n File path {1}.\r\n Error: {2}", file.name, file.path, e.Message));
                    }*/
                    catch (Exception e)
                    {

                        throw new ConfigurationErrorsException(string.Format("Failed to parse bundle {0}.\r\n File path {1}", file.name, file.path), e);
                    }
                    
                    if (!enumerator.MoveNext())
                        return false;
                    if (!createBundles(bundle, enumerator))
                        return false;
                }
            }
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