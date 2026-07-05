using CodeReviewAssistant.Models;
using CodeReviewAssistant.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<CodeReviewService>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173", "https://kind-desert-061e58f00.azurestaticapps.net").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseCors();

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
