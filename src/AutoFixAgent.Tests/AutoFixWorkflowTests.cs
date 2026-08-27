using AutoFixAgent;
using Xunit;

namespace AutoFixAgent.Tests;

public class FakeRepoClient : IRepoClient
{
    public string IssueBody { get; set; } = "";
    public string ExistingFileContent { get; set; } = "";

    public List<(string NewBranch, string FromBranch)> CreatedBranches { get; } = new();
    public List<(string Branch, string Path, string Content, string Message)> Commits { get; } = new();
    public List<(string Head, string Base, string Title, string Body, IReadOnlyList<string> Labels)> PullRequests { get; } = new();

    public Task<string> GetIssueBodyAsync(int issueNumber) => Task.FromResult(IssueBody);

    public Task<string> GetFileContentAsync(string path, string branch) => Task.FromResult(ExistingFileContent);

    public Task CreateBranchAsync(string newBranch, string fromBranch)
    {
        CreatedBranches.Add((newBranch, fromBranch));
        return Task.CompletedTask;
    }

    public Task CommitFileAsync(string branch, string path, string content, string commitMessage)
    {
        Commits.Add((branch, path, content, commitMessage));
        return Task.CompletedTask;
    }

    public Task<int> CreatePullRequestAsync(string headBranch, string baseBranch, string title, string body, IReadOnlyList<string> labels)
    {
        PullRequests.Add((headBranch, baseBranch, title, body, labels));
        return Task.FromResult(PullRequests.Count);
    }
}

public class AutoFixWorkflowTests
{
    [Fact]
    public async Task ReadsTheIssueAndTheTargetFileBeforeGeneratingAFix()
    {
        var repo = new FakeRepoClient { IssueBody = "issue text", ExistingFileContent = "old content" };
        var model = new FakeModelClient { ResponseToReturn = "new content" };

        await AutoFixWorkflow.RunAsync(model, repo, issueNumber: 42, targetFilePath: "src/VictimApi/Program.cs", baseBranch: "main");

        Assert.Contains("issue text", model.LastUserPrompt);
        Assert.Contains("old content", model.LastUserPrompt);
    }

    [Fact]
    public async Task CreatesABranchCommitsTheFixAndOpensAPullRequestLabeledAiGenerated()
    {
        var repo = new FakeRepoClient { IssueBody = "issue text", ExistingFileContent = "old content" };
        var model = new FakeModelClient { ResponseToReturn = "new content" };

        var prNumber = await AutoFixWorkflow.RunAsync(model, repo, issueNumber: 42, targetFilePath: "src/VictimApi/Program.cs", baseBranch: "main");

        Assert.Single(repo.CreatedBranches);
        Assert.Equal("main", repo.CreatedBranches[0].FromBranch);

        Assert.Single(repo.Commits);
        Assert.Equal("src/VictimApi/Program.cs", repo.Commits[0].Path);
        Assert.Equal("new content", repo.Commits[0].Content);
        Assert.Equal(repo.CreatedBranches[0].NewBranch, repo.Commits[0].Branch);

        Assert.Single(repo.PullRequests);
        Assert.Equal("main", repo.PullRequests[0].Base);
        Assert.Contains("ai-generated", repo.PullRequests[0].Labels);
        Assert.Equal(1, prNumber);
    }
}
