using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NugetPackageUpdates
{
    public class Github : IRepositorySource
    {
        readonly HttpClient _client = new HttpClient();
        private readonly string _githubToken;
        private readonly string _owner;
        private readonly string _project;
        private readonly string _defaultBranch;
        readonly TextWriter _log;

        public Github(string githubToken, string owner, string project, string defaultBranch, TextWriter log)
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", githubToken);
            _client.BaseAddress = new Uri("https://api.github.com");
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Add("User-Agent", ".NET Client");

            _githubToken = githubToken;
            _owner = owner;
            _project = project;
            _defaultBranch = defaultBranch;
            _log = log;
        }

        private async Task<bool> PrExists(string branch)
        {
            var response = await _client.GetAsync($"/repos/{_owner}/{_project}/git/refs/{branch.Replace("refs/", string.Empty)}");

            return response.IsSuccessStatusCode;
        }

        public async Task SubmitPR(ChangeSet changeSet, string[] reviewers)
        {
            if (await PrExists(changeSet.BranchName))
            {
                _log.WriteLine($"Branch '{changeSet.BranchName}' already exists.");
                return;
            }

            _log.WriteLine("Finding master sha");

            var response = await _client.GetAsync($"/repos/{_owner}/{_project}/git/refs/heads/{_defaultBranch}");
            var value = await response.Content.ReadAsStringAsync();
            var details = JsonConvert.DeserializeObject<dynamic>(value);

            var masterSha = (string)details.@object.sha;
            var commitUrl = (string)details.@object.url;

            var lastCommit = await _client.GetAsync(commitUrl);
            var lastCommitObj = JsonConvert.DeserializeObject<dynamic>(await lastCommit.Content.ReadAsStringAsync());
            var masterTreeSha = (string)lastCommitObj.tree.sha;

            var changeSetTree = new
            {
                base_tree = masterTreeSha,
                tree = changeSet.Changes.Select(x => new
                {
                    path = x.FilePath,
                    mode = "100644", //create blob
                    type = "blob",
                    content = x.FileContents,
                }).ToArray()
            };

            _log.WriteLine("Posting changeset");

            var treeResult = await _client.PostAsync(
                $"/repos/{_owner}/{_project}/git/trees",
                new StringContent(JsonConvert.SerializeObject(changeSetTree), Encoding.UTF8, "application/json"));
            var treeContentStr = await treeResult.Content.ReadAsStringAsync();
            var treeContent = JsonConvert.DeserializeObject<dynamic>(treeContentStr);

            _log.WriteLine("Creating new commit");

            var commit = new
            {
                message = changeSet.Message,
                parents = new[] { masterSha },
                tree = (string)treeContent.sha
            };

            var commitResult = await _client.PostAsync(
                $"/repos/{_owner}/{_project}/git/commits",
                new StringContent(JsonConvert.SerializeObject(commit), Encoding.UTF8, "application/json"));
            var commitContentStr = await commitResult.Content.ReadAsStringAsync();
            var commitContent = JsonConvert.DeserializeObject<dynamic>(commitContentStr);

            _log.WriteLine("Creating branch");

            var newRef = new
            {
                @ref = changeSet.BranchName,
                sha = (string)commitContent.sha
            };

            var refResult = await _client.PostAsync(
                $"/repos/{_owner}/{_project}/git/refs",
                new StringContent(JsonConvert.SerializeObject(newRef), Encoding.UTF8, "application/json"));

            refResult.EnsureSuccessStatusCode();

            var messageLines = changeSet.Message.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            _log.Write("Opening PR...");

            var newPr = new
            {
                title = messageLines.First(),
                body = string.Join(Environment.NewLine, messageLines.Skip(1)),
                head = $"{_owner}:{changeSet.BranchName.Replace("refs/heads/", string.Empty)}",
                @base = _defaultBranch
            };

            var prResult = await _client.PostAsync(
                $"/repos/{_owner}/{_project}/pulls",
                new StringContent(JsonConvert.SerializeObject(newPr), Encoding.UTF8, "application/json"));

            if (!prResult.IsSuccessStatusCode)
            {
                var prContentStr = await prResult.Content.ReadAsStringAsync();
                _log.WriteLine($"failed to create PR :( {Environment.NewLine} {prContentStr}");
            }

            _log.WriteLine("OK");
        }

        public async Task<ProjectFile> GetProjectFile(string projectPath)
        {
            _log.WriteLine($"Fetching '{projectPath}'");

            var response = await _client.GetAsync($"/repos/{_owner}/{_project}/contents/{projectPath}");
            var value = await response.Content.ReadAsStringAsync();

            var details = JsonConvert.DeserializeObject<dynamic>(value);
            var fromBase64String = Convert.FromBase64String((string)details.content);

            return new ProjectFile(projectPath, fromBase64String, true);
        }

        public async Task<TextFile> GetTextFile(string path)
        {
            _log.WriteLine($"Fetching '{path}'");

            var response = await _client.GetAsync($"/repos/{_owner}/{_project}/contents/{path}");
            var value = await response.Content.ReadAsStringAsync();

            var details = JsonConvert.DeserializeObject<dynamic>(value);
            var fromBase64String = Convert.FromBase64String((string)details.content);

            return new TextFile(path, fromBase64String, true);
        }

        public async Task<ICollection<string>> FindProjectFiles()
        {
            var results = new List<string>();

            var path = $"/search/code?q=.csproj+in:path+repo:{_owner}/{_project}";
            var page = 1;

            var response = await _client.GetAsync(path);
            var value = await response.Content.ReadAsStringAsync();

            var content = JsonConvert.DeserializeObject<dynamic>(value);

            while (content.items.Count > 0)
            {
                foreach (dynamic item in content.items)
                {
                    if (((string)item.path).EndsWith("csproj"))
                    {
                        results.Add((string)item.path);
                    }
                }

                page++;
                response = await _client.GetAsync($"{path}&page={page}");
                value = await response.Content.ReadAsStringAsync();

                content = JsonConvert.DeserializeObject<dynamic>(value);
            }

            return results;
        }
    }
}