using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace AIAssistant.API.Services;

public class ChatService
{
    private readonly ChatClient? _chatClient;
    private readonly ILogger<ChatService> _logger;
    private readonly Dictionary<string, List<ChatMessage>> _conversationHistory = new();

    public ChatService(IConfiguration configuration, ILogger<ChatService> logger)
    {
        _logger = logger;
        
        try
        {
            var endpoint = configuration["AzureOpenAI:Endpoint"];
            var key = configuration["AzureOpenAI:Key"];
            var deploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4";

            if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(key))
            {
                var client = new AzureOpenAIClient(new Uri(endpoint), new Azure.AzureKeyCredential(key));
                _chatClient = client.GetChatClient(deploymentName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not initialize Azure OpenAI client");
        }
    }

    public async Task<string> GetChatResponseAsync(string userId, string message)
    {
        if (_chatClient == null)
        {
            return "AI Assistant is not configured. Please set up Azure OpenAI credentials.";
        }

        try
        {
            // Get or create conversation history
            if (!_conversationHistory.ContainsKey(userId))
            {
                _conversationHistory[userId] = new List<ChatMessage>
                {
                    new SystemChatMessage(
                        "You are a helpful AI assistant for an ecommerce platform. " +
                        "Help users with product recommendations, order inquiries, and general shopping questions.")
                };
            }

            var history = _conversationHistory[userId];
            history.Add(new UserChatMessage(message));

            // Get response
            var response = await _chatClient.CompleteChatAsync(history);
            var assistantMessage = response.Value.Content[0].Text;

            history.Add(new AssistantChatMessage(assistantMessage));

            return assistantMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat response");
            return "I'm sorry, I encountered an error processing your request.";
        }
    }

    public Task ClearConversationAsync(string userId)
    {
        _conversationHistory.Remove(userId);
        return Task.CompletedTask;
    }
}
