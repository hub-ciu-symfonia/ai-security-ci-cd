using Octokit;

namespace AutoFixAgent;

public class OctokitRepoClient : IRepoClient
{
    private readonly GitHubClient _client;
    private readonly string _owner;
    private readonly string _repo;

    public OctokitRepoClient(string owner, string repo, string token)
    {
        _owner = owner;
        _repo = repo;
        _client = new GitHubClient(new ProductHeaderValue("auto-fix-agent"))
        {
            Credentials = new Credentials(token),
        };
    }

    public async Task<string> GetIssueBodyAsync(int issueNumber)
    {
        var issue = await _client.Issue.Get(_owner, _repo, issueNumber);
        return issue.Body ?? "";
    }

    public async Task<string> GetFileContentAsync(string path, string branch)
    {
        var contents = await _client.Repository.Content.GetAllContentsByRef(_owner, _repo, path, branch);
        return contents[0].Content;
    }

    public async Task CreateBranchAsync(string newBranch, string fromBranch)
    {
        var baseRef = await _client.Git.Reference.Get(_owner, _repo, $"heads/{fromBranch}");
        await _client.Git.Reference.Create(_owner, _repo, new NewReference($"refs/heads/{newBranch}", baseRef.Object.Sha));
    }

    public async Task CommitFileAsync(string branch, string path, string content, string commitMessage)
    {
        var existing = await _client.Repository.Content.GetAllContentsByRef(_owner, _repo, path, branch);
        var updateRequest = new UpdateFileRequest(commitMessage, content, existing[0].Sha, branch);
        await _client.Repository.Content.UpdateFile(_owner, _repo, path, updateRequest);
    }

    public async Task<int> CreatePullRequestAsync(string headBranch, string baseBranch, string title, string body, IReadOnlyList<string> labels)
    {
        var newPr = new NewPullRequest(title, headBranch, baseBranch) { Body = body };
        var pr = await _client.PullRequest.Create(_owner, _repo, newPr);

        if (labels.Count > 0)
        {
            await _client.Issue.Labels.AddToIssue(_owner, _repo, pr.Number, labels.ToArray());
        }

        return pr.Number;
    }
}
