using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JiraPrTable.Dtos;
using JiraPrTable.Services;
using JiraPrTable.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

public class JiraFixModel : PageModel
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly AzureDevOpsService _azureDevOpsService;
    public readonly JiraSettings _jiraSettings;
    public readonly AzureDevOpsSettings _devOpsSettings;
    
    public JiraFixModel(IHttpClientFactory httpFactory, IOptions<JiraSettings> opts,
        IOptions<AzureDevOpsSettings> devOps,
        AzureDevOpsService azureDevOpsService)
    {
        _httpFactory = httpFactory;
        _azureDevOpsService = azureDevOpsService;
        _jiraSettings = opts.Value;
        _devOpsSettings = devOps.Value;
        
    }

    [BindProperty(SupportsGet = true)]
    public string FixVersion { get; set; } = "";

    [BindProperty(SupportsGet = true)]
    public string? ProjectKeyOverride { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? AzureProjectKeyOverride { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? DomainOverride { get; set; }

    public List<TicketInfo> Tickets { get; private set; } = new();

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(FixVersion) || string.IsNullOrWhiteSpace(_jiraSettings.AuthKey))
        {
            // nothing to do until form is submitted
            return;
        }

        try
        {
            var domain = DomainOverride ?? _jiraSettings.Domain;
            var projectKey = ProjectKeyOverride ?? _jiraSettings.ProjectKey;

            var client = _httpFactory.CreateClient("jira");
            client.BaseAddress = new Uri($"https://{domain}/");
            
            var basicAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_jiraSettings.AuthKey}"));

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);

            // 1) Search issues by JQL
            var jql = $"project = \"{projectKey}\" AND fixVersion = \"{FixVersion}\" AND issuetype != Epic";
            var searchUrl = $"/rest/api/2/search?jql={Uri.EscapeDataString(jql)}&fields=summary&maxResults=1000";
            var searchResp = await client.GetAsync(searchUrl);
            searchResp.EnsureSuccessStatusCode();

            using var js = await searchResp.Content.ReadAsStreamAsync();
            var searchResult = await JsonSerializer.DeserializeAsync<JiraSearchResult>(js,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });


            Tickets = await (_devOpsSettings.Enabled ? ReadDevopsPrList(searchResult): ReadJiraPrList(client, searchResult));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>
    /// Get PR details from Azure Devops
    /// </summary>    
    /// <param name="searchResult"></param>
    /// <returns></returns>
    private async Task<List<TicketInfo>> ReadDevopsPrList(JiraSearchResult searchResult)
    {
        
        var list = new List<TicketInfo>();

        // Ensure at least one call to FetchPrUrlsAsync to populate the cache
        var firstIssue = searchResult?.issues?.FirstOrDefault();
        if (firstIssue != null)
        {
            await _azureDevOpsService.FetchPrUrlsAsync(firstIssue.key, AzureProjectKeyOverride);
        }

        // Process remaining issues in parallel
        var tasks = (searchResult?.issues ?? Enumerable.Empty<Issue>())                   
                   .Select(async issue =>
                   {
                       // Only tickets with at least one matching PR
                       var prUrls = await _azureDevOpsService.FetchPrUrlsAsync(issue.key, AzureProjectKeyOverride);

                       if (prUrls.Any())
                       {
                           return new TicketInfo(issue.key, issue.fields.summary, prUrls);
                       }

                       return null;
                   });

        var results = await Task.WhenAll(tasks);
        list.AddRange(results.Where(ticket => ticket != null)
            .ToList());

        return list;
    }

    /// <summary>
    /// Fetch PR details from Jira
    /// You may need to change the application type keyword in the requrest URL from "github" to "bitbucket" or "gitlab" depending upon your setup
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>

    private async Task<List<TicketInfo>> ReadJiraPrList(HttpClient client, JiraSearchResult searchResult)
    {
         // 2) Fetch PR details
        var list = new List<TicketInfo>();
        foreach (var issue in searchResult?.issues ?? Enumerable.Empty<Issue>())
        {
            var detailUrl = $"/rest/dev-status/1.0/issue/detail?issueId={issue.id}&applicationType=github&dataType=pullrequest";
            var detailResp = await client.GetAsync(detailUrl);
            detailResp.EnsureSuccessStatusCode();

            using var ds = await detailResp.Content.ReadAsStreamAsync();
            var detailResult = await JsonSerializer.DeserializeAsync<DevStatusResult>(ds,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var prUrls = detailResult
                        ?.detail
                        .SelectMany(d => d.pullRequests.Select(pr => pr.url))
                        .Distinct()
                        .ToList()
                      ?? new();

            if (prUrls.Any())
            {
                list.Add(new TicketInfo(issue.key, issue.fields.summary, prUrls));
            }
        }

        return list;
    }
}
