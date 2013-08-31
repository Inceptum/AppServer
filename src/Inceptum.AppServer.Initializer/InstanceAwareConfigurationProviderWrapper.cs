using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Inceptum.AppServer.Configuration;

namespace Inceptum.AppServer.Hosting
{
    internal class InstanceAwareConfigurationProviderWrapper : IConfigurationProvider
    {
        private readonly IConfigurationProvider m_ConfigurationProvider;
        private readonly IDictionary<string, string> m_InstanceParams;
        private readonly Regex m_InstanceParamRegex;

        public InstanceAwareConfigurationProviderWrapper(IConfigurationProvider configurationProvider, object instanceParams)
        {
            if (configurationProvider == null) throw new ArgumentNullException("configurationProvider");
            if (instanceParams == null) throw new ArgumentNullException("instanceParams");
            m_ConfigurationProvider = configurationProvider;
            m_InstanceParams = extractParamValues(instanceParams);
            m_InstanceParamRegex = createRegex(m_InstanceParams);
        }

        //TODO[MT]: methods (replaceParams, createRegex and extractParamValues) should be moved to some internal "Utils" namespace (same logic is used inside ConfigurationFacility)
        private static Regex createRegex(IDictionary<string, string> paramsDictionary)
        {
            var r = String.Format("^{0}$", String.Join("|", paramsDictionary.Keys.Select(x => "\\{" + x + "\\}")));
            return new Regex(r, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
        }

        private static Dictionary<string, string> extractParamValues(object values)
        {
            var vals = values
                .GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead)
                .Select(property => new { key = property.Name, value = property.GetValue(values, null) });


            return vals.ToDictionary(o => o.key, o => (o.value ?? "").ToString());
        }

        private string replaceParams(string paramName, bool strict = true)
        {
            if (!strict && (m_InstanceParamRegex == null || !m_InstanceParamRegex.IsMatch(paramName)))
                return paramName;
            return m_InstanceParams.Aggregate(paramName,
                (current, param) =>
                    current.Replace(String.Format("{{{0}}}", param.Key), param.Value));
        }

        public string GetBundle(string configuration, string bundleName, params string[] parameters)
        {
            var extraParams = parameters.Select(x => replaceParams(x)).ToArray();
            bundleName = replaceParams(bundleName, false);

            return m_ConfigurationProvider.GetBundle(configuration, bundleName, extraParams);
        }
    }
}