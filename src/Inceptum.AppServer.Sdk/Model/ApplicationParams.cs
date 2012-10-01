using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Inceptum.AppServer.Model
{
    public class ApplicationParams
    {
        public string AppType { get; set; }
        public string ConfigFile { get; set; }
        public string[] NativeDllToLoad { get; set; }
        public Dictionary<AssemblyName, string> AssembliesToLoad { get; private set; }

        public ApplicationParams(string appType, string configFile, string[] nativeDllToLoad, Dictionary<AssemblyName, string> assembliesToLoad)
        {
            AppType = appType;
            ConfigFile = configFile;
            NativeDllToLoad = nativeDllToLoad;
            AssembliesToLoad = new Dictionary<AssemblyName, string>(assembliesToLoad);
        }

        public bool Equals(ApplicationParams other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.AppType, AppType) 
                && Equals(other.ConfigFile, ConfigFile) 
                && other.NativeDllToLoad.OrderBy(x => x).SequenceEqual(NativeDllToLoad.OrderBy(x => x))
                && other.AssembliesToLoad.Select(x => x.Key.FullName + "|" + x.Value).OrderBy(x => x).SequenceEqual(AssembliesToLoad.Select(x => x.Key.FullName + "|" + x.Value).OrderBy(x => x));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (ApplicationParams)) return false;
            return Equals((ApplicationParams) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = (AppType != null ? AppType.GetHashCode() : 0);
                result = (result*397) ^ (ConfigFile != null ? ConfigFile.GetHashCode() : 0);
                result = (result*397) ^ (NativeDllToLoad != null ? NativeDllToLoad.GetHashCode() : 0);
                result = (result*397) ^ (AssembliesToLoad != null ? AssembliesToLoad.GetHashCode() : 0);
                return result;
            }
        }

        public ApplicationParams Clone()
        {
            return new ApplicationParams(AppType, ConfigFile, NativeDllToLoad.ToArray(), new Dictionary<AssemblyName, string>(AssembliesToLoad));
        }

        public static bool operator ==(ApplicationParams p1,ApplicationParams p2)
        {
            if(ReferenceEquals(p1,null)) 
                return ReferenceEquals(p2,null);

            return p1.Equals(p2);
        }

        public static bool operator !=(ApplicationParams p1, ApplicationParams p2)
        {
            return !(p1 == p2);
        }
    }
}