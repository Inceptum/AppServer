using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Newtonsoft.Json.Linq;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Extensions;

namespace Inceptum.AppServer.TestApp
{
    public class TestConf
    {
        public TestConf(string value)
        {
            Value = value;
        }

        public string Value
        {
            get; private set; }
    }


    class TestDocument
    {
        public string Id { get; set; } 
        public string Value { get; set; } 
    }
    public class TestApp:IHostedApplication
    {
        private TestConf m_Config;
        private ILogger m_Logger;
        private JObject m_JObject;
        private AppServerContext m_Context;

        public TestApp(ILogger logger,TestConf config,AppServerContext context)
        {
            m_Context = context;
            m_Logger = logger??NullLogger.Instance;
            m_Config = config;
        }

        public void Start()
        {
            File.WriteAllText("test.txt","ПЫЩ ПЫЩ ПЫЩ!!!");
            m_Logger.InfoFormat("Test App");
            m_Logger.InfoFormat("CurrentDirectory: {0}",System.IO.Directory.GetCurrentDirectory());
            m_Logger.InfoFormat("Value from config: '{0}'", m_Config.Value);
            m_Logger.InfoFormat("Value from app.config: '{0}'", ConfigurationManager.AppSettings["appConfigSetting"]);
            m_Logger.InfoFormat("Application assembly version: '{0}'", typeof(TestApp).Assembly.GetName().Version);
            m_JObject = JObject.Parse("{}");
            bool fail;
            if (bool.TryParse(ConfigurationManager.AppSettings["fail"], out fail) && fail)
            {
                new Thread(() =>
                {
                    m_Logger.Error("FAIL!!!");
                    throw new Exception();
                }).Start();
            }

            //var documentStore = new DocumentStore { Url = "http://localhost:" + m_Config.WebUIPort + "/" };
            var documentStore = new DocumentStore { Url = m_Context.RavenUrl };
            documentStore.Initialize();

            documentStore.DatabaseCommands.EnsureDatabaseExists("TestApp");
            using (IDocumentSession session = documentStore.OpenSession("TestApp"))
            {
                var model = new TestDocument() { Value = Guid.NewGuid().ToString() };
                session.Store(model, Guid.NewGuid().ToString());
                session.SaveChanges();
            }


            bool hangOnStart;
            if (bool.TryParse(ConfigurationManager.AppSettings["hangOnStart"], out hangOnStart) && hangOnStart)
            {
                Console.ReadLine();
            }
            m_Logger.InfoFormat("log record");
            m_Logger.DebugFormat("Debug");
            m_Logger.Warn("Warning");
            m_Logger.Error("Error");

        }

        public string DoSomething(DateTime dateValue, string stringValue, int intValue, decimal decimalValue, bool boolValue)
        {
            return string.Format("Something is done. Date {0}", dateValue);
        }
    }
}
