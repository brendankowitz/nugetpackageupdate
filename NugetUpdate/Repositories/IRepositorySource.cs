using System.Collections.Generic;
using System.Threading.Tasks;

namespace NugetPackageUpdates
{
    public interface IRepositorySource
    {
        Task SubmitPR(ChangeSet changeSet, string[] reviewers, bool associateWithWorkItem = false);

        Task<ProjectFile> GetProjectFile(string projectPath);

        Task<TextFile> GetTextFile(string path);

        Task<ICollection<string>> FindProjectFiles(string componentDirectory = null);
    }
}