# Nuget Package Update lib

This library is designed to be used via nuget and can easily be run in a Console app or Azure Function.

```csharp

var nugetApi = new NugetApi(
                new Uri("https://<org>.pkgs.visualstudio.com/_packaging/<feedguid>/nuget/v3/registrations2/"),
                new AuthenticationHeaderValue("Basic", "<token>"),
                Console.Out
                );

var manager = new UpdateManager(new[] { nugetApi }, Console.Out);

// Add your own grouping conventions (defaults are provided)
manager.PackageGroupings.Add(new UniqueNameAndVersionPackageGroup("MyCompany", x => x.StartsWith("MyCompany.Packages")));

// List package names that can be beta version, or * for all
manager.AllowBetaPackages.Add("*");

// Delay taking major version updates for this many days
manager.PackagesMustBePublishedForThisManyDays = 21;

var devops = new AzureDevOps("<devopsToken>", "MyCompany", "MyProject", "my-repo", Console.Out, "<workItemAreaPath>");

// Find all project files, get all package updates, create prs with defined groups, go:
manager.CreatePullRequestsAsync(
        devops,
        reviewers: null,
        prLimit: 5,
        associatWithWorkItem: true)
    .Wait();
```

Install:
```
Install-Package NugetPackageUpdates
```

https://www.nuget.org/packages/NugetPackageUpdates/
