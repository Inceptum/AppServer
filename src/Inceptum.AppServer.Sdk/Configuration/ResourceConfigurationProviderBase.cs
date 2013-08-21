using System;
using System.Linq;

namespace Inceptum.AppServer.Configuration
{
    public abstract class ResourceConfigurationProviderBase : IConfigurationProvider
    {
        #region IConfigurationProvider Members

        public virtual string GetBundle(string configuration, string bundleName, params string[] extraParams)
        {
            NormalizeParams(ref configuration,ref bundleName, extraParams);
            try
            {
                return GetResourceContent(GetResourceName(configuration, bundleName, extraParams.Select(p => p.Trim()).ToArray()));
            }
            catch
            {
                return null;
            }
        }

        #endregion

        protected void NormalizeParams(ref string configuration,ref string bundleName, string[] extraParams)
        {
            if (String.IsNullOrWhiteSpace(configuration)) throw new ArgumentException("Empty configuration name provided", "configuration");
            if (String.IsNullOrWhiteSpace(bundleName)) throw new ArgumentException("Empty bundle name provided", "bundleName");
            bundleName = bundleName.Trim();
            configuration = configuration.Trim();

            if (extraParams.Where(String.IsNullOrWhiteSpace).Any()) throw new ArgumentException("Empty param provided", "extraParams");
        }

        protected internal abstract string GetResourceName(string configuration,string bundleName, params string[] extraParams);
        protected internal abstract string GetResourceContent(string name);
       
    }
}
