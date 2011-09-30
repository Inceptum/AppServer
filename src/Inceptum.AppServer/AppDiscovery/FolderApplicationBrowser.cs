using System;
using System.Collections.Generic;
using System.IO;
using Castle.Core.Logging;

namespace Inceptum.AppServer.AppDiscovery
{
    public class FolderApplicationBrowser : IApplicationBrowser
    {
        private readonly string m_Folder;
        private ILogger m_Logger=NullLogger.Instance;

        public ILogger Logger
        {
            get { return m_Logger; }
            set { m_Logger = value??NullLogger.Instance; }
        }

        public FolderApplicationBrowser(string folder)
        {
            if (folder == null) throw new ArgumentNullException("folder");
            if (!Directory.Exists(folder)) throw new ArgumentException("folder",string.Format("Folder '{0}' not found",folder));
            m_Folder = folder;
        }

        public IEnumerable<HostedAppInfo> GetAvailabelApps()
        {
            var discoveredApps=new List<HostedAppInfo>();
            var appDomain = AppDomain.CreateDomain("AssemblyExplorer", null, new AppDomainSetup
            {
                PrivateBinPath = m_Folder,
                ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase
            });

            try
            {
                var tester = (AssemblyTester)appDomain.CreateInstanceAndUnwrap(typeof(AssemblyTester).Assembly.FullName, typeof(AssemblyTester).FullName);
                foreach (var file in Directory.GetFiles(m_Folder, "*.dll"))
                {
                    try
                    {
                        var hostedAppInfo = tester.Try(file);
                        if (hostedAppInfo == null)
                            continue;
                        if(hostedAppInfo.AppType==null)
                            m_Logger.Warn("Application {0} is marked with HostedApplicationAttribute but does not contain IHostedApplication implementation. Ignoring");
                        else
                            discoveredApps.Add(hostedAppInfo);
                    }
                    catch (Exception e)
                    {
                        m_Logger.WarnFormat(e, "Failed to determine whether {0} is application assembly", file);
                    }
                }


            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
            return discoveredApps.ToArray();
        }
    }
}