using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using Inceptum.AppServer.Runtime;
using NuGet;
using NuGet.Resources;

namespace Inceptum.AppServer.AppDiscovery.NuGet
{
    internal class ApplicationProjectManager : ProjectManager
    {
        private readonly string m_PackageId;
        private readonly IPackageRepository m_SharedRepository;

        public ApplicationProjectManager(string packageId, IPackageRepository sourceRepository, IPackagePathResolver pathResolver,
            IProjectSystem project, IPackageRepository localRepository, IPackageRepository sharedRepository)
            : base(sourceRepository, pathResolver, project, localRepository)
        {
            m_PackageId = packageId;
            m_SharedRepository = sharedRepository;
        }

        public override void AddPackageReference(IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions)
        {

            

            //NOTE: looks like there is a bug in nuget - when package is updated in scope of install, dependencies matching previous version are not installed. Double pass helps
            //sample
            //A->B->C[1,2]->D1
            //A->C1->D1
            //A->D1
            //installing A, first pass initially sets up C2 and D1 then unisnstalls both and installs C1, but D1 is not installe dfor some reason
            for (int i = 0; i < 2; i++)
            {
                // In case of a scenario like UpdateAll, the graph has already been walked once for all the packages as a bulk operation
                // But, we walk here again, just for a single package, since, we need to use UpdateWalker for project installs
                // unlike simple package installs for which InstallWalker is used
                // Also, it is noteworthy that the DependentsWalker has the same TargetFramework as the package in PackageReferenceRepository
                // unlike the UpdateWalker whose TargetFramework is the same as that of the Project
                // This makes it harder to perform a bulk operation for AddPackageReference and we have to go package by package
                var dependentsWalker = new DependentsWalker(LocalRepository, getPackageTargetFramework(package.Id))
                {
                    DependencyVersion = DependencyVersion
                };
                
                execute(package, new UpdateWalker(LocalRepository,
                    SourceRepository,
                    dependentsWalker,
                    ConstraintProvider,
                    Project.TargetFramework,
                    Logger,
                    !ignoreDependencies,
                    allowPrereleaseVersions)
                {
                    AcceptedTargets = PackageTargets.Project,
                    DependencyVersion = DependencyVersion
                });
            }
        }

        private FrameworkName getPackageTargetFramework(string packageId)
        {
            var packageReferenceRepository = LocalRepository as IPackageReferenceRepository;
            if (packageReferenceRepository != null)
            {
                return packageReferenceRepository.GetPackageTargetFramework(packageId) ?? Project.TargetFramework;
            }

            return Project.TargetFramework;
        }
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        protected override void ExtractPackageFilesToProject(IPackage package)
        {
            IEnumerable<IPackageFile> satellites;
            VersionUtility.TryGetCompatibleItems(Project.TargetFramework, package.GetLibFiles(), out satellites);
            var assemblyReferences = new List<IPackageFile>();
            assemblyReferences.AddRange(satellites.ToArray());


            var contentFiles = Project.GetCompatibleItemsCore(package.GetContentFiles()).ToList();

            IEnumerable<IPackageFile> configItems;
            Project.TryGetCompatibleItems(package.GetFiles("config"), out configItems);
            var configFiles = configItems.ToArray();


            // If the package doesn't have any compatible assembly references or content files,
            // throw, unless it's a meta package.
            if (assemblyReferences.Count == 0 && contentFiles.Count == 0 && (package.AssemblyReferences.Any() || package.GetContentFiles().Any()))
            {
                // for portable framework, we want to show the friendly short form (e.g. portable-win8+net45+wp8) instead of ".NETPortable, Profile=Profile104".
                var targetFramework = Project.TargetFramework;
                var targetFrameworkString = FrameworkNameExtensions.IsPortableFramework(targetFramework)
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
                //Nuget bug again - some times packages.config is not accessible 
                m_SharedRepository.AddPackage(package);
                bool success=false;
                while (!success)
                {
                    try
                    {
                        LocalRepository.AddPackage(package);
                        success = true;
                    }
                    catch (IOException e)
                    {
                        //Logger.Log(MessageLevel.Warning, e.ToString());
                        Thread.Sleep(500);
                    }
                }
            }
        }

        private void execute(IPackage package, IPackageOperationResolver resolver)
        {
            IEnumerable<PackageOperation> operations = resolver.ResolveOperations(package);
            if (operations.Any())
            {
                foreach (PackageOperation operation in operations)
                {
                    Execute(operation);
                }
            }
            else if (LocalRepository.Exists(package))
            {
                Logger.Log(MessageLevel.Info, NuGetResources.Log_ProjectAlreadyReferencesPackage, Project.ProjectName, package.GetFullName());
            }
        }
    }
}