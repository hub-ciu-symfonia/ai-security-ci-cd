using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;

namespace AutoFixAgent;

public class BedrockModelClient : IModelClient
{
    private readonly AmazonBedrockRuntimeClient _client;
    private readonly string _modelId;

    public BedrockModelClient(string modelId)
    {
        _modelId = modelId;
        _client = new AmazonBedrockRuntimeClient();
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt)
    {
        var request = new ConverseRequest
        {
            ModelId = _modelId,
            System = new List<SystemContentBlock> { new SystemContentBlock { Text = systemPrompt } },
            Messages = new List<Message>
            {
                new Message
                {
                    Role = ConversationRole.User,
                    Content = new List<ContentBlock> { new ContentBlock { Text = userPrompt } },
                },
            },
        };

        var response = await _client.ConverseAsync(request);

        return response.Output.Message.Content.FirstOrDefault(c => c.Text != null)?.Text ?? "";
    }
}
