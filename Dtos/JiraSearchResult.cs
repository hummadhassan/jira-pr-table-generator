namespace JiraPrTable.Dtos
{
    public record JiraSearchResult(int total, List<Issue> issues);
}
