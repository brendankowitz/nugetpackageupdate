using System;

namespace NugetPackageUpdates
{
    public class UniqueNameAndVersionPackageGroup : IPackageGrouping
    {
        private readonly string _groupName;
        private readonly Predicate<string> _nameFilter;

        public UniqueNameAndVersionPackageGroup(string groupName, Predicate<string> nameFilter)
        {
            _groupName = groupName ?? throw new ArgumentNullException(nameof(groupName));
            _nameFilter = nameFilter ?? throw new ArgumentNullException(nameof(nameFilter));
        }

        public string GetGroupName(string packageName, string packageVersion)
        {
            if (_nameFilter(packageName))
            {
                return $"{_groupName}-{packageVersion}";
            }

            return null;
        }
    }
}