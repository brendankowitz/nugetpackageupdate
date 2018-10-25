using System.Linq;

namespace NugetPackageUpdates
{
    public class StartsWithPackageGroup : IPackageGrouping
    {
        private readonly string _groupName;
        private readonly string[] _packageStartsWith;

        public StartsWithPackageGroup(string groupName, params string[] packageStartsWith)
        {
            _groupName = groupName;
            _packageStartsWith = packageStartsWith;
        }

        public string GetGroupName(string packageName)
        {
            if (_packageStartsWith.Any(x => packageName.StartsWith(x)))
            {
                return _groupName;
            }

            return null;
        }
    }
}