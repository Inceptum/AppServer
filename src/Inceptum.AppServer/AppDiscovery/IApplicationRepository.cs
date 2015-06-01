﻿using System;
using System.Collections.Generic;
using NuGet;

namespace Inceptum.AppServer.AppDiscovery
{
    public class ApplicationInfo
    {
        public bool Debug { get; set; } 
        public string Vendor { get; set; } 
        public string Description { get; set; } 
        public string ApplicationId { get; set; }
        public SemanticVersion Version { get; set; }

        protected bool Equals(ApplicationInfo other)
        {
            return string.Equals(Vendor, other.Vendor) && string.Equals(Description, other.Description) && string.Equals(ApplicationId, other.ApplicationId) && Equals(Version, other.Version);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ApplicationInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Vendor != null ? Vendor.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Description != null ? Description.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (ApplicationId != null ? ApplicationId.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Version != null ? Version.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(ApplicationInfo left, ApplicationInfo right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ApplicationInfo left, ApplicationInfo right)
        {
            return !Equals(left, right);
        }
    }
    public interface IApplicationRepository
    {
        IEnumerable<ApplicationInfo> GetAvailableApps();
        void Install(string path,ApplicationInfo application);
        void Upgrade(string path, ApplicationInfo application);
        string Name { get;  }
    }
}