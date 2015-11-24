using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using NuGet;

namespace Inceptum.AppServer.AppDiscovery.NuGet
{
    internal static class NugetExtensions
    {
        internal static IEnumerable<T> GetCompatibleItemsCore<T>(this IProjectSystem projectSystem, IEnumerable<T> items) where T : IFrameworkTargetable
        {
            IEnumerable<T> compatibleItems;
            if (VersionUtility.TryGetCompatibleItems(projectSystem.TargetFramework, items, out compatibleItems))
                return compatibleItems;

            return Enumerable.Empty<T>();
        }


        internal static bool IsPortableFramework(this FrameworkName framework)
        {
            if (framework != null)
                return ".NETPortable".Equals(framework.Identifier, StringComparison.OrdinalIgnoreCase);

            return false;
        }
    }
}