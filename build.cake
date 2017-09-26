#tool "nuget:?package=GitVersion.CommandLine"

//////////////////////////////////////////////////////////////////////
// CONFIGURATIONS
//////////////////////////////////////////////////////////////////////

var nugetSources = new[] {"https://nuget.sahbdev.dk/nuget", "https://api.nuget.org/v3/index.json"};

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var removeCompose = Argument("removeCompose", "0");
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var sln = Argument("sln", "");

//////////////////////////////////////////////////////////////////////
// Clenup build
//////////////////////////////////////////////////////////////////////
if (System.IO.Directory.GetFiles("..", "*.compose_build.sln").Any())
{
	System.IO.File.Delete(System.IO.Directory.GetFiles("..", "*.compose_build.sln").First());
}

//////////////////////////////////////////////////////////////////////
// Solution
//////////////////////////////////////////////////////////////////////

if (string.IsNullOrEmpty(sln)) {
	sln = System.IO.Directory.GetFiles("..", "*.sln")[0];
}

//////////////////////////////////////////////////////////////////////
// Remove Compose
//////////////////////////////////////////////////////////////////////

if (removeCompose == "1") {
	using(var process = StartAndReturnProcess("BuildComposeRemoval.cmd", new ProcessSettings { RedirectStandardOutput = true })) {
		process.WaitForExit();
	}
	
	var slnWithBuild = sln.Replace(".sln", ".compose_build.sln");
	using(var process = StartAndReturnProcess("dotnet", new ProcessSettings { Arguments = "run --project ComposeRemoval/src/DockerComposeBuild/DockerComposeBuild.csproj \"" + sln + "\" \"" + slnWithBuild + "\"", RedirectStandardOutput = true })) {
		process.WaitForExit();
		foreach (var info in process.GetStandardOutput())
			Information(info);
	}
	
	sln = slnWithBuild;
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
