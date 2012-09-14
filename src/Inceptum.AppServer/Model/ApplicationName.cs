namespace Inceptum.AppServer.Model
{
    public struct ApplicationName
    {
        public ApplicationName(string name, string vendor) : this()
        {
            Name = name;
            Vendor = vendor;
        }

        public string Name { get; private set; }
        public string Vendor { get; private set; }

        public bool Equals(ApplicationName other)
        {
            return Equals(other.Name, Name) && Equals(other.Vendor, Vendor);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof (ApplicationName)) return false;
            return Equals((ApplicationName) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0)*397) ^ (Vendor != null ? Vendor.GetHashCode() : 0);
            }
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", Vendor, Name);
        }

        public static bool operator ==(ApplicationName n1, ApplicationName n2)
        {
            return n1.Equals(n2);
        }

        public static bool operator !=(ApplicationName n1, ApplicationName n2)
        {
            return !(n1 == n2);
        }
    }
}