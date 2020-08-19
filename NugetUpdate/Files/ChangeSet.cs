using System.Collections.Generic;

namespace NugetPackageUpdates
{
    public class ChangeSet
    {
        public string Message { get; set; }
        public string BranchName { get; set; }
        public ICollection<Change> Changes { get; set; } = new List<Change>();
    }
}