namespace NugetPackageUpdates
{
    public class UniqueNamePackageGroup : IPackageGrouping
    {
        public string GetGroupName(string packageName, string packageVersion)
        {
            return packageName;
        }
    }
}