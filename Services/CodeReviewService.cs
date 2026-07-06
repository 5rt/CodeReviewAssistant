using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;
using CodeReviewAssistant.Models;

namespace CodeReviewAssistant.Services;

public class CodeReviewService
{
    private readonly ChatClient _chatClient;

    public CodeReviewService(IConfiguration config)
    {
        var endpoint = config["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("Set AzureOpenAI:Endpoint");
        var deployment = config["AzureOpenAI:Deployment"]
            ?? throw new InvalidOperationException("Set AzureOpenAI:Deployment");

        // Keyless auth — uses your `az login` identity, no key stored anywhere
        var azureClient = new AzureOpenAIClient(new Uri(endpoint), new DefaultAzureCredential());
        _chatClient = azureClient.GetChatClient(deployment);
    }

    public async Task<List<ReviewComment>> ReviewAsync(string diff, string focus)
    {
        var systemPrompt = """
            You are a senior code reviewer. Review the code or diff the user gives you.
            Return ONLY a JSON object shaped like:
            { "issues": [ { "severity": "High|Medium|Low", "category": "bug|security|style",
              "line": 12, "issue": "short description", "suggestedFix": "short fix" } ] }
            If there are no issues, return { "issues": [] }.
            Do not write anything outside the JSON.
            """;

        if (focus != "everything")
            systemPrompt += $"\nOnly report {focus} issues.";

        ChatCompletion completion = await _chatClient.CompleteChatAsync(
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(diff)
        ]);

        var raw = completion.Content[0].Text;
        return ReviewParser.Parse(raw);
    }
}