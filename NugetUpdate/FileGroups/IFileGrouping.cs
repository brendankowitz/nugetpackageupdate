namespace NugetPackageUpdates.FileGroups
{
    public interface IFileGrouping
    {
        string GetGroupName(string path);
    }
}