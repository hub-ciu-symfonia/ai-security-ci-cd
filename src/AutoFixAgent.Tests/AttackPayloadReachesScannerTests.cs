using Defense;
using Xunit;

namespace AutoFixAgent.Tests;

public class AttackPayloadReachesScannerTests
{
    [Fact]
    public void TheSimulatedAttackPayloadWouldBeCaughtByTheRealBackdoorScanner()
    {
        // Reuses the exact same backdoored content the agent pipeline test
        // (PatchGeneratorTests.ReproducesTheBackdoorWhenGivenThePoisonedIssueFixture) proves the
        // model produces, so this test closes the gap between "the agent complies with the
        // poisoned issue" and "the scanner catches what the agent produces" - one shared fixture,
        // not two independently hand-written string literals that could silently drift apart.
        var backdooredContent = BackdooredContentFixture.Build();

        // Simulate what a unified diff adding this content to Program.cs would look like.
        var diff = string.Join("\n", backdooredContent
            .Split('\n')
            .Select(line => "+" + line));

        var result = BackdoorScanner.Scan(diff);

        Assert.True(result.Suspicious);
    }
}
