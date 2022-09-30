#load nuget:?package=Cake.Recipe&version=2.2.1

Environment.SetVariableNames();

BuildParameters.SetParameters(context: Context,
                            buildSystem: BuildSystem,
                            sourceDirectoryPath: "./src",
                            title: "Chocolatey.Docs.Cake.Recipe",
                            repositoryOwner: "chocolatey",
                            repositoryName: "Chocolatey.Docs.Cake.Recipe",
                            appVeyorAccountName: "chocolatey",
                            nuspecFilePath: "./Chocolatey.Docs.Cake.Recipe/Chocolatey.Docs.Cake.Recipe.nuspec",
                            preferredBuildProviderType: BuildProviderType.TeamCity);

BuildParameters.PrintParameters(Context);

ToolSettings.SetToolSettings(context: Context);

ToolSettings.SetToolPreprocessorDirectives(
                                        gitReleaseManagerTool: "#tool nuget:?package=GitReleaseManager&version=0.13.0",
                                        gitReleaseManagerGlobalTool: "#tool dotnet:?package=GitReleaseManager.Tool&version=0.13.0");

BuildParameters.Tasks.CleanTask
    .IsDependentOn("Generate-Version-File");

Task("Generate-Version-File")
    .Does<BuildVersion>((context, buildVersion) => {
        var buildMetaDataCodeGen = TransformText(@"
        public class BuildMetaData
        {
            public static string Date { get; } = ""<%date%>"";
            public static string Version { get; } = ""<%version%>"";
            public static string CakeVersion { get; } = ""<%cakeversion%>"";
        }",
        "<%",
        "%>"
        )
   .WithToken("date", BuildMetaData.Date)
   .WithToken("version", buildVersion.SemVersion)
   .WithToken("cakeversion", BuildMetaData.CakeVersion)
   .ToString();

    System.IO.File.WriteAllText(
        "./Chocolatey.Docs.Cake.Recipe/Content/version.cake",
        buildMetaDataCodeGen
        );
    });

Build.RunNuGet();
