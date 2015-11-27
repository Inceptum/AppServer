using System;
using System.Runtime.Versioning;

namespace Inceptum.AppServer.Runtime
{
    internal static class FrameworkNameExtensions
    {
        internal static bool IsPortableFramework(this FrameworkName framework)
        {
            if (framework != null)
            {
                return ".NETPortable".Equals(framework.Identifier, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}