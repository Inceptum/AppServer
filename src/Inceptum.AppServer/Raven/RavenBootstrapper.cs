using System;
using System.IO;
using System.Reflection;
using Castle.Core;
using Castle.Core.Logging;
using Inceptum.AppServer.Configuration;
using Newtonsoft.Json.Linq;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;
using Raven.Client.Extensions;
using Raven.Client.Indexes;

namespace Inceptum.AppServer.Raven
{
    public sealed class RavenBootstrapper : IDisposable

    {
        private readonly RavenConfig m_Config;
        private readonly ILogger m_Logger;
        private readonly Assembly[] m_IndexLookupAssemblies;
        private Lazy<EmbeddableDocumentStore> m_Store;
        public RavenBootstrapper(IManageableConfigurationProvider configurationProvider,ILogger logger)
        {
            Assembly[] indexLookupAssemblies = null;
            if (logger == null) throw new ArgumentNullException("logger");
            m_Logger = logger;
            try
            {
                
                var bundleString = configurationProvider.GetBundle("AppServer", "server.host", "{environment}", "{machineName}");
                dynamic bundle = JObject.Parse(bundleString).SelectToken("Raven");
                m_Config = new RavenConfig()
                {
                    WebUIPort = bundle.port,
                    BaseDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Raven"),
                    RunInMemory = false,
                    WebUIEnabled = bundle.webUIEnabled
                };

            }
            catch (Exception e)
            {
                m_Logger.WarnFormat( "Failed to get raven configuration , using default");
                m_Config = new RavenConfig()
                {
                    WebUIPort = 9233,
                    BaseDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Raven"),
                    RunInMemory = false,
                    WebUIEnabled = true
                };
            }
            

            m_Logger = logger;
            m_IndexLookupAssemblies = indexLookupAssemblies ?? new Assembly[0];
            m_Store=new Lazy<EmbeddableDocumentStore>(createStore);
        }

        private EmbeddableDocumentStore createStore()
        {
            m_Logger.InfoFormat("Initializing RavenDB in {0} ...", m_Config.DataDir);

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var store = new EmbeddableDocumentStore
            {
                DataDirectory = m_Config.DataDir,
                EnlistInDistributedTransactions = false,
                UseEmbeddedHttpServer = m_Config.WebUIEnabled,
                Conventions =
                {
                    DefaultQueryingConsistency = ConsistencyOptions.None,
                    DisableProfiling = true
                }
            };
            store.Configuration.Port = m_Config.WebUIPort;
            store.Configuration.PluginsDirectory = m_Config.PluginsDir;
            store.Configuration.RunInMemory = m_Config.RunInMemory;
            store.Configuration.CreateAnalyzersDirectoryIfNotExisting = false;
            store.Configuration.CompiledIndexCacheDirectory = m_Config.CompiledIndexCacheDir;

            store.Configuration.MemoryCacheLimitMegabytes = 256;
            store.Configuration.Settings["Raven/Esent/CacheSizeMax"] = "256";
            store.Configuration.Settings["Raven/Esent/MaxVerPages"] = "128";
            store.Configuration.MaxNumberOfItemsToIndexInSingleBatch = 1024;
            store.Configuration.DisableClusterDiscovery = true;

            

            store.Initialize();

            foreach (var assembly in m_IndexLookupAssemblies)
            {
                IndexCreation.CreateIndexes(assembly, store);
            }

            sw.Stop();

            m_Logger.InfoFormat("RavenDB initialized in {0:0.00} seconds", ((double)sw.ElapsedMilliseconds) / 1000);

            return store;
        }

        public IDocumentStore Store
        {
            get { return m_Store.Value; }
        }

        public void Dispose()
        {
            if (m_Store != null && m_Store.IsValueCreated)
            {
                m_Store.Value.Dispose();
                m_Store = null;
                m_Logger.Info("RavenDb is stopped");
            }
        }


        public string Start()
        {
            return m_Store.Value.Configuration.ServerUrl;
        }
    }

    public class RavenConfig
    {
        public bool RunInMemory { get; set; }
        public string BaseDir { get; set; }
        public bool WebUIEnabled { get; set; }
        public int WebUIPort { get; set; }

        public string DataDir
        {
            get { return Path.Combine(BaseDirFullPath, "database"); }
        }

        public string PluginsDir
        {
            get { return Path.Combine(BaseDirFullPath, "plugins"); }
        }

        public string CompiledIndexCacheDir
        {
            get { return Path.Combine(BaseDirFullPath, "compiled-indexes"); }
        }

        private string BaseDirFullPath
        {
            get { return Path.IsPathRooted(BaseDir) ? BaseDir : Path.Combine(Directory.GetCurrentDirectory(), BaseDir); }
        }
 
    }
}