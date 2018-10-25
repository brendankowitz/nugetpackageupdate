using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetPackageUpdates
{
    public class UpdateManager
    {
        private readonly TextWriter _log;

        public UpdateManager(TextWriter log)
        {
            _log = log;
        }

        public IList<IPackageGrouping> PackageGroupings { get; } = new List<IPackageGrouping>
        {
            new StartsWithPackageGroup("Microsoft.AspNetCore", "Microsoft.AspNetCore", "Microsoft.Extensions", "Microsoft.ApplicationInsights"),
            new StartsWithPackageGroup("Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis", "Microsoft.SourceLink", "StyleCop.Analyzers", "Microsoft.CodeCoverage", "Microsoft.NET.Test.Sdk"),
            new FirstSegmentPackageGroup("System", "Microsoft"),
            new UniqueNamePackageGroup()
        };

        public IList<string> AllowBetaPackages { get; } = new List<string>();

        public async Task CreatePullRequestsAsync(IRepositorySource ops, int? prLimit = null)
        {
            await CreatePullRequestsAsync(ops, new string[0], prLimit);
        }

        public async Task CreatePullRequestsAsync(IRepositorySource ops, string[] reviewers, int? prLimit = null)
        {
            _log.WriteLine("Fetching project files");
            var projectFiles = await ops.GetProjectFiles();

            var allPackages = projectFiles.SelectMany(x => x.ListPackages()).Select(x => x.Key).Distinct().ToArray();

            _log.WriteLine("Fetching latest packages");
            var latestPackageVersions = (await NugetApi.GetPackageVersions(allPackages, _log))
                .ToDictionary(x => x.Key, x => x.Value.LastOrDefault(y => AllowBetaPackages.Contains(x.Key) || !y.Contains("-")));

            var packagesToUpdate =
                latestPackageVersions
                    .Where(x => !string.IsNullOrEmpty(x.Value))
                    .Select(x => $"{x.Key}|{x.Value}")
                    .Except(projectFiles.SelectMany(x => x.ListPackages()).Select(x => $"{x.Key}|{x.Value}").Distinct())
                    .ToArray();

            var packageGroupings =
                packagesToUpdate
                    .Select(x => x.Split('|'))
                    .Select(x => x[0])
                    .GroupBy(x => PackageGroupings
                        .First(y => y.GetGroupName(x) != null)
                                     .GetGroupName(x));

            var changeSets = new List<ChangeSet>();

            foreach (var group in packageGroupings)
            {
                var message =
                    new StringBuilder($"Auto-update for packages related to '{group.Key}'");
                message.AppendLine();
                message.AppendLine();

                var groupChangeSet = new ChangeSet
                {
                    BranchName = $"refs/heads/auto-nuget-update/{group.Key.ToLowerInvariant()}",
                };

                foreach (var package in group)
                {
                    var packageUpdateMessage =
                        $"Updates package '{package}' to version '{latestPackageVersions[package]}'";

                    if (group.Count() == 1)
                    {
                        message = new StringBuilder(packageUpdateMessage);
                    }
                    else
                    {
                        message.AppendLine(packageUpdateMessage);
                    }

                    foreach (var projectFile in projectFiles)
                    {
                        projectFile.UpdatePackageReference(package,
                            latestPackageVersions[package]);
                    }
                }

                foreach (var projectFile in projectFiles)
                {
                    groupChangeSet.Changes.Add(new Change
                    {
                        FilePath = projectFile.FilePath,
                        FileContents = projectFile.ToString()
                    });

                    projectFile.Reset();
                }

                groupChangeSet.Message = message.ToString();
                changeSets.Add(groupChangeSet);
            }

            _log.WriteLine("Opening PRs");

            foreach (var pr in changeSets.Take(prLimit.GetValueOrDefault(int.MaxValue)))
            {
                try
                {
                    await ops.SubmitPR(pr, reviewers);
                }
                catch
                {
                    _log.WriteLine($"Unable to open PR for {pr.BranchName}");
                }
            }
        }
    }
}