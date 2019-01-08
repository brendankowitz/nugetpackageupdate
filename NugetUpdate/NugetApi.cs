using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;

namespace NugetPackageUpdates
{
    public class NugetApi
    {
        private readonly Uri _baseUri;
        private readonly AuthenticationHeaderValue _authorizationHeader;
        private readonly TextWriter _log;

        public static readonly Func<TextWriter, NugetApi> Official = logger => new NugetApi(new Uri("https://api.nuget.org/v3/registration3"), null, logger);

        public NugetApi(Uri baseUri, AuthenticationHeaderValue authorizationHeader, TextWriter log)
        {
            _baseUri = baseUri;
            _authorizationHeader = authorizationHeader;
            _log = log;
        }

        public async Task<IDictionary<string, NugetPackage>> GetPackageVersions(
            IEnumerable<string> packages,
            ICollection<string> allowBetaVersions)
        {
            var results = new Dictionary<string, NugetPackage>();

            using (var client = CreateHttpClient())
            {
                if (_authorizationHeader != null)
                {
                    client.DefaultRequestHeaders.Authorization = _authorizationHeader;
                }

                foreach (var package in packages)
                {
                    try
                    {
                        var result = await client.GetAsync($"{_baseUri.ToString().Trim('/')}/{package.ToLowerInvariant()}/index.json");

                        result.EnsureSuccessStatusCode();

                        var content = await result.Content.ReadAsStringAsync();
                        var obj = JObject.Parse(content);

                        var parse = obj["items"]
                            .SelectMany(x => x["items"])
                            .Select(x => x["catalogEntry"])
                            .Select(x => new NugetPackage(package, NuGetVersion.Parse(x.Value<string>("version")), x.Value<DateTime>("published"), x.Value<bool>("listed")))
                            .ToArray();

                        var selected = parse
                            .OrderByDescending(x => x.Version)
                            .Where(x => x.Listed)
                            .FirstOrDefault(x => !x.Version.IsPrerelease || allowBetaVersions.Contains(package) || allowBetaVersions.Contains("*"));

                        if (selected != null)
                        {
                            results.Add(package, selected);
                        }
                        else
                        {
                            _log.WriteLine($"{package} had no released versions");
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        if (!ex.Message.Contains("404"))
                        {
                            _log.WriteLine(ex.ToString());
                            throw;
                        }

                        _log.WriteLine($"Could not find {package} on {_baseUri.Host}.");
                    }
                }
            }

            return results;
        }

        protected virtual HttpClient CreateHttpClient()
        {
            return new HttpClient();
        }
    }
}