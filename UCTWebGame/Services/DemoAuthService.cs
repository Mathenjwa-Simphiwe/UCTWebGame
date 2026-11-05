using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using UCTWebGame.DTOs;
using UCTWebGame.Models;

namespace UCTWebGame.Services;

/// <summary>
/// Development-only accounts and registration that work without MongoDB.
/// Accounts exist only for the lifetime of the API process.
/// </summary>
public sealed class DemoAuthService : IAuthService
{
    public const string CommanderEmail = "commander@frontline.demo";
    public const string CommanderPassword = "FrontlineDemo1!";
    public const string ScoutEmail = "scout@frontline.demo";
    public const string ScoutPassword = "FrontlineDemo2!";

    private readonly ConcurrentDictionary<string, User> _users = new(StringComparer.OrdinalIgnoreCase);
    private readonly JwtSettings _jwt;

    public DemoAuthService(IOptions<JwtSettings> jwtOptions)
    {
        _jwt = jwtOptions.Value;
        ValidateJwtSettings(_jwt);

        AddSeedUser("Commander", CommanderEmail, CommanderPassword);
        AddSeedUser("Scout", ScoutEmail, ScoutPassword);
    }

    public Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var email = NormalizeEmail(request.Email);
        var user = new User
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Username = request.Username.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 10),
            CreatedAt = DateTime.UtcNow
        };

        if (!_users.TryAdd(email, user))
            throw new EmailAlreadyRegisteredException();

        return Task.FromResult(CreateAuthResponse(user));
    }

    public Task<AuthResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var email = NormalizeEmail(request.Email);
        if (!_users.TryGetValue(email, out var user) ||
            !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Task.FromResult<AuthResponse?>(null);
        }

        return Task.FromResult<AuthResponse?>(CreateAuthResponse(user));
    }

    private void AddSeedUser(string username, string email, string password)
    {
        _users[email] = new User
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 10),
            CreatedAt = DateTime.UtcNow
        };
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id!),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_jwt.ExpiresMinutes),
            signingCredentials: credentials);

        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            new AuthUser(user.Id!, user.Username, user.Email));
    }

    private static void ValidateJwtSettings(JwtSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Secret) ||
            Encoding.UTF8.GetByteCount(settings.Secret) < 32)
        {
            throw new InvalidOperationException("Jwt:Secret must contain at least 32 UTF-8 bytes.");
        }

        if (string.IsNullOrWhiteSpace(settings.Issuer) ||
            string.IsNullOrWhiteSpace(settings.Audience) ||
            settings.ExpiresMinutes < 1)
        {
            throw new InvalidOperationException("JWT issuer, audience, and positive expiry must be configured.");
        }
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
