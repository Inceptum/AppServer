using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Conversion;
using Inceptum.AppServer.Configuration.Convertion;
using Inceptum.AppServer.Configuration.Providers;
using Newtonsoft.Json.Linq;

namespace Inceptum.AppServer.Configuration
{
    public class ConfigurationFacility : AbstractFacility, IConfigurationFacility
    {
        private string m_DefaultConfiguration;
        private string m_ServiceUrl;
        private string m_Path;
        private Dictionary<string, string> m_Params = new Dictionary<string, string>();
        private IConfigurationProvider m_Provider;
        private Regex m_ParamRegex;
        private readonly List<Action<IKernel>> m_InitSteps = new List<Action<IKernel>>();

        internal IConfigurationProvider Provider
        {
            get { return m_Provider; }
            set { m_Provider = value; }
        }

        private IConversionManager ConversionManager
        {
            get { return (IConversionManager) Kernel.GetSubSystem(SubSystemConstants.ConversionManagerKey); }
        }

        private IJsonObjectAccessor JsonObjectAccessor
        {
            get { return Kernel.Resolve<IJsonObjectAccessor>(); }
        }

        protected override void Init()
        {
            if(Kernel.HasComponent(typeof(InstanceContext)))
            {
                string defaultConfiguration = Kernel.Resolve<InstanceContext>().DefaultConfiguration;
                if(!string.IsNullOrEmpty(defaultConfiguration))
                    m_DefaultConfiguration = defaultConfiguration;
            }

            if (!Kernel.HasComponent(typeof (IJsonObjectAccessor)))
            {
                Kernel.Register(
                    Component.For<IJsonObjectAccessor>()
                        .ImplementedBy<JsonObjectAccessor>()
                        .LifeStyle.Singleton
                    );
            }

            if (m_DefaultConfiguration == null)
                throw new ConfigurationErrorsException("ConfigurationFacility is not set up correctly. You have to provide DefaultConfiguration");

            if (m_Provider == null)
            {
                if (string.IsNullOrWhiteSpace(m_Path))
                    m_Path = ".";

                if (m_ServiceUrl != null)
                {
                    Kernel.Register(Component.For<IConfigurationProvider>().ImplementedBy<CachingRemoteConfigurationProvider>().DependsOn(new { serviceUrl = m_ServiceUrl, path = m_Path }).IsDefault());
                }
                else if (!Kernel.HasComponent(typeof(IConfigurationProvider)))
                        throw new ConfigurationErrorsException("IConfigurationProvider not found. Register it before registering ConfigurationFacility");


                m_Provider = Kernel.Resolve<IConfigurationProvider>();
            }
            else
            {
                Kernel.Register(Component.For<IConfigurationProvider>().Instance(m_Provider));
            }
            Kernel.Register(Component.For<IConfigurationFacility>().Instance(this));

            Kernel.ComponentModelCreated += onComponentModelCreated;

            foreach (var initStep in m_InitSteps)
            {
                initStep(Kernel);
            }
        }
        
        public T DeserializeFromBundle<T>(string configuration,string bundleName, string jsonPath, IEnumerable<string> parameters)
        {
            var extraParams = parameters.Select(x => replaceParams(x)).ToArray();
            bundleName = replaceParams(bundleName, false);
            var configurationName = getConfigurationName(configuration);

            var bundle = m_Provider.GetBundle(configurationName,bundleName, extraParams);
            if (bundle == null)
            {
                throw new ConfigurationErrorsException(string.Format("bundle '{0}' not found", bundleName));
            }

            var token = JsonObjectAccessor.SelectToken(bundle, jsonPath);
            if (token == null)
            {
                throw new ConfigurationErrorsException(string.Format("property path {0} in bundle '{1}' not found", jsonPath, bundleName));
            }
            return (T)JsonObjectAccessor.ConvertTo(token, typeof(T));
        }

        public ConfigurationFacility Configuration(string configurationName)
        {
            if (!ValidationHelper.IsValidBundleName(configurationName))
                throw new ArgumentException("configuration " + ValidationHelper.INVALID_NAME_MESSAGE,
                                            "configurationName");
            m_DefaultConfiguration = configurationName;
            return this;
        }

        public ConfigurationFacility Remote(string serviceUrl, string cachePath=null)
        {
            if (!ValidationHelper.IsValidUrl(serviceUrl))
                throw new ArgumentException("Invalid url");
            m_ServiceUrl = serviceUrl;
            m_Path = cachePath;
            return this;
        }

        public ConfigurationFacility Path(string path)
        {
            m_Path = path;

            return this;
        }

        public ConfigurationFacility Params(object values)
        {
            var vals = values
                .GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead)
                .Select(property => new { key = property.Name, value = property.GetValue(values, null) });


            m_Params = vals.ToDictionary(o => o.key, o => (o.value??"").ToString());

            string r = string.Format("{0}", string.Join("|", m_Params.Keys.Select(x => "\\{" + x + "\\}")));
            m_ParamRegex = new Regex(r, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return this;
        }

        public void AddInitStep(Action<IKernel> step)
        {
            m_InitSteps.Add(step);
        }

        private void onComponentModelCreated(ComponentModel component)
        {
            var dependsOnBundle = (string)component.ExtendedProperties["dependsOnBundle"];
            if (dependsOnBundle != null)
            {
                var configuration = getConfigurationName((string)component.ExtendedProperties["configuration"]);
                var jsonPath = (string)component.ExtendedProperties["jsonPath"];
                var parameters = (string[])component.ExtendedProperties["bundleParameters"];
                var values = DeserializeFromBundle<Dictionary<string, object>>(configuration, dependsOnBundle, jsonPath, parameters);
                foreach (var keyValuePair in values)
                {
                    object value = changeValueTypeIfNeeded(component, keyValuePair.Key, keyValuePair.Value);

                    component.CustomDependencies[keyValuePair.Key] = value;
                }
            }
        }

        private object changeValueTypeIfNeeded(ComponentModel component, string key, object value)
        {
            if (value != null)
            {
                Func<DependencyModel, bool> dependencyPredicate = dm => string.Equals(dm.DependencyKey, key, StringComparison.InvariantCultureIgnoreCase);

                DependencyModel dependency = component.Dependencies.FirstOrDefault(dependencyPredicate);
                if (dependency == null)
                {
                    dependency = component.Constructors
                        .SelectMany(c => c.Dependencies)
                        .Where(dependencyPredicate)
                        .FirstOrDefault();
                }

                if (dependency != null)
                {
                    if (dependency.TargetItemType != value.GetType())
                    {
                        var jToken = value as JToken;
                        if (jToken != null)
                        {
                            value = JsonObjectAccessor.ConvertTo(jToken, dependency.TargetItemType);
                        }
                        else if (ConversionManager.CanHandleType(dependency.TargetItemType))
                        {
                            value = ConversionManager.PerformConversion(value.ToString(), dependency.TargetItemType);
                        }
                    }
                }
            }

            return value;
        }

        private string replaceParams(string paramName, bool strict = true)
        {
            if (!strict && (m_ParamRegex == null || !m_ParamRegex.IsMatch(paramName)))
            {
                return paramName;
            }

            return m_Params.Aggregate(paramName, (current, param) => current.Replace(string.Format("{{{0}}}", param.Key), param.Value));
        }

        private string getConfigurationName(string configuration)
        {
            string configurationName = configuration ?? m_DefaultConfiguration;
            if (configurationName == null)
                throw new ConfigurationErrorsException(
                    "Configuration is not provided, provide it explicitley in component registration or as default in ConfigurationFacility registration");
            return configurationName;
        }
    }
}