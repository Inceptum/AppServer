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
using Inceptum.AppServer.Configuration.Providers;
using Newtonsoft.Json;
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

        protected override void Init()
        {
            if(Kernel.HasComponent(typeof(InstanceContext)))
            {
                string defaultConfiguration = Kernel.Resolve<InstanceContext>().DefaultConfiguration;
                if(!string.IsNullOrEmpty(defaultConfiguration))
                    m_DefaultConfiguration = defaultConfiguration;
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
                throw new ConfigurationErrorsException(string.Format("bundle '{0}' not found", bundleName));

            var token = JObject.Parse(bundle).SelectToken(jsonPath);
            if (token == null)
                throw new ConfigurationErrorsException(string.Format("property path {0} in bundle '{1}' not found",
                                                                     jsonPath, bundleName));
            return JsonConvert.DeserializeObject<T>(token.ToString());
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

        private void onComponentModelCreated(ComponentModel model)
        {
            var dependsOnBundle = (string)model.ExtendedProperties["dependsOnBundle"];
            if (dependsOnBundle == null)
            {
                return;
            }

            var configuration = getConfigurationName((string)model.ExtendedProperties["configuration"]);
            var jsonPath = (string)model.ExtendedProperties["jsonPath"];
            var parameters = (string[])model.ExtendedProperties["bundleParameters"];
            var values = DeserializeFromBundle<Dictionary<string, object>>(configuration, dependsOnBundle, jsonPath, parameters);

            Lazy<IConversionManager> conversionManagerLazy = new Lazy<IConversionManager>(() => (IConversionManager)Kernel.GetSubSystem(SubSystemConstants.ConversionManagerKey));

            foreach (var keyValuePair in values)
            {
                object value = keyValuePair.Value;
                if (value is long)
                {
                    var valueLong = (long)value;
                    if (int.MinValue < valueLong && valueLong < int.MaxValue)
                    {
                        value = (int)valueLong;
                    }
                }
                else
                {
                    // main dependency or ctor dependency
                    var dependency = model.Dependencies.FirstOrDefault(d => string.Equals(d.DependencyKey, keyValuePair.Key, StringComparison.InvariantCultureIgnoreCase));
                    dependency = dependency ?? model.Constructors.SelectMany(c => c.Dependencies.Where(d => string.Equals(d.DependencyKey, keyValuePair.Key, StringComparison.InvariantCultureIgnoreCase))).FirstOrDefault();
                    if (dependency != null)
                    {
                        if (value is JObject || value is JArray)
                        {
                            value = JsonConvert.DeserializeObject(value.ToString(), dependency.TargetItemType);
                        }
                        else
                        {
                            if (value != null)
                            {
                                if (dependency.TargetItemType != value.GetType())
                                {
                                    if (conversionManagerLazy.Value.CanHandleType(dependency.TargetItemType))
                                    {
                                        value = conversionManagerLazy.Value.PerformConversion<TimeSpan>(value.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
                model.CustomDependencies[keyValuePair.Key] = value;
            }
        }

        private string replaceParams(string paramName, bool strict = true)
        {
            if (!strict && (m_ParamRegex == null || !m_ParamRegex.IsMatch(paramName)))
                return paramName;
            return m_Params.Aggregate(paramName,
                                      (current, param) =>
                                      current.Replace(string.Format("{{{0}}}", param.Key), param.Value));
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