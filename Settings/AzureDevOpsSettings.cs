namespace JiraPrTable.Settings
{
    public record AzureDevOpsSettings
    {
        public bool Enabled { get; init; } = false;
        public string Organization { get; init; } = "";
        public string Project { get; init; } = "";
        public string PersonalAccessToken { get; init; } = "";
        public string ApiVersion { get; init; } = "7.1";
        public int PageSize { get; init; } = 1000;
    }
}
