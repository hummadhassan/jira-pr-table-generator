namespace JiraPrTable.Dtos
{
    public record Detail(string issueId, List<PullRequest> pullRequests);
}
