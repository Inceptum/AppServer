using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Inceptum.AppServer.Hosting
{
    internal class ApplicationHost 
    {
        public static IApplicationHost Create(HostedAppInfo appInfo)
        {
            var domain = AppDomain.CreateDomain(appInfo.Name, null, new AppDomainSetup
                                                                              {
                                                                                  ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                                                                                  PrivateBinPathProbe = null,
                                                                                  DisallowApplicationBaseProbing = true,
                                                                                  ConfigurationFile = appInfo.ConfigFile
                                                                              });
            domain.Load(typeof (HostedAppInfo).Assembly.GetName());
            var appDomainInitializer = (AppDomainInitializer) domain.CreateInstanceFromAndUnwrap(typeof (AppDomainInitializer).Assembly.Location, typeof (AppDomainInitializer).FullName, false, BindingFlags.Default, null, null, null, null);
            appDomainInitializer.Initialize(appInfo.AssembliesToLoad, appInfo.NativeDllToLoad.ToArray());
            return appDomainInitializer.CreateHost(appInfo);
        }
    }
}