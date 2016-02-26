using System;
using System.IO;
using System.Linq;
using NuGet;

namespace FixNuspecVersions
{
    class Program
    {
        static void Main(string[] args)
        {
            string nuspec;
            if (args.Length > 0)
            {
                nuspec = args[0];
                if (!File.Exists(nuspec))
                {
                    Console.Error.WriteLine("{0} file not found.", nuspec);
                    Environment.Exit(1);
                }
            }
            else
            {
                var nuspecs = Directory.GetFiles(".","*.nuspec");
                if (!nuspecs.Any())
                {
                    Console.Error.WriteLine(".nuspec file not found. Please pass it as argument");
                    Environment.Exit(1);
                }
                
                if (nuspecs.Count()>1 )
                {
                    Console.Error.WriteLine("Several .nuspec files found. Please pass nuspec file explicitly as argument");
                    Environment.Exit(1);
                }
                nuspec = nuspecs.First();
            }

            var packagesConfig = Directory.GetFiles(".","packages.config").FirstOrDefault();
            if (packagesConfig == null)
            {
                Console.Error.WriteLine("packages.config file not found");
                Environment.Exit(1);  
            }
         

            var file = new PackageReferenceFile(packagesConfig);

            using (var ms = new MemoryStream(File.ReadAllBytes(nuspec)))
            {
                var packages = file.GetPackageReferences().Select(r => new ManifestDependency() {Id = r.Id, Version = "[" + r.Version.ToString() + "]"});

                var manifest = Manifest.ReadFrom(ms,true);
                foreach (var dependencySet in manifest.Metadata.DependencySets)
                {
                    foreach (var package in packages.ToArray())
                    {
                        var dependency = dependencySet.Dependencies.FirstOrDefault(d=>d.Id==package.Id);
                        if (dependency == null)
                        {
                            dependencySet.Dependencies.Add(package);
                        }
                        else
                        {
                            dependency.Version = package.Version;
                        }
                    }
                }


                var backup = nuspec + ".bak";
                var i = 1;
                while (File.Exists(backup))
                {
                    backup = nuspec + ".bak." + i;
                    i++;
                }
                File.Move(nuspec, backup);

                using (var output = File.OpenWrite(nuspec))
                {
                    manifest.Save(output);
                }
                Console.WriteLine("{0} updated",Path.GetFileName(nuspec));
                Console.WriteLine("Backup is stored as {0} ",backup);
            }
        }
    }
}
