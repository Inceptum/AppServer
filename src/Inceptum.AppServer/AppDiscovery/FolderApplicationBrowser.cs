using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Castle.Core.Logging;
using Inceptum.AppServer.Model;
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
        private readonly string[] m_Folders;
        private ILogger m_Logger = NullLogger.Instance;
        private string[] m_NativeDlls;

        public string Name
        {
            get { return "FileSystem"; }
        }

        public FolderApplicationBrowser(string[] folders, string[] nativeDlls)
        {
            if (folders == null) throw new ArgumentNullException("folders");
            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder)) throw new ArgumentException("folder", string.Format("Folder '{0}' not found", folder));
            }


            m_NativeDlls = nativeDlls;

            m_Folders = folders.Select(folder => Path.GetFullPath(folder)).ToArray();
        }

        public ILogger Logger
        {
            get { return m_Logger; }
            set { m_Logger = value ?? NullLogger.Instance; }
        }

        #region IApplicationBrowser Members

        public IEnumerable<HostedAppInfo> GetAvailableApps()
        {
            return m_Folders.SelectMany(GetAvailabelAppsInFolder);
        }

        public ApplicationParams GetApplicationParams(string application, Version version)
        {
            throw new NotImplementedException();
        }

        #endregion
        public IEnumerable<HostedAppInfo> GetAvailabelAppsInFolder(string folder)
        {
            var apps = (from file in Directory.GetFiles(Path.GetFullPath(folder), "*.dll").Concat(Directory.GetFiles(Path.GetFullPath(folder), "*.exe"))
                        let asm = CeceilExtencions.TryReadAssembly(file)
                        where asm != null
                        let attribute = asm.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == typeof(HostedApplicationAttribute).FullName)
                        where attribute != null
                        let name = attribute.ConstructorArguments.First().Value.ToString()
                        let  vendor = attribute.ConstructorArguments.Count==2?attribute.ConstructorArguments[1].Value.ToString():HostedApplicationAttribute.DEFAULT_VENDOR
                        select new { assembly = asm, file, name, vendor });

            var assemblies =
                from file in 
                        Directory.GetFiles(Path.GetFullPath(folder), "*.dll")
                        .Concat(Directory.GetFiles(Path.GetFullPath(folder), "*.exe"))
                        .Concat(Directory.GetDirectories(folder).SelectMany(dir => Directory.GetFiles(dir,"*.resources.dll")))
                let asm = CeceilExtencions.TryReadAssembly(file)
                where asm != null
                select new {assemblyName = asm.FullName, file};
            var assembliesToLoad  = assemblies.ToDictionary(a => new AssemblyName(a.assemblyName), a => a.file);
            string appVendor=null;
            foreach (var app in apps)
            {

                if (app == null)
                    continue;
                TypeDefinition[] appTypes = app.assembly.MainModule.Types.Where(t => t.Interfaces.Any(i => i.FullName == typeof(IHostedApplication).FullName)).ToArray();
                if (appTypes.Length == 0)
                    continue;

                TypeDefinition appType = appTypes.First();
                if (appTypes.Length > 1)
                    Logger.InfoFormat("Assembly {0} contains several types implementing IHostedApplication, using {1}", app.file, appType.Name);

                try
                {
                    appVendor =app.assembly.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == typeof (AssemblyCompanyAttribute).FullName).ConstructorArguments[0].Value.ToString();
                }catch
                {
                    appVendor = null;
                }


                yield return
                    new HostedAppInfo(app.name, app.vendor, app.assembly.Name.Version,
                        appType.FullName + ", " + app.assembly.FullName, assembliesToLoad,
                        m_NativeDlls.Where(dll => File.Exists(Path.Combine(folder, dll))).ToArray())
                    {
                        Vendor = appVendor,
                        ConfigFiles = Directory.GetFiles(folder, "*.config"),
                        Debug = true
                    };
            }
        }

       

        public static AppInfo ReadAppInfo(AssemblyDefinition assembly)
        {
            CustomAttribute attr = assembly.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "HostedApplicationAttribute");
            if (attr == null)
                return null;
            
            
            return new AppInfo(attr.ConstructorArguments.First().Value.ToString(), assembly.Name.Version );
        }
    }
}