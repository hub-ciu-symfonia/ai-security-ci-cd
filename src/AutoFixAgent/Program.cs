using AutoFixAgent;

string GetRequiredEnv(string name) =>
    Environment.GetEnvironmentVariable(name)
    ?? throw new InvalidOperationException($"Wymagana zmienna srodowiskowa {name} nie jest ustawiona.");

var issueNumberRaw = GetRequiredEnv("ISSUE_NUMBER");
if (!int.TryParse(issueNumberRaw, out var issueNumber))
{
    throw new InvalidOperationException($"Zmienna ISSUE_NUMBER ('{issueNumberRaw}') nie jest poprawna liczba calkowita.");
}
var targetFilePath = GetRequiredEnv("TARGET_FILE_PATH");
var baseBranch = Environment.GetEnvironmentVariable("BASE_BRANCH") ?? "main";
var modelId = GetRequiredEnv("BEDROCK_MODEL_ID");
var githubToken = GetRequiredEnv("GITHUB_TOKEN");
var repoOwner = GetRequiredEnv("GITHUB_REPOSITORY_OWNER");
var repoName = GetRequiredEnv("GITHUB_REPOSITORY_NAME");

IModelClient model = new BedrockModelClient(modelId);
IRepoClient repo = new OctokitRepoClient(repoOwner, repoName, githubToken);

var prNumber = await AutoFixWorkflow.RunAsync(model, repo, issueNumber, targetFilePath, baseBranch);

Console.WriteLine($"Utworzono pull request #{prNumber}");
