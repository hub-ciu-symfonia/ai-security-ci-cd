namespace AutoFixAgent;

public interface IRepoClient
{
    Task<string> GetIssueBodyAsync(int issueNumber);
    Task<string> GetFileContentAsync(string path, string branch);
    Task CreateBranchAsync(string newBranch, string fromBranch);
    Task CommitFileAsync(string branch, string path, string content, string commitMessage);
    Task<int> CreatePullRequestAsync(string headBranch, string baseBranch, string title, string body, IReadOnlyList<string> labels);
}
