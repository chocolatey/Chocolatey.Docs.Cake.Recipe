public class BuildData
{
    public string DeployRemote { get; set; }
    public string DeployBranch { get; set; }
    public string GitHubToken { get; set; }
    public FilePath ProjectFilePath { get; set; }
    public DirectoryPath PublishDirectory { get; set; }
    public DirectoryPath OutputDirectory { get; set; }
    public string Target { get; private set; }
    public string Configuration { get; private set; }

    public BuildData(ICakeContext context, FilePath projectFilePath, DirectoryPath publishDirectory, DirectoryPath outputDirectory)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        DeployRemote = context.EnvironmentVariable(Environment.DeployRemoteVariable);
        DeployBranch = context.EnvironmentVariable(Environment.DeployBranchVariable);
        GitHubToken = context.EnvironmentVariable(Environment.GitHubTokenVariable);
        ProjectFilePath = projectFilePath;
        PublishDirectory = publishDirectory;
        OutputDirectory = outputDirectory;
        Configuration = context.Argument("configuration", "Release");
    }
}