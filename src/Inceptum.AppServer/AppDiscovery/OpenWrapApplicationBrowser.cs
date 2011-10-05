using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenFileSystem.IO.FileSystems.Local.Win32;
using OpenWrap.Commands;
using OpenWrap.PackageManagement;
using OpenWrap.PackageModel;
using OpenWrap.Repositories;
using OpenWrap.Repositories.FileSystem;
using OpenWrap.Repositories.Http;
using OpenWrap.Runtime;
using OpenWrap.Services;
using OpenWrap.Collections;


namespace Inceptum.AppServer.AppDiscovery
{
    public class OpenWrapApplicationBrowser : IApplicationBrowser
    {
        private IPackageRepository m_RemoteRepository;
        private IPackageManager m_PackageManager;
        private ExecutionEnvironment m_Environment;
        private ServiceRegistry m_ServiceRegistry;
        private IPackageRepository m_ProjectRepository;

        public OpenWrapApplicationBrowser(string remoteRepository, string localRepository)
        {
            m_ServiceRegistry = new ServiceRegistry();
            m_ServiceRegistry.Initialize();

            m_RemoteRepository = new IndexedFolderRepository("", new Win32Directory(remoteRepository));
            m_PackageManager = ServiceLocator.GetService<IPackageManager>();
            m_Environment = new ExecutionEnvironment
            {
                Platform = (IntPtr.Size == 4) ? "x86" : "x64",
                Profile = (Environment.Version.Major >= 4) ? "net40" : "net35"
            };
            m_ProjectRepository = new FolderRepository(new Win32Directory(localRepository));

        }

        public IEnumerable<AppInfo> GetAvailabelApps()
        {
            var apps = m_RemoteRepository.PackagesByName
                .Select(x => x.OrderByDescending(y => y.Version).First())
                .NotNull()
                .Select(package => new AppInfo(package.Name, package.Version.ToString()));


            return apps.ToArray();
        }

        public HostedAppInfo GetAppLoadParams(AppInfo appInfo)
        {
            IPackageDescriptor descriptor = new PackageDescriptor(new[]
                                                                      {
                                                                          new GenericDescriptorEntry("name", "test"),
                                                                          new GenericDescriptorEntry("version","0.0.0.1"),
                                                                          new GenericDescriptorEntry("depends",appInfo.Name)
                                                                      });
         

            //TODO: take version into account
            IPackageAddResult addProjectPackage = m_PackageManager.AddSystemPackage(PackageRequest.Any(appInfo.Name), new[] { m_RemoteRepository }, m_ProjectRepository);
            //TODO: errors processing 
            //iterrating is nessesary for add operation to complete
            foreach (var res in addProjectPackage)
            {
                /*if (!res.Success && res.ToOutput().Type==CommandResultType.Error)
                    return null;*/
            }

            IEnumerable<IGrouping<string, Exports.IAssembly>> projectExports =m_PackageManager.GetProjectExports<Exports.IAssembly>(descriptor, m_ProjectRepository, m_Environment);
            var path = Path.GetFullPath(Path.Combine("apps",appInfo.Name));
            if(!Directory.Exists(path))
                Directory.CreateDirectory(path);

            
            var assembliesToLoad = projectExports.Select(e => e.First().File.Path.FullPath).ToArray();

            var appType=(from file in assembliesToLoad
                            let assembly = CeceilExtencions.TryReadAssembly(file)
                            let type = assembly.MainModule.Types.FirstOrDefault(t => t.Interfaces.Any(i => i.FullName == typeof (IHostedApplication).FullName)) 
                            where type != null
                            select type.FullName + ", " + assembly.FullName).FirstOrDefault();

            if (appType == null)
                return null;
            return new HostedAppInfo(appInfo.Name, appInfo.Version, path, assembliesToLoad)
                       {
                           AppType = appType
                       };
        }
    }
}