using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NugetPackageUpdates;
using Xunit;

namespace NugetUpdate.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async Task FindPackgeUrl()
        {
            using var client = new HttpClient();
            var str = await client.GetStringAsync("https://api.nuget.org/v3-index/index.json");
            var doc = JObject.Parse(str);
            var packageUrl = doc.SelectToken("resources[?(@['@type']=='RegistrationsBaseUrl/Versioned')]['@id']")?.ToString();

            Assert.NotNull(packageUrl);
        }

        [Fact]
        public async Task FindPackageUpdates()
        {
            var nugetApi = NugetApi.Official(Console.Out);

            var packages = new string[]
            {
                "AngleSharp",
                "Ensure.That",
                "FluentValidation",
                "Hl7.Fhir.R4",
                "Hl7.Fhir.R5",
                "Hl7.Fhir.Serialization",
                "Hl7.Fhir.STU3",
                "Hl7.FhirPath",
                "IdentityServer4",
                "MediatR",
                "MediatR.Extensions.Microsoft.DependencyInjection",
                "Microsoft.ApplicationInsights.AspNetCore",
                "Microsoft.AspNetCore.Authentication.JwtBearer",
                "Microsoft.AspNetCore.Mvc.NewtonsoftJson",
                "Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation",
                "Microsoft.AspNetCore.Mvc.Testing",
                "Microsoft.AspNetCore.TestHost",
                "Microsoft.Azure.Cosmos",
                "Microsoft.Azure.DocumentDB.Core",
                "Microsoft.Azure.KeyVault",
                "Microsoft.Azure.Services.AppAuthentication",
                "Microsoft.Azure.Storage.Blob",
                "Microsoft.Extensions.Configuration.AzureKeyVault",
                "Microsoft.Extensions.Configuration.Json",
                "Microsoft.Extensions.Diagnostics.HealthChecks",
                "Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions",
                "Microsoft.Extensions.FileProviders.Embedded",
                "Microsoft.Extensions.Hosting.Abstractions",
                "Microsoft.Extensions.Http",
                "Microsoft.Extensions.Logging",
                "Microsoft.Extensions.Logging.Abstractions",
                "Microsoft.Extensions.Logging.ApplicationInsights",
                "Microsoft.Extensions.Options.ConfigurationExtensions",
                "Microsoft.IdentityModel.JsonWebTokens",
                "Microsoft.IdentityModel.Protocols.OpenIdConnect",
                "Microsoft.IO.RecyclableMemoryStream",
                "Microsoft.NET.Test.Sdk",
                "Microsoft.SqlServer.DACFx",
                "Microsoft.SqlServer.SqlManagementObjects",
                "Newtonsoft.Json",
                "Newtonsoft.Json.Schema",
                "NSubstitute",
                "Polly",
                "prometheus-net.AspNetCore",
                "prometheus-net.DotNetRuntime",
                "prometheus-net.SystemMetrics",
                "selenium.chrome.webdriver",
                "selenium.webdriver",
                "System.Collections.Immutable",
                "System.Data.SqlClient",
                "System.Diagnostics.PerformanceCounter",
                "System.IO.FileSystem.AccessControl",
                "System.Net.Http",
                "xunit",
                "xunit.runner.visualstudio",
            };

            var allowBetaVersions = new List<string>();
            allowBetaVersions.Add("System.CommandLine.Experimental");
            allowBetaVersions.Add("System.CommandLine.Rendering");

            var results = await nugetApi.GetPackageVersions(packages, allowBetaVersions);

            Assert.NotEmpty(results);
            foreach (var result in results)
            {
                Assert.NotNull(result.Value);
            }
        }

        [Fact]
        public void GivenAProjectFileWithNestedVersionNode_WhenUpdating_ThenItUpdatesCorrectly()
        {
            var json = @"<Project><ItemGroup><PackageReference Include=""Test""><Version>1.0.0</Version></PackageReference></ItemGroup></Project>";
            var proj = new ProjectFile("Test.csproj", Encoding.UTF8.GetBytes(json));

            Assert.True(proj.UpdatePackageReference("Test", "2.0.0"));
        }

        [Fact]
        public void GivenAProjectFileWithAttributeVersion_WhenUpdating_ThenItUpdatesCorrectly()
        {
            var json = @"<Project><ItemGroup><PackageReference Include=""Test"" Version=""1.0.0"" /></ItemGroup></Project>";
            var proj = new ProjectFile("Test.csproj", Encoding.UTF8.GetBytes(json));

            Assert.True(proj.UpdatePackageReference("Test", "2.0.0"));
        }
    }
}