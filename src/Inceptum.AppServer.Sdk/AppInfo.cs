using System;

namespace Inceptum.AppServer
{
    [Serializable]
    public class AppInfo
    {
        public AppInfo(string name, Version version)
        {
            Name = name;
            Version = version;
        }

        public string Name { get; private set; }

        public Version Version { get; private set; }

        public static bool operator ==(AppInfo info1, AppInfo info2)
        {
            if (null == (object) info1)
                return null == (object) info2;
            return info1.Equals(info2);
        }

        public static bool operator !=(AppInfo info1, AppInfo info2)
        {
            return !(info1 == info2);
        }

        public bool Equals(AppInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Name, Name) && Equals(other.Version, Version);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (AppInfo)) return false;
            return Equals((AppInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0)*397) ^ (Version != null ? Version.GetHashCode() : 0);
            }
        }

        public override string ToString()
        {
            return string.Format("{0} v{1}", Name, Version);
        }
    }
}