using System.IO;
using Mono.Cecil;

namespace Inceptum.AppServer.Hosting
{
    public static class AssemblyDefinitionFactory
    {
        public static AssemblyDefinition ReadAssemblySafe(string file)
        {
            try
            {
                return AssemblyDefinition.ReadAssembly(file);
            }
            catch
            {
                return null;
            }
        }

        public static AssemblyDefinition TryReadAssembly(Stream assemblyStream)
        {
            try
            {
                return AssemblyDefinition.ReadAssembly(assemblyStream);
            }
            catch
            {
                return null;
            }
        }
    }
}