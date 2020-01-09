using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NugetPackageUpdates
{
    public class AzureDevOps : IRepositorySource, IDisposable
    {
        readonly HttpClient _client = new HttpClient();
        readonly string _apiBase;
        readonly TextWriter _log;

        public AzureDevOps(string token, string organization, string project, string repo, TextWriter log)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
            _log = log;

            _apiBase = $"https://{organization}.visualstudio.com/{project}/_apis/git/repositories/{repo}";
        }

        public async Task<ICollection<string>> FindProjectFiles()
        {
            var response = await _client.GetAsync($"{_apiBase}/items?api-version=2.0-preview&versionType=branch&Version=master&recursionLevel=Full");

            response.EnsureSuccessStatusCode();

            var content = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());

            var results = new List<string>();

            foreach (dynamic item in content.value)
            {
                if (((string)item.path).EndsWith("csproj"))
                {
                    results.Add((string)item.path);
                }
            }

            return results;
        }

        public async Task<ProjectFile> GetProjectFile(string projectPath)
        {
            _log.WriteLine($"Fetching '{projectPath}'");

            var response = await _client.GetAsync($"{_apiBase}/items?api-version=2.0-preview&versionType=branch&Version=master&scopePath={projectPath}");
            return new ProjectFile(projectPath, await response.Content.ReadAsByteArrayAsync());
        }

        public async Task<TextFile> GetTextFile(string path)
        {
            _log.WriteLine($"Fetching '{path}'");

            var response = await _client.GetAsync($"{_apiBase}/items?api-version=2.0-preview&versionType=branch&Version=master&scopePath={path}");
            return new TextFile(path, await response.Content.ReadAsByteArrayAsync());
        }

        public async Task SubmitPR(ChangeSet changeSet)
        {
            await SubmitPR(changeSet, new string[0]);
        }

        public async Task SubmitPR(ChangeSet changeSet, string[] reviewers)
        {
            if (changeSet == null) throw new ArgumentNullException(nameof(changeSet));
            if (reviewers == null) throw new ArgumentNullException(nameof(reviewers));

            _log.WriteLine("Finding master object id");

            var response = await _client.GetAsync($"{_apiBase}/refs?api-version=2.0-preview&filter=heads%2Fmaster");
            var obj = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
            string masterObjectId = obj.value[0].objectId;

            var vstsChanges = changeSet.Changes.Select(x => new
            {
                changeType = "edit",
                item = new
                {
                    path = x.FilePath
                },
                newContent = new
                {
                    content = x.FileContents,
                    contentType = "rawText"
                }
            }).ToArray();

            var createBranch = new
            {
                refUpdates = new[] {
                    new
                    {
                        name = changeSet.BranchName,
                        oldObjectId = masterObjectId
                    }
                },
                commits = new[] {
                    new
                    {
                        comment = changeSet.Message,
                        changes = vstsChanges,
                    }
                }
            };

            _log.WriteLine($"Creating branch {changeSet.BranchName}");

            var branchResult = await _client.PostAsync(
                $"{_apiBase}/pushes?api-version=2.0-preview&versionType=branch&Version=master",
                new StringContent(JsonConvert.SerializeObject(createBranch), Encoding.UTF8, "application/json"));

            branchResult.EnsureSuccessStatusCode();

            var messageLines = changeSet.Message.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            var pullRequest = new
            {
                sourceRefName = changeSet.BranchName,
                targetRefName = "refs/heads/master",
                title = messageLines.First(),
                description = string.Join(Environment.NewLine, messageLines.Skip(1)),
                reviewers = reviewers.Select(x => new { id = x }).ToArray()
            };

            _log.WriteLine($"Creating pull request");
            var prResult = await _client.PostAsync(
                $"{_apiBase}/pullRequests?api-version=2.0-preview",
                new StringContent(JsonConvert.SerializeObject(pullRequest), Encoding.UTF8, "application/json"));

            var pr = JsonConvert.DeserializeObject<dynamic>(await prResult.Content.ReadAsStringAsync());

            prResult.EnsureSuccessStatusCode();

            var setAutoComplete = new
            {
                autoCompleteSetBy = new
                {
                    id = (string)pr.createdBy.id
                },
                completionOptions = new
                {
                    mergeCommitMessage = changeSet.Message,
                    deleteSourceBranch = true,
                    squashMerge = true,
                    bypassPolicy = false
                }
            };

            _log.WriteLine("Setting auto-complete");
            await _client.SendAsync(new HttpRequestMessage(new HttpMethod("PATCH"), $"{_apiBase}/pullRequests/{pr.pullRequestId}?api-version=2.0-preview")
            {
                Content = new StringContent(JsonConvert.SerializeObject(setAutoComplete), Encoding.UTF8, "application/json")
            });
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}