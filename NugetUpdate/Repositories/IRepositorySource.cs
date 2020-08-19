using System.Collections.Generic;
using System.Threading.Tasks;

namespace NugetPackageUpdates
{
    public interface IRepositorySource
    {
        Task SubmitPR(ChangeSet changeSet, string[] reviewers);

        Task<ProjectFile> GetProjectFile(string projectPath);

        Task<TextFile> GetTextFile(string path);

        Task<ICollection<string>> FindProjectFiles();
    }
}