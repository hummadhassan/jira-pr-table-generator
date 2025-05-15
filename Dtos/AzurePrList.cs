namespace JiraPrTable.Dtos
{
    public record AzurePrList(int Count, List<AzurePr> Value);

    public record AzurePr(
        int PullRequestId,
        string Title,
        string? Description,
        string SourceRefName,
        string Status,
        DateTime CreationDate,
        Repository Repository    // <-- new
    );

    public record Repository(
        string Name,             // repo name
        ProjectInfo Project      // project block
    );

    public record ProjectInfo(
        string Name              // project name
    );
}
