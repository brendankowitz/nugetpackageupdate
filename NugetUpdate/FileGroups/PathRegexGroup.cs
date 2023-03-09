using System.Text.RegularExpressions;

namespace NugetPackageUpdates.FileGroups
{
    public class PathRegexGroup : IFileGrouping
    {
        private readonly Regex _regex;

        public PathRegexGroup(Regex regex)
        {
            _regex = regex;
        }

        public string Name { get; }

        public string GetGroupName(string path)
        {
            var normalizedPath = path.Replace('\\', '/');
            var results = _regex.Match(normalizedPath);

            if(results.Success)
            {
                return results.Result("$1").Trim('/','\\').Replace(' ', '-');
            }

            return null;
        }
    }
}