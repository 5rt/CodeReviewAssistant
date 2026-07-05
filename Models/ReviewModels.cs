namespace CodeReviewAssistant.Models;

// What the caller sends us
public record ReviewRequest(string Diff, string Focus = "everything");

// One review comment from the model
public record ReviewComment(
    string Severity,      // High / Medium / Low
    string Category,      // bug / security / style
    int? Line,
    string Issue,
    string SuggestedFix);

// Wrapper so the model can return { "issues": [ ... ] }
public record ReviewResult(List<ReviewComment> Issues);