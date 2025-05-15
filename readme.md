# 🛠️ Jira Fix Version → PR Table Generator Web App

Welcome to your new **release hero**! This friendly Razor‑Pages tool does the heavy lifting of collecting Jira tickets and Azure DevOps PRs in under **10 seconds**, saving you **1–2 hours** of manual copy‑pasting every sprint.

---

## 🎯 What It Does

* **One-click Jira Lookup**: Fetches all issues in your Fix Version (no more Epics) via the Jira Cloud REST API.
* **🔗 Azure DevOps PR Match**: Grabs active PRs from the last 6 months and links them to Jira keys found in titles, descriptions, or branch names.
* **⚡ Fast & Cached**: In-memory cache stores PRs for **30 mins**, so only one API call per half-hour.
* **🎉 Confluence‑Ready**: Renders a Bootstrap table you can paste directly into Confluence pages or release docs.

---

## 🔑 Setup & Configuration

### 1. Requirements

* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 2. Credentials

#### 🔒 Azure DevOps PAT

1. Sign in to Azure DevOps → **User Settings** → **Personal Access Tokens**.
2. Click **+ New Token**, give it a name, expire in 90 days, and grant **Code (Read)** scope.
3. Copy the token safely!

👉 [Microsoft Docs: Use PATs](https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate)

#### 🗝️ Jira API Token (Basic Auth)

1. Go to your Atlassian account → **Security** → **API tokens**.
2. Click **Create API token**, name it, and copy the result.

👉 [Atlassian Docs: Manage API Tokens](https://support.atlassian.com/atlassian-account/docs/manage-api-tokens-for-your-atlassian-account/)

### 3. appsettings.json Example

```json
{
  "Jira": {
    "Domain": "your-domain.atlassian.net",
    "ProjectKey": "YOUR_JIRA_PROJECT_KEY", // i.e if your ticket is AB-123456 then you project key would be "AB"
    "AuthKey": "user@example.com:YOUR_JIRA_API_TOKEN"
  },
  "AzureDevOps": {
    "Organization": "YOUR_AZURE_DEVOPS_ORG",
    "Project": "YOUR_AZURE_DEVOPS_PROJECT_NAME", //i.e Hello world
    "PersonalAccessToken": "YOUR_AZURE_DEVOPS_PAT",
    "ApiVersion": "7.1",
    "PageSize": 1000
  }
}
```

* **AuthKey** is `<your-email>:<Jira-API-token>` (Basic Auth). 

---

## 📦 Download the Code

Grab the source and contribute on GitHub:

[![GitHub Repo](https://img.shields.io/badge/GitHub-Download-blue?logo=github)](https://github.com/hummad-hassan/jira-pr-table-generator)

Feel free to fork, star ★, and send pull requests!

---

## 🚀 How to Run

```bash
dotnet restore
dotnet run
```

Open ➡️ `https://localhost:5001/JiraFix`, enter your **Fix Version**, click **Fetch Tickets**. Ready to rock! 🤘

---

## 📊 Sample Output

Below is how the table renders in **Markdown**—just copy & paste directly into your README or Confluence page:

```markdown
| Ticket #  | Title                                 | Pull Requests                                                                                                               |
|-----------|---------------------------------------|----------------------------------------------------------------------------------------------------------------------------|
| SH-19840  | Fix null reference at agreements page | [140953](https://dev.azure.com/Acme/hello%20world/_git/my-azure-debops-git-repo-name/pullrequest/140953)                                 |
| SH-19845  | Update login workflow                 | [141020](https://dev.azure.com/Acme/hello%20world/_git/my-azure-debops-git-repo-name/pullrequest/141020), [141022](https://dev.azure.com/Acme/hello%20world/_git/my-azure-debops-git-repo-name/pullrequest/141022) |
```