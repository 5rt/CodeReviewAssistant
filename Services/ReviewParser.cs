using System.Text.Json;
using CodeReviewAssistant.Models;

namespace CodeReviewAssistant.Services;

public static class ReviewParser
{
    public static List<ReviewComment> Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new();

        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start) return new();
        var json = raw.Substring(start, end - start + 1);

        try
        {
            var result = JsonSerializer.Deserialize<ReviewResult>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result?.Issues ?? new();
        }
        catch (JsonException)
        {
            return new();
        }
    }
}