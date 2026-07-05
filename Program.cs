using CodeReviewAssistant.Models;
using CodeReviewAssistant.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<CodeReviewService>();

var app = builder.Build();

app.MapPost("/api/review", async (ReviewRequest req, CodeReviewService service) =>
{
    if (string.IsNullOrWhiteSpace(req.Diff))
        return Results.BadRequest("No code provided.");

    try
    {
        var issues = await service.ReviewAsync(req.Diff, req.Focus);
        return Results.Ok(issues);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Review failed: {ex.Message}");
    }
});

app.Run();
