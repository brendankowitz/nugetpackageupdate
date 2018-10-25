using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NugetPackageUpdates
{
    public class NugetApi
    {
        public static async Task<IDictionary<string, ICollection<string>>> GetPackageVersions(IEnumerable<string> packages, TextWriter log)
        {
            var results = new Dictionary<string, ICollection<string>>();

            using (var client = new HttpClient())
            {
                foreach (var package in packages)
                {
                    try
                    {
                        var versions = await client.GetAsync($"https://api.nuget.org/v3-flatcontainer/{package}/index.json");
                        var json = await versions.Content.ReadAsStringAsync();
                        var content = JsonConvert.DeserializeObject<dynamic>(json);
                        string[] value = content.versions.ToObject<string[]>();

                        if (value?.Any() == true)
                        {
                            results.Add(package, value);
                        }
                        else
                        {
                            log.WriteLine($"{package} had no released versions");
                        }
                    }
                    catch
                    {
                        log.WriteLine($"Could not find {package} on nuget.org.");
                    }
                }
            }

            return results;
        }
    }
}