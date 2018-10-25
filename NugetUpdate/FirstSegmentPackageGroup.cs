using System.Linq;

namespace NugetPackageUpdates
{
    public class FirstSegmentPackageGroup : IPackageGrouping
    {
        private readonly string[] _exclude;

        public FirstSegmentPackageGroup(params string[] exclude)
        {
            _exclude = exclude;
        }

        public string GetGroupName(string packageName)
        {
            var first = packageName.Split('.').First();

            if (_exclude.All(x => x != first))
            {
                return first;
            }

            return null;
        }
    }
}