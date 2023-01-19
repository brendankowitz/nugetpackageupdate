using System;
using System.Collections.Generic;
using System.Text;

namespace NugetPackageUpdates.WorkItems
{
    public class PullRequestItem
    {

        public int PullRequestId { get; set; }
        public string ArtifactId { get; set; }

        public CreatedBy CreatedBy { get; set; }
    }
}
