using System;
using System.IO;
using System.Text;
using Castle.Core.Logging;
using NLog;
using NLog.Config;

namespace Inceptum.AppServer.Logging
{
    /// <summary>
    /// NLogger factory with smart formatting generic-based loggers
    /// </summary>    
    /// <remarks>
    /// Refer to IB-220 for details
    /// </remarks>
    public class GenericsAwareNLoggerFactory : Castle.Services.Logging.NLogIntegration.ExtendedNLogFactory
    {
        public GenericsAwareNLoggerFactory() { }

        public GenericsAwareNLoggerFactory(bool configuredExternally) : base(configuredExternally)
        {
        }
        public GenericsAwareNLoggerFactory(string configFile, Action<LoggingConfiguration> updateConfig)
            : base(prepareConfig(configFile, updateConfig))
        {
        }

        private static LoggingConfiguration prepareConfig(string configFile, Action<LoggingConfiguration> updateConfig)
        {
            LoggingConfiguration configuration=null;

            if (configFile != null && File.Exists(GetConfigFile(configFile).FullName))
                configuration = new XmlLoggingConfiguration(GetConfigFile(configFile).FullName);

            if (configuration == null)
            {
                configuration=new LoggingConfiguration();
            }
            updateConfig(configuration);
            return configuration;
        }

        public GenericsAwareNLoggerFactory(string configFile) : base(configFile) { }

        public GenericsAwareNLoggerFactory(LoggingConfiguration loggingConfiguration) : base(loggingConfiguration) { }

        public override IExtendedLogger Create(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return base.Create(GetLoggerName(type));
        }

        protected virtual string GetLoggerName(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return !type.IsGenericType ? type.FullName : getDisplayableTypeName(type);
        }

        string getDisplayableTypeName(Type type)
        {
            if (!type.IsGenericType) return type.Name;

            var sb = new StringBuilder();
            foreach (var argType in type.GetGenericArguments())
            {
                if (sb.Length == 0)
                    sb.Append(getDisplayableTypeName(argType));
                else
                    sb.Append(",").Append(getDisplayableTypeName(argType));
            }

            return string.Format("{0}<{1}>", type.Name, sb);
        }
    }
}