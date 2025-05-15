namespace JiraPrTable.Settings
{
    // Configuration POCO
    public record JiraSettings
    {
        public string Domain { get; init; } = "";

        public string ProjectKey { get; init; } = "";

        public string AuthKey { get; init; } = "";
    }
}
