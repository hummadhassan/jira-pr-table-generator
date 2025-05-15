using JiraPrTable.Dtos;
using JiraPrTable.Settings;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace JiraPrTable.Services
{
    public class AzureDevOpsService
    {
        private readonly HttpClient _client;
        private readonly AzureDevOpsSettings _cfg;
        private readonly IMemoryCache _cache;

        // cache key and duration
        private const string CacheKey = "AzureDevOpsPrList";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public AzureDevOpsService(IHttpClientFactory httpFactory,
                                  IOptions<AzureDevOpsSettings> options,
                                  IMemoryCache cache)
        {
            _client = httpFactory.CreateClient("azure");
            _cfg = options.Value;
            _cache = cache;
        }

        /// <summary>
        /// Returns the web URLs of all non-abandoned PRs created in the last six months
        /// whose title/description/branch mentions the given Jira issueKey.
        /// Caches the full PR list for <see cref="CacheDuration"/>.
        /// </summary>
        public async Task<List<string>> FetchPrUrlsAsync(string issueKey, string? overridenProjectName = null)
        {
            var projectName = _cfg.Project;

            if (!String.IsNullOrWhiteSpace(overridenProjectName))
            {
                projectName = overridenProjectName;
            }

            // Fetch or get from cache the raw PR list
            var prList = await _cache.GetOrCreateAsync($"{CacheKey}.{projectName}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;

                // calculate cutoff (six months ago)
                var minTime = DateTime.UtcNow.AddMonths(-6).ToString("O");

                // build query so Azure only returns recent PRs
                var qs = new StringBuilder()
                    .Append("searchCriteria.status=all")
                    .Append("&searchCriteria.minTime=").Append(Uri.EscapeDataString(minTime))
                    .Append("&searchCriteria.queryTimeRangeType=created")
                    .Append("&$top=").Append(_cfg.PageSize);

                var url = $"{projectName}/_apis/git/pullrequests?{qs}&api-version={_cfg.ApiVersion}";
                var resp = await _client.GetAsync(url);
                resp.EnsureSuccessStatusCode();

                await using var stream = await resp.Content.ReadAsStreamAsync();
                var wrapper = await JsonSerializer.DeserializeAsync<AzurePrList>(stream,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // return the full list to cache
                return wrapper?.Value ?? new List<AzurePr>();
            });

            // Now filter in memory for this specific issue key
            var matches = prList?
                .Where(pr => !pr.Status.Equals("abandoned", StringComparison.OrdinalIgnoreCase))
                .Where(pr =>
                    pr.Title.Contains(issueKey, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(pr.Description) &&
                      pr.Description.Contains(issueKey, StringComparison.OrdinalIgnoreCase)) ||
                    pr.SourceRefName.Contains(issueKey, StringComparison.OrdinalIgnoreCase)
                )
                .Select(pr =>
                {
                    // URL‐encode project and repo names
                    var proj = Uri.EscapeDataString(pr.Repository.Project.Name);
                    var repo = Uri.EscapeDataString(pr.Repository.Name);
                    return $"https://dev.azure.com/{_cfg.Organization}/{proj}/_git/{repo}/pullrequest/{pr.PullRequestId}";
                })
                .Distinct()
                .ToList();
                

            return matches ?? new List<string>();
        }
    }
}
