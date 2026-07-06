using CodeReviewAssistant.Services;

namespace CodeReviewAssistant.Tests;

public class ReviewParserTests
{
    [Fact]
    public void Parses_valid_json_into_comments()
    {
        var raw = """{ "issues": [ { "severity": "High", "category": "security", "line": 2, "issue": "SQL injection", "suggestedFix": "Use parameters" } ] }""";

        var result = ReviewParser.Parse(raw);

        Assert.Single(result);
        Assert.Equal("High", result[0].Severity);
        Assert.Equal(2, result[0].Line);
    }

    [Fact]
    public void Parses_json_wrapped_in_markdown_fences()
    {
        var raw = "```json\n{ \"issues\": [ { \"severity\": \"Low\", \"category\": \"style\", \"line\": 1, \"issue\": \"x\", \"suggestedFix\": \"y\" } ] }\n```";

        var result = ReviewParser.Parse(raw);

        Assert.Single(result);
        Assert.Equal("Low", result[0].Severity);
    }

    [Fact]
    public void Returns_empty_list_for_garbage_input()
    {
        Assert.Empty(ReviewParser.Parse("the model rambled and returned no json at all"));
        Assert.Empty(ReviewParser.Parse(""));
        Assert.Empty(ReviewParser.Parse("{ this is broken json"));
    }

    [Fact]
    public void Property_matching_is_case_insensitive()
    {
        var raw = """{ "ISSUES": [ { "SEVERITY": "Medium", "CATEGORY": "bug", "LINE": 5, "ISSUE": "npe", "SUGGESTEDFIX": "check null" } ] }""";

        var result = ReviewParser.Parse(raw);

        Assert.Single(result);
        Assert.Equal("Medium", result[0].Severity);
    }

    [Fact]
    public void Empty_issues_array_means_clean_code()
    {
        Assert.Empty(ReviewParser.Parse("""{ "issues": [] }"""));
    }
}