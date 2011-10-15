using System.Collections.Generic;
using System.Linq;
using OpenFileSystem.IO;
using OpenWrap;
using OpenWrap.PackageManagement;
using OpenWrap.PackageModel;
using OpenWrap.Runtime;

namespace Inceptum.AppServer.AppDiscovery.Openwrap
{
    public class NativeDllExporter : IExportProvider
    {
        #region IExportProvider Members

        public IEnumerable<IGrouping<string, TItem>> Items<TItem>(IPackage package, ExecutionEnvironment environment) where TItem : IExportItem
        {
            if (typeof (TItem) != typeof (INativeDll)) return Enumerable.Empty<IGrouping<string, TItem>>();
            IEnumerable<NativeDll> dlls = from directory in package.Content
                                          where directory.Key.EqualsNoCase("unmanaged")
                                          from file in directory
                                          where IsNativeCode(file)
                                          select new NativeDll(file.Path, file.Package, file.File);
            return dlls.Cast<TItem>().GroupBy(dll => dll.Path);
        }

        #endregion

        private bool IsNativeCode(Exports.IFile file)
        {
            return file.File.Extension.EqualsNoCase(".dll");
        }
    }

    public interface INativeDll : Exports.IFile
    {
    }

    internal class NativeDll : INativeDll
    {
        public NativeDll(string path, IPackage package, IFile file)
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