namespace AutoFixAgent;

public interface IModelClient
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt);
}
