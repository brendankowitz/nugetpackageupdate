using System;
using NuGet.Versioning;

namespace NugetPackageUpdates
{
    public class NugetPackage
    {
        public NugetPackage(string id, NuGetVersion version, DateTime released, bool listed)
        {
            Id = id;
            Version = version;
            Released = released;
            Listed = listed;
        }

        public string Id { get; set; }

        public NuGetVersion Version { get; set; }

        public DateTime Released { get; set; }

        public bool Listed { get; set; }
    }
}