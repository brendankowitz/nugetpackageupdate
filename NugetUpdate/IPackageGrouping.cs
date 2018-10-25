namespace NugetPackageUpdates
{
    public interface IPackageGrouping
    {
        string GetGroupName(string packageName);
    }
}