using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using NuGet;
using NuGet.Resources;

namespace Inceptum.AppServer.NuGetAppInstaller
{

    static class ProjectExtensions
    {
        internal static IEnumerable<T> GetCompatibleItemsCore<T>(this IProjectSystem projectSystem, IEnumerable<T> items) where T : IFrameworkTargetable
        {
            IEnumerable<T> compatibleItems;
            if (VersionUtility.TryGetCompatibleItems<T>(projectSystem.TargetFramework, items, out compatibleItems))
                return compatibleItems;
            else
                return Enumerable.Empty<T>();
        }


        internal static bool IsPortableFramework(this FrameworkName framework)
        {
            if (framework != (FrameworkName)null)
                return ".NETPortable".Equals(framework.Identifier, StringComparison.OrdinalIgnoreCase);
            else
                return false;
        }

        public static string GetTargetFrameworkLogString(FrameworkName targetFramework)
        {
            return (targetFramework == null || targetFramework == VersionUtility.EmptyFramework) ? "(not framework-specific)" : String.Empty;
        }

    }
    class MyProjectManager : ProjectManager
    {
        public MyProjectManager(IPackageRepository sourceRepository, IPackagePathResolver pathResolver, IProjectSystem project, IPackageRepository localRepository) : base(sourceRepository, pathResolver, project, localRepository)
        {
        }
/*
        private void execute(IPackage package, IPackageOperationResolver resolver)
        {
            IEnumerable<PackageOperation> source = resolver.ResolveOperations(package);
            if (Enumerable.Any<PackageOperation>(source))
            {
                foreach (PackageOperation operation in source)
                    this.Execute(operation);
            }
            else
            {
                if (!PackageRepositoryExtensions.Exists(this.LocalRepository, (IPackageMetadata)package))
                    return;
                this.Logger.Log(MessageLevel.Info, NuGetResources.Log_ProjectAlreadyReferencesPackage, (object)this.Project.ProjectName, (object)PackageExtensions.GetFullName((IPackageMetadata)package));
            }
        }


        public override void AddPackageReference(IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions)
        {
            InstallWalker installWalker = new InstallWalker(this.LocalRepository, this.SourceRepository, Project.TargetFramework, this.Logger, ignoreDependencies, allowPrereleaseVersions);
            this.execute(package, (IPackageOperationResolver)installWalker);
            base.AddPackageReference(package, ignoreDependencies, allowPrereleaseVersions);
            /*
                        var updateWalker = new UpdateWalker(this.LocalRepository, this.SourceRepository, (IDependentsResolver)new DependentsWalker(this.LocalRepository,Project.TargetFramework), this.ConstraintProvider, this.Project.TargetFramework, NullLogger.Instance, !ignoreDependencies, allowPrereleaseVersions)
                        {
                            AcceptedTargets = PackageTargets.All
                        };
                        this.execute(package, (IPackageOperationResolver)updateWalker);
            #1#


            
        }
*/

        private void filterAssemblyReferences(List<IPackageAssemblyReference> assemblyReferences, ICollection<PackageReferenceSet> packageAssemblyReferences)
        {
            if (packageAssemblyReferences != null && packageAssemblyReferences.Count > 0)
            {
                var packageReferences = Project.GetCompatibleItemsCore(packageAssemblyReferences).FirstOrDefault();
                if (packageReferences != null)
                {
                    // remove all assemblies of which names do not appear in the References list
                    assemblyReferences.RemoveAll(assembly => !packageReferences.References.Contains(assembly.Name, StringComparer.OrdinalIgnoreCase));
                }
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        protected override void ExtractPackageFilesToProject(IPackage package)
        {
            // BUG 491: Installing a package with incompatible binaries still does a partial install.
            // Resolve assembly references and content files first so that if this fails we never do anything to the project
            List<IPackageAssemblyReference> assemblyReferences = Project.GetCompatibleItemsCore(package.AssemblyReferences).ToList();
            List<FrameworkAssemblyReference> frameworkReferences = Project.GetCompatibleItemsCore(package.FrameworkAssemblies).ToList();
            List<IPackageFile> contentFiles = Project.GetCompatibleItemsCore(package.GetContentFiles()).ToList();
            List<IPackageFile> buildFiles = Project.GetCompatibleItemsCore(package.GetBuildFiles()).ToList();

            // If the package doesn't have any compatible assembly references or content files,
            // throw, unless it's a meta package.
            if (assemblyReferences.Count == 0 && frameworkReferences.Count == 0 && contentFiles.Count == 0 && buildFiles.Count == 0 &&
                (package.FrameworkAssemblies.Any() || package.AssemblyReferences.Any() || package.GetContentFiles().Any() || package.GetBuildFiles().Any()))
            {
                // for portable framework, we want to show the friendly short form (e.g. portable-win8+net45+wp8) instead of ".NETPortable, Profile=Profile104".
                FrameworkName targetFramework = Project.TargetFramework;
                string targetFrameworkString = targetFramework.IsPortableFramework()
                                                    ? VersionUtility.GetShortFrameworkName(targetFramework)
                                                    : targetFramework != null ? targetFramework.ToString() : null;

                throw new InvalidOperationException(
                           String.Format(CultureInfo.CurrentCulture,
                           NuGetResources.UnableToFindCompatibleItems, package.GetFullName(), targetFrameworkString));
            }

            // IMPORTANT: this filtering has to be done AFTER the 'if' statement above,
            // so that we don't throw the exception in case the <References> filters out all assemblies.
            filterAssemblyReferences(assemblyReferences, package.PackageAssemblyReferences);

            try
            {
                if (assemblyReferences.Count > 0 || contentFiles.Count > 0 )
                {
                 /*   Logger.Log(MessageLevel.Debug, "For adding package '{0}' to project '{1}' that targets '{2}',", package.GetFullName(), Project.ProjectName, VersionUtility.GetShortFrameworkName(Project.TargetFramework));

                    if (assemblyReferences.Count > 0)
                    {
                        Logger.Log(MessageLevel.Debug, NuGetResources.Debug_TargetFrameworkInfo, NuGetResources.Debug_TargetFrameworkInfo_AssemblyReferences,
                            Path.GetDirectoryName(assemblyReferences[0].Path), VersionUtility.GetTargetFrameworkLogString(assemblyReferences[0].TargetFramework));
                    }

                    if (contentFiles.Count > 0)
                    {
                        Logger.Log(MessageLevel.Debug, NuGetResources.Debug_TargetFrameworkInfo, NuGetResources.Debug_TargetFrameworkInfo_ContentFiles,
                            Path.GetDirectoryName(contentFiles[0].Path), VersionUtility.GetTargetFrameworkLogString(contentFiles[0].TargetFramework));
                    }

                    if (buildFiles.Count > 0)
                    {
                        Logger.Log(MessageLevel.Debug, NuGetResources.Debug_TargetFrameworkInfo, NuGetResources.Debug_TargetFrameworkInfo_BuildFiles,
                            Path.GetDirectoryName(buildFiles[0].Path), VersionUtility.GetTargetFrameworkLogString(buildFiles[0].TargetFramework));
                    }*/
                }

                // Add content files
           //     Project.AddFiles(contentFiles, _fileTransformers);

                // Add the references to the reference path
                foreach (IPackageAssemblyReference assemblyReference in assemblyReferences)
                {
                    if (assemblyReference.IsEmptyFolder())
                    {
                        continue;
                    }

                    // Get the physical path of the assembly reference
                    string referencePath = Path.Combine(PathResolver.GetInstallPath(package), assemblyReference.Path);
                    string relativeReferencePath = PathUtility.GetRelativePath(Project.Root, referencePath);

                    if (Project.ReferenceExists(assemblyReference.Name))
                    {
                        Project.RemoveReference(assemblyReference.Name);
                    }

                    // The current implementation of all ProjectSystem does not use the Stream parameter at all.
                    // We can't change the API now, so just pass in a null stream.
                    Project.AddReference(relativeReferencePath,assemblyReference.GetStream());
                }

                // Add GAC/Framework references
                foreach (FrameworkAssemblyReference frameworkReference in frameworkReferences)
                {
                    if (!Project.ReferenceExists(frameworkReference.AssemblyName))
                    {
                        Project.AddFrameworkReference(frameworkReference.AssemblyName);
                    }
                }

                foreach (var importFile in buildFiles)
                {
                    string fullImportFilePath = Path.Combine(PathResolver.GetInstallPath(package), importFile.Path);
                    Project.AddImport(
                        fullImportFilePath,
                        importFile.Path.EndsWith(".props", StringComparison.OrdinalIgnoreCase) ? ProjectImportLocation.Top : ProjectImportLocation.Bottom);
                }
            }
            finally
            {
/*
                if (_packageReferenceRepository != null)
                {
                    // save the used project's framework if the repository supports it.
                    _packageReferenceRepository.AddPackage(package.Id, package.Version, package.DevelopmentDependency, Project.TargetFramework);
                }
                else
*/
                {
                    // Add package to local repository in the finally so that the user can uninstall it
                    // if any exception occurs. This is easier than rolling back since the user can just
                    // manually uninstall things that may have failed.
                    // If this fails then the user is out of luck.
                    LocalRepository.AddPackage(package);
                }
            }
        }

/*
        protected override void ExtractPackageFilesToProject(IPackage package)
        {
            base.ExtractPackageFilesToProject(package); 
            IEnumerable<IPackageFile> configItems;
            Project.TryGetCompatibleItems(package.GetFiles("config"), out configItems);
            var configFiles = configItems.ToArray();
            if (!configFiles.Any() && package.GetFiles("config").Any())
            {
                // for portable framework, we want to show the friendly short form (e.g. portable-win8+net45+wp8) instead of ".NETPortable, Profile=Profile104".
                FrameworkName targetFramework = Project.TargetFramework;
                string targetFrameworkString = /*targetFramework.IsPortableFramework()#1#targetFramework != null && ".NETPortable".Equals(targetFramework.Identifier, StringComparison.OrdinalIgnoreCase)
                    ? VersionUtility.GetShortFrameworkName(targetFramework)
                    : targetFramework != null ? targetFramework.ToString() : null;

                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture,
                        NuGetResources.UnableToFindCompatibleItems, package.GetFullName(), targetFrameworkString));
            }

            if (configFiles.Any())
            {
                Logger.Log(MessageLevel.Debug, ">> {0} are being added from '{1}'{2}", "config files",
                    Path.GetDirectoryName(configFiles[0].Path), GetTargetFrameworkLogString(configFiles[0].TargetFramework));
            }
            // Add config files
            Project.AddFiles(configFiles, new Dictionary<FileTransformExtensions, IPackageFileTransformer>());
        }
*/
        public static string GetTargetFrameworkLogString(FrameworkName targetFramework)
        {
            return (targetFramework == null || targetFramework == VersionUtility.EmptyFramework) ? "(not framework-specific)" : String.Empty;
        }



    }
}