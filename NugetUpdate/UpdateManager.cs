﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Versioning;

namespace NugetPackageUpdates
{
    public class UpdateManager
    {
        private readonly IEnumerable<NugetApi> _nugetApis;
        private readonly TextWriter _log;

        public UpdateManager(TextWriter log)
        {
            _log = log;
            _nugetApis = new[] { NugetApi.Official(log), };
        }

        public UpdateManager(IEnumerable<NugetApi> nugetApis, TextWriter log)
            : this(log)
        {
            _nugetApis = nugetApis;
        }

        public IList<IPackageGrouping> PackageGroupings { get; } = new List<IPackageGrouping>
        {
            new StartsWithPackageGroup("Microsoft.AspNet", "Microsoft.AspNet", "Microsoft.Extensions", "Microsoft.ApplicationInsights"),
            new StartsWithPackageGroup("Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis", "Microsoft.SourceLink", "StyleCop.Analyzers", "Microsoft.CodeCoverage", "Microsoft.NET.Test.Sdk"),
            new FirstSegmentPackageGroup("System", "Microsoft"),
            new UniqueNamePackageGroup()
        };

        public IList<string> AllowBetaPackages { get; } = new List<string>();

        public int PackagesMustBePublishedForThisManyDays { get; set; } = 21;

        public async Task CreatePullRequestsAsync(IRepositorySource ops, int? prLimit = null)
        {
            await CreatePullRequestsAsync(ops, new string[0], prLimit);
        }

        public async Task CreatePullRequestsAsync(
            IRepositorySource ops,
            string[] reviewers,
            int? prLimit = null,
            bool associatWithWorkItem = false)
        {
            var changeSets = await GetChangeSets(ops);

            _log.WriteLine("Opening PRs");

            foreach (var pr in changeSets.Take(prLimit.GetValueOrDefault(int.MaxValue)))
            {
                try
                {
                    await ops.SubmitPR(pr, reviewers, associatWithWorkItem);
                }
                catch
                {
                    _log.WriteLine($"Unable to open PR for {pr.BranchName}");
                }
            }
        }

        public async Task<IReadOnlyCollection<ChangeSet>> GetChangeSets(IRepositorySource ops)
        {
            _log.WriteLine("Fetching project files");
            var projectFiles = await ops.GetProjectFiles();

            if (projectFiles == null || !projectFiles.Any())
            {
                _log.WriteLine("No project files found");
                return new List<ChangeSet>();
            }

            var allPackages = projectFiles.SelectMany(x => x.ListPackages())?.Select(x => x.Key)?.Distinct()?.ToArray();

            if (allPackages == null || !allPackages.Any())
            {
                _log.WriteLine("No packages found");
                return new List<ChangeSet>();
            }

            _log.WriteLine("Fetching latest packages");

            var nugetPackages = await Task.WhenAll(_nugetApis.Select(x => x.GetPackageVersions(allPackages, AllowBetaPackages)));

            if (nugetPackages == null || !nugetPackages.Any())
            {
                _log.WriteLine("No packages found");
                return new List<ChangeSet>();
            }

            var latestPackageVersions =
                nugetPackages
                .SelectMany(x => x)
                .GroupBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.Value.Version).FirstOrDefault().Value);

            HashSet<string> packagesToUpdate = new HashSet<string>();
            var packages = projectFiles.SelectMany(x => x.ListPackages())
                ?.GroupBy(x => x.Key)
                ?.ToDictionary(x => x.Key, x => x.First().Value);

            foreach (var item in latestPackageVersions)
            {
                if (NuGetVersion.TryParse(packages[item.Key], out var projectVersion))
                {
                    var lastVersion = item.Value;
                    if (lastVersion.Version > projectVersion)
                    {
                        // Major minor versions older than 21 days
                        var shouldUpdateMajorMinor =
                            (lastVersion.Version.Major > projectVersion.Major || lastVersion.Version.Minor > projectVersion.Minor)
                                && lastVersion.Released <= DateTime.Now.AddDays(-PackagesMustBePublishedForThisManyDays);

                        var shouldUpdatePatch = lastVersion.Version.Major == projectVersion.Major
                                                && lastVersion.Version.Minor == projectVersion.Minor
                                                && lastVersion.Version > projectVersion;

                        if (shouldUpdateMajorMinor || shouldUpdatePatch)
                        {
                            packagesToUpdate.Add($"{item.Key}|{item.Value.Version}");
                        }
                    }
                }
                else
                {
                    _log.WriteLine($"Unable to parse package version '{packages[item.Key]}' for '{item.Key}'.");
                }
            }

            var packageGroupings =
                packagesToUpdate
                    .Select(x => x.Split('|'))
                    .GroupBy(x => PackageGroupings.Select(y => y.GetGroupName(x[0], x[1])).FirstOrDefault(y => y != null),
                        x => x[0])
                    .Where(x => x.Key != null);

            var changeSets = new List<ChangeSet>();

            foreach (var group in packageGroupings)
            {
                var message =
                    new StringBuilder($"Auto-update for packages related to '{@group.Key}'");
                message.AppendLine();
                message.AppendLine();

                var groupChangeSet = new ChangeSet
                {
                    BranchName = $"refs/heads/auto-nuget-update/{@group.Key.ToLowerInvariant()}",
                };

                foreach (var package in @group)
                {
                    var packageUpdateMessage =
                        $"Updates package '{package}' to version '{latestPackageVersions[package].Version}'";

                    if (@group.Count() == 1)
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
                            latestPackageVersions[package].Version.ToString());
                    }
                }

                foreach (var projectFile in projectFiles)
                {
                    var change = projectFile.ToChange();
                    if (change != null)
                    {
                        groupChangeSet.Changes.Add(change);
                    }

                    projectFile.Reset();
                }

                groupChangeSet.Message = message.ToString();
                changeSets.Add(groupChangeSet);
            }

            return changeSets;
        }
    }
}