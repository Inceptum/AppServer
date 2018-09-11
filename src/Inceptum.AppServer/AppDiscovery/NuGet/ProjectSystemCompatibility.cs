using System.Collections.Generic;
using System.Linq;
using NuGet;

namespace Inceptum.AppServer.AppDiscovery.NuGet
{
    internal static class ProjectSystemCompatibility
    {
        internal static IEnumerable<T> GetCompatibleItemsCore<T>(this IProjectSystem projectSystem, IEnumerable<T> items) where T : IFrameworkTargetable
        {
            IEnumerable<T> compatibleItems;
            if (VersionUtility.TryGetCompatibleItems(projectSystem.TargetFramework, items, out compatibleItems))
            {
                return compatibleItems;
            }

            return Enumerable.Empty<T>();
        }
    }
}