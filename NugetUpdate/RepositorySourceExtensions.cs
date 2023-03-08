using System.Collections.Generic;
using System.Threading.Tasks;

namespace NugetPackageUpdates
{
    public static class RepositorySourceExtensions
    {
        public static async Task<ICollection<ProjectFile>> GetProjectFiles(this IRepositorySource repository, string componentDirectory = null)
        {
            var files = new List<ProjectFile>();

            foreach (var path in await repository.FindProjectFiles(componentDirectory))
            {
                files.Add(await repository.GetProjectFile(path));
            }

            return files;
        }
    }
}