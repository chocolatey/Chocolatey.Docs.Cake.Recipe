///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup<BuildData>(context =>
{
    Information("Setting up BuildData...");

    var buildData = new BuildData(context, BuildParameters.ProjectFilePath, BuildParameters.PublishDirectory, BuildParameters.OutputDirectory, BuildParameters.VirtualDirectory);

    return buildData;
});

///////////////////////////////////////////////////////////////////////////////
// TASK DEFINITIONS
///////////////////////////////////////////////////////////////////////////////

BuildParameters.Tasks.CleanTask = Task("Clean")
    .Does<BuildData>((context, buildData) =>
{
    var directoriesToClean = new []{
        buildData.PublishDirectory,
        buildData.OutputDirectory,
        "./bin",
        "./obj",
        "./temp",
        "./wwwroot"
    };

    CleanDirectories(directoriesToClean);
});

BuildParameters.Tasks.YarnInstallTask = Task("Yarn-Install")
    .WithCriteria(() => FileExists("./package.json"), "package.json file not found in repository")
    .IsDependentOn("Clean")
    .Does(() =>
{
    if (BuildSystem.IsLocalBuild)
    {
        Information("Running yarn install...");
        Yarn.Install();
    }
    else
    {
        Information("Running yarn install --immutable...");
        Yarn.Install(settings => settings.ArgumentCustomization = args => args.Append("--immutable"));
    }
});

BuildParameters.Tasks.RunChocoThemeTask = Task("Run-Choco-Theme")
    .IsDependentOn("Yarn-Install")
    .Does(() =>
{
    Yarn.RunScript("choco-theme");
});

BuildParameters.Tasks.StatiqPreviewTask = Task("Statiq-Preview")
    .IsDependentOn("Run-Choco-Theme")
    .Does<BuildData>((context, buildData) =>
{
    var settings = new DotNetRunSettings {
      Configuration = buildData.Configuration
    };

    var argumentBuilder = new ProcessArgumentBuilder().Append(string.Format("preview --output \"{0}\"", buildData.OutputDirectory));

    if (buildData.VirtualDirectory != null)
    {
        argumentBuilder = argumentBuilder.Append(string.Format(" --virtual-dir \"{0}\"", buildData.VirtualDirectory));
    }

    DotNetRun(buildData.ProjectFilePath.FullPath, argumentBuilder, settings);
});

BuildParameters.Tasks.StatiqBuildTask = Task("Statiq-Build")
    .IsDependentOn("Run-Choco-Theme")
    .Does<BuildData>((context, buildData) =>
{
    var settings = new DotNetRunSettings {
      Configuration = buildData.Configuration
    };

    DotNetRun(buildData.ProjectFilePath.FullPath, new ProcessArgumentBuilder().Append(string.Format("--output \"{0}\"", buildData.OutputDirectory)), settings);
});

BuildParameters.Tasks.StatiqLinkValidationTask = Task("Statiq-LinkValidation")
    .IsDependentOn("Run-Choco-Theme")
    .Does<BuildData>((context, buildData) =>
{
    var settings = new DotNetRunSettings {
      Configuration = buildData.Configuration,
      ArgumentCustomization = args => args.Append("-a ValidateRelativeLinks=Error -a ValidateAbsoluteLinks=Error")
    };

    DotNetRun(buildData.ProjectFilePath.FullPath, new ProcessArgumentBuilder().Append(string.Format("--output \"{0}\"", buildData.OutputDirectory)), settings);
});

BuildParameters.Tasks.PublishDocumentationTask = Task("Publish-Documentation")
    .IsDependentOn("Statiq-Build")
    .Does<BuildData>((context, buildData) =>
{
    var sourceCommit = GitLogTip("./");

    CleanDirectory(buildData.PublishDirectory);
    var publishFolder = buildData.PublishDirectory.Combine(DateTime.Now.ToString("yyyyMMdd_HHmmss"));

    Information("Publishing Folder: {0}", publishFolder);
    Information("Getting publish branch...");

    if (!string.IsNullOrWhiteSpace(buildData.GitHubToken))
    {
        Information("Cloning repository using token...");
        GitClone(buildData.DeployRemote, publishFolder, buildData.GitHubToken, "x-oauth-basic", new GitCloneSettings{ BranchName = buildData.DeployBranch });
    }
    else
    {
        Information("Cloning repository anonymously...");
        GitClone(buildData.DeployRemote, publishFolder, new GitCloneSettings{ BranchName = buildData.DeployBranch });
    }

    Information("Sync output files...");

    RequireTool(ToolSettings.KuduSyncGlobalTool, () => {
        Kudu.Sync(buildData.OutputDirectory, publishFolder, new KuduSyncSettings {
            ArgumentCustomization = args=>args.Append("--ignore").AppendQuoted(".git;CNAME")
        });
    });

    if (GitHasUncommitedChanges(publishFolder))
    {
        Information("Stage all changes...");
        GitAddAll(publishFolder);

        if (GitHasStagedChanges(publishFolder))
        {
            Information("Commit all changes...");
            GitCommit(
                publishFolder,
                sourceCommit.Committer.Name,
                sourceCommit.Committer.Email,
                string.Format("Continuous Integration Publish: {0}\r\n{1}", sourceCommit.Sha, sourceCommit.Message)
            );

            Information("Pushing all changes...");

            GitPush(publishFolder, buildData.GitHubToken, "x-oauth-basic", buildData.DeployBranch);
        }
        else
        {
            Information("There are no changes that need to be committed");
        }
    }
    else
    {
        Information("There are no changes that need to be staged");
    }
});

BuildParameters.Tasks.DefaultTask = Task("Default")
    .IsDependentOn("Statiq-Preview");

///////////////////////////////////////////////////////////////////////////////
// EXECUTION
///////////////////////////////////////////////////////////////////////////////

public Builder Build
{
    get
    {
        return new Builder(target => RunTarget(target));
    }
}

public class Builder
{
    private Action<string> _action;

    public Builder(Action<string> action)
    {
        _action = action;
    }

    public void Run()
    {
        _action(BuildParameters.Target);
    }
}