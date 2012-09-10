using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Inceptum.AppServer.Hosting
{
    internal class ApplicationHost 
    {
        public static IApplicationHost Create(AppServerContext appServerContext,HostedAppInfo appInfo)
        {
            string path = Path.GetFullPath(Path.Combine(appServerContext.AppsDirectory, appInfo.Name));
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var domain = AppDomain.CreateDomain(appInfo.Name, null, new AppDomainSetup
                                                                              {
                                                                                  ApplicationBase = path,//AppDomain.CurrentDomain.BaseDirectory,
                                                                                  PrivateBinPathProbe = null,
                                                                                  DisallowApplicationBaseProbing = true,
                                                                                  ConfigurationFile = appInfo.ConfigFile
                                                                              });
            domain.Load(typeof (HostedAppInfo).Assembly.GetName());
            var appDomainInitializer = (AppDomainInitializer) domain.CreateInstanceFromAndUnwrap(typeof (AppDomainInitializer).Assembly.Location, typeof (AppDomainInitializer).FullName, false, BindingFlags.Default, null, null, null, null);
            

            
            appDomainInitializer.Initialize(path,appInfo.AssembliesToLoad, appInfo.NativeDllToLoad.ToArray());
            return appDomainInitializer.CreateHost(appInfo);
        }
    }
}