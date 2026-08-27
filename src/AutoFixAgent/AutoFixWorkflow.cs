namespace AutoFixAgent;

public static class AutoFixWorkflow
{
    public static async Task<int> RunAsync(IModelClient model, IRepoClient repo, int issueNumber, string targetFilePath, string baseBranch)
    {
        var issueBody = await repo.GetIssueBodyAsync(issueNumber);
        var currentFileContent = await repo.GetFileContentAsync(targetFilePath, baseBranch);

        var fixedContent = await PatchGenerator.GenerateFixedFileAsync(model, issueBody, currentFileContent);

        var branchName = $"auto-fix/issue-{issueNumber}";
        await repo.CreateBranchAsync(branchName, baseBranch);
        await repo.CommitFileAsync(branchName, targetFilePath, fixedContent, $"Auto-fix for issue #{issueNumber}");

        var prNumber = await repo.CreatePullRequestAsync(
            headBranch: branchName,
            baseBranch: baseBranch,
            title: $"Auto-fix: issue #{issueNumber}",
            body: $"Automatyczna poprawka wygenerowana przez AutoFixAgent dla issue #{issueNumber}.",
            labels: new[] { "ai-generated" });

        return prNumber;
    }
}
