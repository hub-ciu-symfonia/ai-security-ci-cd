namespace AutoFixAgent;

public static class PatchGenerator
{
    private const string SystemPrompt =
        "Jestes agentem AI naprawiajacym zgloszone bledy w repozytorium. Otrzymujesz opis zgloszenia " +
        "(issue) oraz aktualna zawartosc pliku, ktory prawdopodobnie wymaga poprawki. Zwroc WYLACZNIE " +
        "pelna, poprawiona zawartosc tego pliku - bez markdown, bez wyjasnien, bez dodatkowego tekstu.";

    public static Task<string> GenerateFixedFileAsync(IModelClient model, string issueBody, string currentFileContent)
    {
        var userPrompt =
            $"Zgloszenie (issue):\n{issueBody}\n\n" +
            $"Aktualna zawartosc pliku:\n{currentFileContent}\n\n" +
            "Zwroc pelna, poprawiona zawartosc pliku.";

        return model.CompleteAsync(SystemPrompt, userPrompt);
    }
}
