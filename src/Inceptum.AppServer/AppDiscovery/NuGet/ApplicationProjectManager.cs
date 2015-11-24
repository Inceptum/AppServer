using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using NuGet;
using NuGet.Resources;

namespace Inceptum.AppServer.AppDiscovery.NuGet
{
    internal class ApplicationProjectManager : ProjectManager
    {
        private readonly string m_PackageId;
        private readonly IPackageRepository m_SharedRepository;

        public ApplicationProjectManager(string packageId, IPackageRepository sourceRepository, IPackagePathResolver pathResolver, IProjectSystem project, IPackageRepository localRepository,
            IPackageRepository sharedRepository)
            : base(sourceRepository, pathResolver, project, localRepository)
        {
            m_PackageId = packageId;
            m_SharedRepository = sharedRepository;
        }

        private void execute(IPackage package, IPackageOperationResolver resolver)
        {
            var source = resolver.ResolveOperations(package);
            if (source.Any())
            {
                foreach (var operation in source)
                {
                    Execute(operation);
                }
            }
            else
            {
                if (!LocalRepository.Exists(package))
                {
                    return;
                }
                Logger.Log(MessageLevel.Info, NuGetResources.Log_ProjectAlreadyReferencesPackage, Project.ProjectName, package.GetFullName());
            }
        }

        public override void AddPackageReference(IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions)
        {
            //TODO[KN]: crappy nuget does not install some packages. Second run helps
            for (var i = 0; i < 1; i++)
            {
                var walker = new UpdateWalker(LocalRepository, SourceRepository,
                    new DependentsWalker(SourceRepository, Project.TargetFramework), ConstraintProvider,
                    Project.TargetFramework, Logger, !ignoreDependencies, allowPrereleaseVersions)
                {
                    AcceptedTargets = PackageTargets.All,
                    DependencyVersion = DependencyVersion.Highest
                };
                execute(package, walker);
            }
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        protected override void ExtractPackageFilesToProject(IPackage package)
        {
            // Resolve assembly references and content files first so that if this fails we never do anything to the project
            /*      List<IPackageFile> assemblyReferences = Project.GetCompatibleItemsCore(package.AssemblyReferences).Cast<IPackageFile>().ToList();
            IEnumerable<IPackageFile> satellites;
            VersionUtility.TryGetCompatibleItems(Project.TargetFramework, package.GetLibFiles(), out satellites);
            assemblyReferences.AddRange(satellites.ToArray());
*/
            IEnumerable<IPackageFile> satellites;
            VersionUtility.TryGetCompatibleItems(Project.TargetFramework, package.GetLibFiles(), out satellites);
            var assemblyReferences = new List<IPackageFile>();
            assemblyReferences.AddRange(satellites.ToArray());


            var contentFiles = Project.GetCompatibleItemsCore(package.GetContentFiles()).ToList();
            /*     
            IEnumerable<IPackageFile> satellites;
            VersionUtility.TryGetCompatibleItems(Project.TargetFramework, package.GetLibFiles(), out satellites);
            var packageFiles = satellites.ToArray();
            return refs.Concat(packageFiles);*/

            IEnumerable<IPackageFile> configItems;
            Project.TryGetCompatibleItems(package.GetFiles("config"), out configItems);
            var configFiles = configItems.ToArray();


            // If the package doesn't have any compatible assembly references or content files,
            // throw, unless it's a meta package.
            if (assemblyReferences.Count == 0 && contentFiles.Count == 0 && (package.AssemblyReferences.Any() || package.GetContentFiles().Any()))
            {
                // for portable framework, we want to show the friendly short form (e.g. portable-win8+net45+wp8) instead of ".NETPortable, Profile=Profile104".
                var targetFramework = Project.TargetFramework;
                var targetFrameworkString = targetFramework.IsPortableFramework()
                    ? VersionUtility.GetShortFrameworkName(targetFramework)
                    : targetFramework != null ? targetFramework.ToString() : null;

                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentCulture,
                        NuGetResources.UnableToFindCompatibleItems, package.GetFullName(), targetFrameworkString));
            }

            // IMPORTANT: this filtering has to be done AFTER the 'if' statement above,
            // so that we don't throw the exception in case the <References> filters out all assemblies.
            //[KN]: we do need all files since reference is just presence in app bin folder in this case
            //filterAssemblyReferences(assemblyReferences, package.PackageAssemblyReferences);

            try
            {
                // Add content files
                if (m_PackageId == package.Id)
                    Project.AddFiles(contentFiles, "");

                // Add config files
                var configTransformers = configFiles.Select(f => Path.GetExtension(f.Path))
                    .Distinct()
                    .ToDictionary(e => new FileTransformExtensions(e, e), e => (IPackageFileTransformer) new ConfigTransformer(e));
                Project.AddFiles(configFiles, configTransformers);


                // Add the references to the reference path
                foreach (var assemblyReference in assemblyReferences)
                {
                    if (assemblyReference.IsEmptyFolder())
                    {
                        continue;
                    }

                    // Get the physical path of the assembly reference
                    var referencePath = Path.Combine(PathResolver.GetInstallPath(package), assemblyReference.Path);
                    var relativeReferencePath = PathUtility.GetRelativePath(Project.Root, referencePath);

                    if (Project.ReferenceExists(assemblyReference.EffectivePath))
                    {
                        Project.RemoveReference(assemblyReference.EffectivePath);
                    }

                    // The current implementation of all ProjectSystem does not use the Stream parameter at all.
                    // We can't change the API now, so just pass in a null stream.
                    Project.AddReference(assemblyReference.EffectivePath, assemblyReference.GetStream());
                }
            }
            finally
            {
                m_SharedRepository.AddPackage(package);
                LocalRepository.AddPackage(package);
            }
        }
    }
}