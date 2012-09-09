using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenWrap.IO;
using OpenWrap.PackageManagement;
using OpenWrap.PackageManagement.Exporters.Assemblies;
using OpenWrap.PackageModel;
using OpenWrap.Runtime;

namespace Inceptum.AppServer.AppDiscovery.Openwrap
{
    public class HostedApplicationExporter : DefaultAssemblyExporter
    {

        public override IEnumerable<IGrouping<string, TItem>> Items<TItem>(IPackage package, ExecutionEnvironment environment)  
        {
            if (typeof(TItem) != typeof(IHostedApplicationExport)) return Enumerable.Empty<IGrouping<string, TItem>>();

            return from source in GetAssemblies<Exports.IAssembly>(package, environment)
                   from assembly in source
                   from hostedApp in read (package, assembly).Cast<TItem>()
                   group hostedApp by source.Key;
        }

        private IEnumerable  read (IPackage package, Exports.IAssembly assembly)
        {
            return assembly.File.Read(stream => HostedApps(package, assembly.Path, stream));
        }

        private IEnumerable<IHostedApplicationExport> HostedApps(IPackage package, string path, Stream assemblyStream)
        {
            try
            {
                var assembly = CeceilExtencions.TryReadAssembly(assemblyStream);
                var attribute = assembly.CustomAttributes.FirstOrDefault(a=>a.AttributeType.FullName==typeof(HostedApplicationAttribute).FullName);
                if(attribute==null)
                      return Enumerable.Empty<IHostedApplicationExport>();
                var appType = assembly.MainModule.Types.FirstOrDefault(t => t.Interfaces.Any(i => i.FullName == typeof(IHostedApplication).FullName));
                if(appType==null)
                      return Enumerable.Empty<IHostedApplicationExport>();


                var name = attribute.ConstructorArguments.First().Value.ToString();
                var vendor = attribute.ConstructorArguments.Count==2?attribute.ConstructorArguments[1].Value.ToString():HostedApplicationAttribute.DEFAULT_VENDOR;

                return new[] { new HostedApplicationExport(package, name,vendor, package.Version, appType.FullName + ", " + assembly.FullName) };
            }
            catch
            {
                return Enumerable.Empty<IHostedApplicationExport>();
            }
        }
    }
}