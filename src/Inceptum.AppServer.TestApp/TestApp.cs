using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.Core.Logging;
using Newtonsoft.Json.Linq;

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
    public class TestApp:IHostedApplication
    {
        private TestConf m_Config;
        private ILogger m_Logger;
        private JObject m_JObject;

        public TestApp(ILogger logger,TestConf config)
        {
            m_Logger = logger??NullLogger.Instance;
            m_Config = config;
        }

        public void Start()
        {
            m_Logger.InfoFormat("Test App v1.0.1");
            m_Logger.InfoFormat("Value from config: '{0}'",m_Config.Value);
            m_JObject = JObject.Parse("{}");
        }
    }
}
