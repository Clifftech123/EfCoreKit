using EfCoreKit.Sample.WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ── Services ───────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplicationServices();

// ── Middleware pipeline ────────────────────────────────────────────────────────
var app = builder.Build();

app.UseGlobalExceptionHandler();
await app.UseDevToolingAsync();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
