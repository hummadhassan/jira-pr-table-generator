namespace JiraPrTable.Settings
{
    public class AppSettings 
    { 
        public JiraSettings Jira { get; set; } = new JiraSettings();

        public AzureDevOpsSettings AzureDevOps { get; set; } = new AzureDevOpsSettings();
    }
}
