#tool "nuget:?package=GitVersion.CommandLine"

//////////////////////////////////////////////////////////////////////
// CONFIGURATIONS
//////////////////////////////////////////////////////////////////////

var nugetSources = new[] {"https://nuget.sahbdev.dk/nuget", "https://api.nuget.org/v3/index.json"};

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var sln = Argument("sln", "");

//////////////////////////////////////////////////////////////////////
// Solution
//////////////////////////////////////////////////////////////////////

if (string.IsNullOrEmpty(sln)) {
	sln = System.IO.Directory.GetFiles("..", "*.sln")[0];
}

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectories("./../src/**/bin");
	CleanDirectories("./../src/**/obj");
	CleanDirectories("./../tests/**/bin");
	CleanDirectories("./../tests/**/obj");
});

Task("Restore-NuGet-Packages")
    .Does(() =>
{
	var settings = new DotNetCoreRestoreSettings 
    {
		Sources = nugetSources
    };

    DotNetCoreRestore(sln, settings);
});

Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
	var settings = new DotNetCoreBuildSettings
    {
		Configuration = configuration
    };

	DotNetCoreBuild(sln, settings);
});

Task("Publish")
	.IsDependentOn("Build")
	.Does(() =>
{
	var settings = new DotNetCorePublishSettings
    {
		Configuration = configuration
    };

	DotNetCorePublish(sln, settings);
});

Task("Test-CI")
    .Does(() =>
{
	foreach (var test in System.IO.Directory.GetFiles("../tests/", "*.Tests.csproj", SearchOption.AllDirectories))
	{
		var settings = new DotNetCoreTestSettings
		{
			Configuration = configuration,
			NoBuild = true,
			ArgumentCustomization = args=>args.Append("--logger \"trx;LogFileName=TestResults.trx\""),
		};
	
		DotNetCoreTest(test, settings);
	}
});

Task("Test")
	.IsDependentOn("Build")
    .IsDependentOn("Test-CI");

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Test");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
