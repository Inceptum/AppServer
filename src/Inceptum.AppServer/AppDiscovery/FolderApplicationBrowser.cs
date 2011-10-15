using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.Core.Logging;
using Mono.Cecil;

namespace Inceptum.AppServer.AppDiscovery
{
    public static class CeceilExtencions
    {
        public static AssemblyDefinition TryReadAssembly(string file)
        {
            try
            {
                return AssemblyDefinition.ReadAssembly(file);
            }
            catch
            {
                return null;
            }
        }
        public static AssemblyDefinition TryReadAssembly(Stream assemblyStream)
        {
            try
            {
                return AssemblyDefinition.ReadAssembly(assemblyStream);
            }
            catch
            {
                return null;
            }
        }
    }

    public class FolderApplicationBrowser : IApplicationBrowser
    {
        private readonly string m_Folder;
        private ILogger m_Logger = NullLogger.Instance;

        public FolderApplicationBrowser(string folder)
        {
            if (folder == null) throw new ArgumentNullException("folder");
            if (!Directory.Exists(folder)) throw new ArgumentException("folder", string.Format("Folder '{0}' not found", folder));
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            m_Folder = folder;
        }

        public ILogger Logger
        {
            get { return m_Logger; }
            set { m_Logger = value ?? NullLogger.Instance; }
        }

        #region IApplicationBrowser Members

        public IEnumerable<HostedAppInfo> GetAvailabelApps()
        {
            var apps = (from file in Directory.GetFiles(m_Folder, "*.dll")
                       let asm = CeceilExtencions.TryReadAssembly(file)
                       where asm != null
                       let attribute = asm.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == typeof(HostedApplicationAttribute).FullName)
                       where attribute != null
                       let name = attribute.ConstructorArguments.First().Value.ToString()
                       select new { assembly = asm, file ,name});
            foreach (var app in apps)
            {

                if (app == null)
                    continue;
                TypeDefinition[] appTypes = app.assembly.MainModule.Types.Where(t => t.Interfaces.Any(i => i.FullName == typeof (IHostedApplication).FullName)).ToArray();
                if (appTypes.Length == 0)
                    continue;

                TypeDefinition appType = appTypes.First();
                if (appTypes.Length > 1)
                    Logger.InfoFormat("Assembly {0} contains several types implementing IHostedApplication, using {1}", app.file, appType.Name);

                yield return new HostedAppInfo(app.name,app.assembly.Name.Version, appType.FullName + ", " + app.assembly.FullName, m_Folder, new[] {app.file}, new string[0]);
            }

        }

        #endregion

     /*   public IEnumerable<HostedAppInfo> GetAvailabelApps()
        {
            var discoveredApps = new List<HostedAppInfo>();
            AppDomain appDomain = AppDomain.CreateDomain("AssemblyExplorer", null, new AppDomainSetup
                                                                                       {
                                                                                           PrivateBinPath = m_Folder,
                                                                                           ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase
                                                                                       });

            try
            {
                var tester = (AssemblyTester) appDomain.CreateInstanceAndUnwrap(typeof (AssemblyTester).Assembly.FullName, typeof (AssemblyTester).FullName);
                foreach (string file in Directory.GetFiles(m_Folder, "*.dll"))
                {
                    try
                    {
                        HostedAppInfo hostedAppInfo = tester.Try(file);
                        if (hostedAppInfo == null)
                            continue;
                        if (hostedAppInfo.AppType == null)
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
*/
        public static AppInfo ReadAppInfo(AssemblyDefinition assembly)
        {
            CustomAttribute attr = assembly.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "HostedApplicationAttribute");
            if (attr == null)
                return null;
            
            
            return new AppInfo(attr.ConstructorArguments.First().Value.ToString(), assembly.Name.Version );
        }
    }
}