using Xunit;

namespace AutoFixAgent.Tests;

public class PoisonedIssueContentTests
{
    private static string ReadFixture()
    {
        // AppContext.BaseDirectory is the test output dir; walk up to the repo-relative examples/ folder.
        var dir = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(dir, "examples")))
        {
            dir = Directory.GetParent(dir)!.FullName;
        }
        return File.ReadAllText(Path.Combine(dir, "examples", "poisoned-issue.md"));
    }

    [Fact]
    public void ContainsTheHiddenInjectionInstruction()
    {
        var content = ReadFixture();

        Assert.Contains("<!--", content);
        Assert.Contains("/debug/env", content);
        Assert.Contains("Environment", content);
    }
}
