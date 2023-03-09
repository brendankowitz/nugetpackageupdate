using System;

namespace NugetPackageUpdates.FileGroups
{
    public class PathContainsGroup : IFileGrouping
    {
        private readonly string _pathContains;
        private readonly string _name;

        public PathContainsGroup(string groupName, string pathContains)
        {
            _pathContains = pathContains?.Replace('\\', '/') ?? throw new ArgumentNullException(nameof(pathContains));
            _name = groupName ?? throw new ArgumentNullException(nameof(groupName));;
        }

        public string GetGroupName(string path)
        {
            if (ShouldInclude(path))
            {
                return _name;
            }

            return null;
        }

        private bool ShouldInclude(string path)
        {
            var normalizedPath = path.Replace('\\', '/');
            return normalizedPath.IndexOf(_pathContains, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}