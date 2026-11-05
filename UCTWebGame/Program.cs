using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using UCTWebGame.Engine;
using UCTWebGame.Services;

var builder = WebApplication.CreateBuilder(args);

// Development runs with seeded accounts and in-memory games unless explicitly disabled.
// Production never enables demo mode, even if the configuration key is set.
var demoMode = builder.Environment.IsDevelopment() &&
               builder.Configuration.GetValue("DemoMode:Enabled", defaultValue: true);
if (demoMode && string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Secret"]))
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Jwt:Secret"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
    });
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings are missing.");
if (string.IsNullOrWhiteSpace(jwtSettings.Secret) ||
    Encoding.UTF8.GetByteCount(jwtSettings.Secret) < 32)
{
    throw new InvalidOperationException("Jwt:Secret must contain at least 32 UTF-8 bytes.");
}
if (string.IsNullOrWhiteSpace(jwtSettings.Issuer) ||
    string.IsNullOrWhiteSpace(jwtSettings.Audience) ||
    jwtSettings.ExpiresMinutes < 1)
{
    throw new InvalidOperationException("JWT issuer, audience, and positive expiry must be configured.");
}

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateLifetime = true,
            RequireExpirationTime = true,
            NameClaimType = JwtRegisteredClaimNames.UniqueName,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? new[] { "http://localhost:5173" };
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (demoMode)
        {
            // Vite may choose a different local port when its default is occupied.
            policy.SetIsOriginAllowed(origin =>
                Uri.TryCreate(origin, UriKind.Absolute, out var uri) && uri.IsLoopback);
        }
        else
        {
            policy.WithOrigins(allowedOrigins);
        }

        policy.AllowAnyHeader().AllowAnyMethod();
    });
});

if (demoMode)
{
    builder.Services.AddSingleton<IAuthService, DemoAuthService>();
    builder.Services.AddSingleton<IGameEventService, DemoGameEventService>();
}
else
{
    builder.Services.AddSingleton<MongoDbService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IGameEventService, GameEventService>();
}
builder.Services.AddSingleton<IDice, RandomDice>();
builder.Services.AddSingleton<GameEngine>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (demoMode)
{
    app.Logger.LogWarning("DEMO MODE is enabled: accounts and games are temporary and stored in memory.");
}
else
{
    await app.Services
        .GetRequiredService<MongoDbService>()
        .EnsureIndexesAsync();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
