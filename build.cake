/** ARGUMENTS **/

var configuration = Argument("Configuration", "Release");
var target = Argument("Target", "Default");
var solution = Argument<string>("Solution", null);

/** VARIABLES **/

var root = MakeAbsolute(new DirectoryPath("../"));

var folders = new 
{
    root = root,
    artefacts = root + "/artefacts",
    src = root + "/src",
    apps = root + "/apps",
    tests = root + "/tests"
};

/** TASKS **/

Task("Clean")
.Does(() =>
{
    CleanDirectories(new DirectoryPath[]
    {
        folders.artefacts
    });

    CleanDirectories(folders.src + "/**/bin/" + configuration);
    CleanDirectories(folders.apps + "/**/bin/" + configuration);
    CleanDirectories(folders.tests + "/**/bin/" + configuration);
});

Task("Build")
.IsDependentOn("Clean")
.Does(() =>
{
    var solutionFile = GetSolutionFile(root, solution);

    if (solutionFile is object)
    {
        Information($"Building solution: {solutionFile.FullPath}");

        DotNetCoreBuild(solutionFile.FullPath, new DotNetCoreBuildSettings
        {
            Configuration = configuration
        });
    }
});

Task("Test")
.IsDependentOn("Build")
.Does(() =>
{
    var projects = GetFiles(folders.tests + "/**/*.csproj");

    foreach (var project in projects)
    {
        Information($"Running unit test project: {project.FullPath}");

        string projectName = System.IO.Path.GetFileNameWithoutExtension(project.FullPath);
        string resultsFile = $"{projectName}.xml";

        DotNetCoreTest(project.FullPath, new DotNetCoreTestSettings
        {
            Configuration = configuration,
            Logger = $"trx;LogFilename={resultsFile}",
            NoBuild = true,
            ResultsDirectory = folders.artefacts + "/test-results"
        });
    }
});

Task("Pack-Libraries")
.IsDependentOn("Test")
.Does(() =>
{
    var projects = GetFiles(folders.src + "/**/*.csproj");

    foreach (var project in projects)
    {
        Information($"Packing project: {project.FullPath}");

        DotNetCorePack(project.FullPath, new DotNetCorePackSettings
        {
            Configuration = configuration,
            NoBuild = true,
            OutputDirectory = folders.artefacts + "/packages"
        });
    }
});

Task("Default")
.IsDependentOn("Pack-Libraries");

/** EXECUTION **/

RunTarget(target);

/** FUNCTIONS **/

FilePath GetSolutionFile(DirectoryPath root, string solution)
{
    if (solution is object)
    {
        var solutionFile = root.CombineWithFilePath(solution);
        if (FileExists(solutionFile))
        {
            Information($"Using solution file: {solutionFile.FullPath}");
            return solutionFile;
        }
        else
        {
            Error($"Unable to resolve solution file: {solutionFile.FullPath}");
        }
    }
    else
    {
        var solutionFiles = GetFiles(root + "/*.sln");
        if (solutionFiles.Count == 1)
        {
            var solutionFile = solutionFiles.Single();
            Information($"Using solution file: {solutionFile.FullPath}");
            return solutionFile;
        }
        else if (solutionFiles.Count > 1)
        {
            Error($"Unable to resolve solution file, there is more than 1 solution file available at: {root.FullPath}");
        }
        else
        {
            Error("Unable to resolve solution file");
        }
    }

    return null;
}