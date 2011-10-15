using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.Core.Logging;
using OpenFileSystem.IO.FileSystems.Local.Win32;
using OpenWrap.PackageManagement;
using OpenWrap.PackageManagement.Exporters;
using OpenWrap.PackageManagement.Exporters.Assemblies;
using OpenWrap.PackageManagement.Exporters.Commands;
using OpenWrap.PackageModel;
using OpenWrap.Repositories;
using OpenWrap.Repositories.FileSystem;
using OpenWrap.Runtime;
using OpenWrap.Services;

namespace Inceptum.AppServer.AppDiscovery.Openwrap
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
        private IPackageExporter m_Exporter;


        public OpenWrapApplicationBrowser(string remoteRepository, string localRepository, string debugRepo, ILogger logger=null)
        {
            m_Logger = logger ?? NullLogger.Instance;
            m_ServiceRegistry = new ServiceRegistry();
            m_ServiceRegistry.Override<IPackageExporter>(() => new DefaultPackageExporter(new List<IExportProvider>
                                                                                              {
                                                                                                  new DefaultAssemblyExporter(),
                                                                                                  new CecilCommandExporter(),
                                                                                                  new SolutionPluginExporter(),
                                                                                                  new NativeDllExporter(),
                                                                                                  new HostedApplicationExporter()
                                                                                              }));
            m_ServiceRegistry.Initialize();
            m_Exporter = ServiceLocator.GetService<IPackageExporter>();

            if (!string.IsNullOrEmpty(debugRepo) && Directory.Exists(debugRepo))
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

/*
        public IEnumerable<AppInfo> GetAvailabelApps()
        {
             var apps = m_RemoteRepository.PackagesByName
                 .Select(x => x.OrderByDescending(y => y.Version).First())
                 .NotNull()
                 .Select(package => new AppInfo(package.Name, package.Version.ToString()));

             if (m_DebugRepo != null)
             {
                 var debugApps = m_DebugRepo.PackagesByName
                                                 .Select(x => x.OrderByDescending(y => y.Version).First())
                                                 .NotNull()
                                                 .Select(package => new AppInfo(package.Name, package.Version.ToString()));
                 return apps.Where(p => !m_DebugRepo.PackagesByName.Contains(p.Name)).Merge(debugApps).ToArray();
             }
             return apps;
        }
*/

        public IEnumerable<HostedAppInfo> GetAvailabelApps()
        {            
            return m_PackageManager.GetSystemExports<IHostedApplicationExport>(m_ProjectRepository, m_Environment).SelectMany(x => x).Select(getAppLoadParams).ToArray();
        }



        private HostedAppInfo getAppLoadParams(IHostedApplicationExport appExport)
        {
/*            var app = m_PackageManager.GetSystemExports<IHostedApplicationInfo>(m_ProjectRepository, m_Environment).SelectMany(x=>x.Where(a=>a.Name==appInfo.Name)).FirstOrDefault();
            if (app == null)
                return null;*/
            var path = Path.GetFullPath(Path.Combine("apps", appExport.Name));
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            IEnumerable<IPackageInfo> packages = new[] { appExport.Package }.Concat(
                ServiceLocator.GetService<IPackageResolver>().TryResolveDependencies(appExport.Package.Descriptor, new[] { m_ProjectRepository }).SuccessfulPackages.Select(_ => _.Packages.First())
                );
            var assembliesTooLoad =packages.SelectMany(p=>m_Exporter.Exports<Exports.IAssembly>(p.Load(),m_Environment)).SelectMany(e => e.Select(f => f.File.Path.FullPath));
            var nativeDllsToLoad = packages.SelectMany(p => m_Exporter.Exports<INativeDll>(p.Load(), m_Environment)).SelectMany(e => e.Select(f => f.File.Path.FullPath));
/*
            var assembliesTooLoad = m_PackageManager.GetProjectExports<Exports.IAssembly>(app.Package.Descriptor, m_ProjectRepository, m_Environment)
                                                    .SelectMany(e => e.Select(f => f.File.Path.FullPath));
            var nativeDllsToLoad = m_PackageManager.GetProjectExports<INativeDll>(app.Package.Descriptor, m_ProjectRepository, m_Environment)
                                                    .SelectMany(e => e.Select(f => f.File.Path.FullPath));*/

            return new HostedAppInfo(appExport.Name,appExport.Version,appExport.Type,path,assembliesTooLoad,nativeDllsToLoad);
      
        }




   /*     public HostedAppInfo GetAppLoadParams(AppInfo appInfo)
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

            var addRes = m_PackageManager.AddSystemPackage(PackageRequest.Exact(appInfo.Name, Version.Parse(appInfo.Version)), m_DebugRepo != null ? new[] { m_DebugRepo, m_RemoteRepository } : new[] { m_RemoteRepository }, m_ProjectRepository);
            var addFailed =addRes.FirstOrDefault (r => !r.Success && r.ToOutput().Type == CommandResultType.Error);
            if (addFailed != null)
            {
                m_Logger.WarnFormat("Failed to load package {0}: {1}", appInfo, addFailed.ToOutput().ToString());
                return null;
            }

            m_PackageManager.UpdateSystemPackages(new[] {m_RemoteRepository}, m_ProjectRepository).Count();

            IEnumerable<IGrouping<string, Exports.IAssembly>> projectExports =m_PackageManager.GetProjectExports<Exports.IAssembly>(descriptor, m_ProjectRepository, m_Environment);
            var path = Path.GetFullPath(Path.Combine("apps",appInfo.Name));
            if(!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var assembliesToLoad = projectExports.SelectMany(e => e.ToArray().Select(f => f.File.Path.FullPath)).ToArray();

            //TODO: need to replace with IExportProvider implementation
            var nativeExports = m_PackageManager.ListProjectPackages(descriptor, m_ProjectRepository).SelectMany(p =>
                                                                                                 {
                                                                                                     var package=p as IPackage;
                                                                                                     return from directory in package.Content
                                                                                                            where directory.Key.EqualsNoCase("unmanaged")
                                                                                                            from file in directory
                                                                                                            //where file.File.Extension
                                                                                                            select file.File.Path.FullPath;
                                                                                                 }).ToArray();

            //var nativeExports=m_PackageManager.GetProjectExports<Exports.IFile>(descriptor, m_ProjectRepository, ExecutionEnvironment.Any).SelectMany(e => e.ToArray().Where(f=>f.Path.ToLower()=="unmanaged").Select(f => f.File.Path.FullPath)).ToArray(); ;
            
            var appType=(from file in assembliesToLoad
                            let assembly = CeceilExtencions.TryReadAssembly(file)
                            let type = assembly.MainModule.Types.FirstOrDefault(t => t.Interfaces.Any(i => i.FullName == typeof (IHostedApplication).FullName)) 
                            where type != null
                            select type.FullName + ", " + assembly.FullName).FirstOrDefault();

            if (appType == null)
                return null;
            return new HostedAppInfo(appInfo.Name, appInfo.Version, path, assembliesToLoad)
                       {
                           NativeDllToLoad = nativeExports,
                           AppType = appType
                       };
        }*/
    }
}