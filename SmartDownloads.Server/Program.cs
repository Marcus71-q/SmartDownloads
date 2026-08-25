using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using SmartDownloads.Server.Models;
using SmartDownloads.Server.Services;
using System.Threading.RateLimiting;

EnvFile.Load(Path.Combine(AppContext.BaseDirectory, ".env.local"));
EnvFile.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env.local"));
EnvFile.Load(Path.Combine(Directory.GetCurrentDirectory(), "SmartDownloads.Server", ".env.local"));
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 64 * 1024);
builder.Services.AddMemoryCache(options => options.SizeLimit = 10_000);
builder.Services.AddHttpClient<GeminiClassifier>(client => { client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/"); client.Timeout = TimeSpan.FromSeconds(60); });
builder.Services.AddSingleton<DeviceTokenService>();
builder.Services.AddSingleton<DailyQuotaService>();
builder.Services.AddSingleton<ClassificationService>();
builder.Services.AddSingleton<GlobalAiBudgetGuard>();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) => await context.HttpContext.Response.WriteAsJsonAsync(new ApiError("rate_limited", "Too many requests. Please try again later."), token);
    options.AddPolicy("register", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
    options.AddPolicy("classify", context => RateLimitPartition.GetTokenBucketLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new TokenBucketRateLimiterOptions { TokenLimit = 20, TokensPerPeriod = 10, ReplenishmentPeriod = TimeSpan.FromMinutes(1), AutoReplenishment = true, QueueLimit = 0 }));
});
var app = builder.Build();
var forwardedHeaders = new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto };
forwardedHeaders.KnownNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();
forwardedHeaders.ForwardLimit = 1;
app.UseForwardedHeaders(forwardedHeaders);
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff"; context.Response.Headers.XFrameOptions = "DENY"; context.Response.Headers.CacheControl = "no-store";
    context.Response.Headers.Append("Referrer-Policy", "no-referrer"); context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    await next();
});
app.UseRateLimiter();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "smart-downloads-classifier", utc = DateTimeOffset.UtcNow }));
app.MapPost("/v1/devices/register", (DeviceRegistration request, DeviceTokenService tokens) =>
{
    if (!Guid.TryParse(request.InstallationId, out var installationId)) return Results.BadRequest(new ApiError("invalid_installation_id", "InstallationId must be a UUID."));
    return Results.Ok(new DeviceRegistrationResponse(tokens.Create(installationId, TimeSpan.FromDays(30)), DateTimeOffset.UtcNow.AddDays(30)));
}).RequireRateLimiting("register");
app.MapPost("/v1/classify", async (ClassificationRequest request, HttpContext context, DeviceTokenService tokens, DailyQuotaService quotas, ClassificationService classifier, CancellationToken cancellationToken) =>
{
    var auth = context.Request.Headers.Authorization.ToString();
    if (!auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) || !tokens.TryRead(auth[7..].Trim(), out var deviceId)) return Results.Json(new ApiError("unauthorized", "A valid device token is required."), statusCode: 401);
    var validationError = request.Validate();
    if (validationError is not null) return Results.BadRequest(new ApiError("invalid_request", validationError));
    if (!quotas.TryConsume(deviceId, 10_000, out var remaining)) return Results.Json(new ApiError("abuse_limit_reached", "Automated abuse protection was triggered."), statusCode: 429);
    var result = await classifier.ClassifyAsync(request, deviceId, cancellationToken);
    context.Response.Headers.Append("X-Daily-Quota-Remaining", remaining.ToString());
    return Results.Ok(result);
}).RequireRateLimiting("classify");
app.Run();
public partial class Program;
