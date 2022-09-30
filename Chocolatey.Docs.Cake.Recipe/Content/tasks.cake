public class BuildTasks
{
    // Build Tasks
    public CakeTaskBuilder CleanTask { get; set; }
    public CakeTaskBuilder YarnInstallTask { get; set; }
    public CakeTaskBuilder RunGulpTask { get; set; }
    public CakeTaskBuilder StatiqPreviewTask { get; set; }
    public CakeTaskBuilder StatiqBuildTask { get; set; }
    public CakeTaskBuilder StatiqLinkValidationTask { get; set; }
    public CakeTaskBuilder PublishDocumentationTask { get; set; }
    public CakeTaskBuilder DefaultTask { get; set; }
}