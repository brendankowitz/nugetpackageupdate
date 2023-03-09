using System.Collections.Generic;
using System.Linq;

namespace NugetPackageUpdates.FileGroups
{
    public static class ProfileFileExtensions
    {
        public static string GetGroupNameFor(this IEnumerable<IFileGrouping> groups, ProjectFile projectFile)
        {
            return groups
                .FirstOrDefault(x => x.GetGroupName(projectFile.FilePath) != null)
                ?.GetGroupName(projectFile.FilePath);
        }
    }
}