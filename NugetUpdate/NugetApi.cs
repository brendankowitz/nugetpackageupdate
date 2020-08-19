using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;

namespace NugetPackageUpdates
{
    public class NugetApi
    {
        private readonly Uri _indexUri;
        private readonly AuthenticationHeaderValue _authorizationHeader;
        private readonly TextWriter _log;

        public static readonly Func<TextWriter, NugetApi> Official = logger => new NugetApi(new Uri("https://api.nuget.org/v3-index/index.json"), null, logger);

        public NugetApi(Uri indexUri, AuthenticationHeaderValue authorizationHeader, TextWriter log)
        {
            if (!indexUri.ToString().EndsWith("index.json"))
            {
                throw new ArgumentException($"{nameof(indexUri)} should end with index.json, e.g. https://api.nuget.org/v3-index/index.json");
            }

            _indexUri = indexUri;
            _authorizationHeader = authorizationHeader;
            _log = log;
        }

        public async Task<IDictionary<string, NugetPackage>> GetPackageVersions(
            IEnumerable<string> packages,
            ICollection<string> allowBetaVersions)
        {
            var results = new Dictionary<string, NugetPackage>();

            using HttpClient client = CreateHttpClient();

            if (_authorizationHeader != null)
            {
                client.DefaultRequestHeaders.Authorization = _authorizationHeader;
            }

            var indexDocumentJson = await client.GetStringAsync(_indexUri);
            var indexDocument = JObject.Parse(indexDocumentJson);
            var baseUriStr = indexDocument.SelectToken("resources[?(@['@type']=='RegistrationsBaseUrl/Versioned')]['@id']")?.ToString();

            if (string.IsNullOrEmpty(baseUriStr))
            {
                throw new NotSupportedException("The node 'RegistrationsBaseUrl/Versioned' was not found in the current nuget feeds index.json");
            }

            var baseUri = new Uri(baseUriStr);

            foreach (var package in packages)
            {
                try
                {
                    var result = await client.GetAsync($"{baseUri.ToString().Trim('/')}/{package.ToLowerInvariant()}/index.json");

                    result.EnsureSuccessStatusCode();

                    string content = await result.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(content);

                    var items = obj.SelectTokens("$.items[*].items[*].catalogEntry");
                    var parse = items
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

                    _log.WriteLine($"Could not find {package} on {baseUri.Host}.");
                }
            }

            return results;
        }

        protected virtual HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            };

            return new HttpClient(handler);
        }
    }
}