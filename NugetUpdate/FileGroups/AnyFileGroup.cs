namespace NugetPackageUpdates.FileGroups
{
    public class AnyFileGroup : IFileGrouping
    {
        public string GetGroupName(string path)
        {
            return string.Empty;
        }
    }
}