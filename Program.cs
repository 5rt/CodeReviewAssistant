using CodeReviewAssistant.Models;
using CodeReviewAssistant.Services;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<CodeReviewService>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173", "https://kind-desert-061e58f00.7.azurestaticapps.net").AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddFixedWindowLimiter("review", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;   // 5 reviews per minute
        opt.QueueLimit = 0;    // no queueing — reject immediately
    });
});

var app = builder.Build();

app.UseCors();
app.UseRateLimiter();

app.MapPost("/api/review", async (ReviewRequest req, CodeReviewService service) =>
{
    if (string.IsNullOrWhiteSpace(req.Diff))
        return Results.BadRequest("No code provided.");

    if (req.Diff.Length > 100_000)
        return Results.BadRequest("Diff too large (max 100KB). Send a smaller snippet.");

    try
    {
        var issues = await service.ReviewAsync(req.Diff, req.Focus);
        return Results.Ok(issues);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Review failed: {ex.Message}");
    }
})
.RequireRateLimiting("review");

app.Run();