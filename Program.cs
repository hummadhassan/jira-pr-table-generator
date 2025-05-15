using System.Net.Http.Headers;
using System.Text;
using JiraPrTable.Services;
using JiraPrTable.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// add MemoryCache
builder.Services.AddMemoryCache();

// Bind Jira settings
builder.Services.Configure<JiraSettings>(builder.Configuration.GetSection("Jira"));

builder.Services.Configure<AzureDevOpsSettings>(
    builder.Configuration.GetSection("AzureDevOps")
);


builder.Services.AddHttpClient("azure", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IOptions<AzureDevOpsSettings>>().Value;
    client.BaseAddress = new Uri($"https://dev.azure.com/{cfg.Organization}/");
    // Basic auth with PAT
    var pat = $":{cfg.PersonalAccessToken}";
    var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes(pat));
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Basic", encoded);
});


// Register HttpClient for Jira
builder.Services.AddHttpClient("jira", (sp, client) =>
{
    var settings = sp.GetRequiredService<IOptions<JiraSettings>>().Value;
    client.BaseAddress = new Uri($"https://{settings.Domain}/");
});

// register your service
builder.Services.AddTransient<AzureDevOpsService>();


builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();

app.Run();


