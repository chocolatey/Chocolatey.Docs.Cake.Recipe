public static class Environment
{
    public static string DeployRemoteVariable { get; private set; }
    public static string DeployBranchVariable { get; private set; }
    public static string GitHubTokenVariable { get; private set; }

    public static void SetVariableNames(
        string deployRemoteVariable = null,
        string deployBranchVariable = null,
        string gitHubTokenVariable = null)
    {
        DeployRemoteVariable = deployRemoteVariable ?? "STATIQ_DEPLOY_REMOTE";
        DeployBranchVariable = deployBranchVariable ?? "STATIQ_DEPLOY_BRANCH";
        GitHubTokenVariable = gitHubTokenVariable ?? "STATIQ_GITHUB_TOKEN";
    }
}