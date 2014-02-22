using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Castle.Core.Logging;

namespace Inceptum.AppServer.AppDiscovery
{
    public class FolderApplicationRepository : IApplicationRepository
    {
        private string[] m_Folders;
        private ILogger m_Logger;

        public string Name
        {
            get { return "FileSystem"; }
        }
        public FolderApplicationRepository(string[] folders,ILogger logger)
        {
            m_Logger = logger;
            m_Folders = folders;
        }

        public IEnumerable<ApplicationInfo> GetAvailableApps()
        {
            return m_Folders.Select(f=>getApplication(f).Item1);
        }

        private Tuple<ApplicationInfo,string> getApplication(string folder)
        {
            var dlls = new[] { "*.dll", "*.exe" }
                .SelectMany(searchPattern => Directory.GetFiles(folder, searchPattern))
                .Select(file => new { path = file, AssemblyDefinition = Hosting.CeceilExtencions.TryReadAssembly(file) }).ToArray();

            var apps =( from file in dlls
                let asm = file.AssemblyDefinition
                where asm != null
                let appAttribute = asm.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == typeof (HostedApplicationAttribute).FullName)
                where appAttribute != null
                let applicationId = appAttribute.ConstructorArguments[0].Value.ToString()
                let vendorAttribute = asm.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == typeof (AssemblyCompanyAttribute).FullName)
                let vendor = vendorAttribute == null ? null : vendorAttribute.ConstructorArguments[0].Value.ToString()
                let types = asm.MainModule.Types.Where(t => t.Interfaces.Any(i => i.FullName == typeof (IHostedApplication).FullName)).Select(t => t.FullName + ", " + asm.FullName)
                where types.Any()
                select Tuple.Create(new ApplicationInfo {Debug=true,ApplicationId = applicationId, Vendor = vendor, Version = asm.Name.Version},file.path)).ToArray();

          

            if (apps.Length> 1)
                throw new InvalidOperationException(string.Format("Folder {0} contains several applications: {1}{2}", folder, Environment.NewLine,string.Join(","+Environment.NewLine, apps.Select(a=>a.ToString()))));


            if (apps.Length == 0)
                throw new InvalidOperationException(string.Format("Folder {0}  does not contain application", folder));
            return apps.Single();
        }

        public void Install(string installPath, ApplicationInfo application)
        {
            
         
            var folder = m_Folders.First(f => getApplication(f).Item1 == application);
            m_Logger.WarnFormat("Cleaning up install folder '{0}'", installPath);
            var binFolder = Path.GetFullPath(Path.Combine(installPath, "bin"));
            if (Directory.Exists(binFolder))
            {
                m_Logger.WarnFormat("Deleting {0} folder", binFolder);
                Directory.Delete(binFolder, true);
            }
            Directory.CreateDirectory(binFolder);

            foreach (var file in Directory.GetFiles(folder))
            {
                var extension = Path.GetExtension(file);
                var fileName= Path.GetFileName(file);
                extension=extension==null?"":extension.ToLower();
                if (extension == ".pdb" || extension == ".dll" || extension == ".exe")
                    File.Copy(file, Path.Combine(binFolder, fileName), true);
                else
                    File.Copy(file, Path.Combine(installPath, fileName), true);
            }
        }

        public void Upgrade(string path, ApplicationInfo application)
        {
            throw new System.NotImplementedException();
        }
    }
}