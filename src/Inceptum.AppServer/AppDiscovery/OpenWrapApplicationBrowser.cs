using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.Core.Logging;
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
        private IPackageRepository m_DebugRepo;
        private ILogger m_Logger;

        public OpenWrapApplicationBrowser(string remoteRepository, string localRepository, string debugRepo, ILogger logger=null)
        {
            m_Logger = logger ?? NullLogger.Instance;
            m_ServiceRegistry = new ServiceRegistry();
            m_ServiceRegistry.Initialize();

            if (debugRepo!=null && Directory.Exists(debugRepo))
                m_DebugRepo = new FolderRepository(new Win32Directory(debugRepo));
            m_RemoteRepository = GetRepository(remoteRepository);

            m_PackageManager = ServiceLocator.GetService<IPackageManager>();
            m_Environment = new ExecutionEnvironment
            {
                Platform = (IntPtr.Size == 4) ? "x86" : "x64",
                Profile = (Environment.Version.Major >= 4) ? "net40" : "net35"
            };
            var localPath = Path.GetFullPath(localRepository);
            if (!Directory.Exists(localPath))
                Directory.CreateDirectory(localPath);
            m_ProjectRepository = new FolderRepository(new Win32Directory(localPath));

        }

        private static IPackageRepository GetRepository(string path)
        {
            return Directory.GetFiles(path).Any(f=>Path.GetFileName(f).ToLower()=="index.wraplist")
                       ?(IPackageRepository)new IndexedFolderRepository("", new Win32Directory(Path.GetFullPath(path)))
                       :new FolderRepository(new Win32Directory(Path.GetFullPath(path)));
        }

        public IEnumerable<AppInfo> GetAvailabelApps()
        {
            var debugApps=m_DebugRepo.PackagesByName
                .Select(x => x.OrderByDescending(y => y.Version).First())
                .NotNull()
                .Select(package => new AppInfo(package.Name, package.Version.ToString()));

            var apps = m_RemoteRepository.PackagesByName
                .Select(x => x.OrderByDescending(y => y.Version).First())
                .NotNull()
                .Where(p => !m_DebugRepo.PackagesByName.Contains(p.Name))
                .Select(package => new AppInfo(package.Name, package.Version.ToString()));


            return apps.Merge(debugApps).ToArray();
        }

        public HostedAppInfo GetAppLoadParams(AppInfo appInfo)
        {
            IPackageDescriptor descriptor = new PackageDescriptor(new[]
                                                                      {
                                                                          new GenericDescriptorEntry("name", "test"),
                                                                          new GenericDescriptorEntry("version","0.0.0.1"),
                                                                          new GenericDescriptorEntry("depends",appInfo.Name+" = "+appInfo.Version)
                                                                      });

            var removeRes = m_PackageManager.RemoveSystemPackage(PackageRequest.Any(appInfo.Name), m_ProjectRepository).ToArray();
            var removeFailed =removeRes.FirstOrDefault(r => !r.Success && r.ToOutput().Type == CommandResultType.Error);
            
            if (removeFailed != null)
            {
                m_Logger.WarnFormat("Failed to load package {0}: {1}", appInfo, removeFailed.ToOutput().ToString());
                return null;
            }
            
            var addRes = m_PackageManager.AddSystemPackage(PackageRequest.Exact(appInfo.Name,Version.Parse(appInfo.Version)), new[] { m_DebugRepo, m_RemoteRepository }, m_ProjectRepository);
            var addFailed =addRes.FirstOrDefault (r => !r.Success && r.ToOutput().Type == CommandResultType.Error);
            if (addFailed != null)
            {
                m_Logger.WarnFormat("Failed to load package {0}: {1}", appInfo, addFailed.ToOutput().ToString());
                return null;
            }

            //addProjectPackage = m_PackageManager.AddSystemPackage(PackageRequest.Any(appInfo.Name), new[] { m_RemoteRepository }, m_ProjectRepository);
            //var addProjectPackage = m_PackageManager.UpdateSystemPackages(new[] { m_RemoteRepository }, m_ProjectRepository,appInfo.Name);
            //TODO: errors processing 
            //iterrating is nessesary for add operation to complete
/*            foreach (var res in addProjectPackage)
            {
                /*if (!res.Success && res.ToOutput().Type==CommandResultType.Error)
                    return null;#1#
            }*/

            m_PackageManager.UpdateSystemPackages(new[] {m_RemoteRepository}, m_ProjectRepository).Count();

            IEnumerable<IGrouping<string, Exports.IAssembly>> projectExports =m_PackageManager.GetProjectExports<Exports.IAssembly>(descriptor, m_ProjectRepository, m_Environment);
            var path = Path.GetFullPath(Path.Combine("apps",appInfo.Name));
            if(!Directory.Exists(path))
                Directory.CreateDirectory(path);
            //projectExports.Skip( 21 ).First().ToArray()[0]
            var assembliesToLoad = projectExports.SelectMany(e => e.ToArray().Select(f => f.File.Path.FullPath)).ToArray();

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