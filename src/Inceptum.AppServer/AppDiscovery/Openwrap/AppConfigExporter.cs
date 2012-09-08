using System.Collections.Generic;
using System.Linq;
using OpenFileSystem.IO;
using OpenWrap;
using OpenWrap.PackageManagement;
using OpenWrap.PackageModel;
using OpenWrap.Runtime;

namespace Inceptum.AppServer.AppDiscovery.Openwrap
{
    public class AppConfigExporter : IExportProvider
    {
        #region IExportProvider Members

        public IEnumerable<IGrouping<string, TItem>> Items<TItem>(IPackage package, ExecutionEnvironment environment) where TItem : IExportItem
        {
            if (typeof(TItem) != typeof(IAppConfig)) return Enumerable.Empty<IGrouping<string, TItem>>();
            IEnumerable<IAppConfig> dlls = from directory in package.Content
                                          where directory.Key.EqualsNoCase("config")
                                          from file in directory
                                          where file.File.Name.EqualsNoCase("app.config")
                                          select new AppConfig(file.Path, file.Package, file.File);
            return dlls.Cast<TItem>().GroupBy(appConfig => appConfig.Path);
        }

        #endregion

     
    }

    public interface IAppConfig : Exports.IFile
    {
    }

    internal class AppConfig : IAppConfig
    {
        public AppConfig(string path, IPackage package, IFile file)
        {
            Path = path;
            Package = package;
            File = file;
        }

        #region INativeDll Members

        public string Path { get; private set; }
        public IPackage Package { get; private set; }

        public IFile File { get; private set; }

        #endregion
    }
}