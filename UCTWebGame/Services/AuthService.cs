using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using UCTWebGame.DTOs;
using UCTWebGame.Models;

namespace UCTWebGame.Services;

public sealed class AuthService : IAuthService
{
    private readonly MongoDbService _mongo;
    private readonly JwtSettings _jwt;

    public AuthService(MongoDbService mongo, IOptions<JwtSettings> jwtOptions)
    {
        _mongo = mongo;
        _jwt = jwtOptions.Value;

        if (string.IsNullOrWhiteSpace(_jwt.Secret) ||
            Encoding.UTF8.GetByteCount(_jwt.Secret) < 32)
        {
            throw new InvalidOperationException("Jwt:Secret must contain at least 32 UTF-8 bytes.");
        }

        if (string.IsNullOrWhiteSpace(_jwt.Issuer) ||
            string.IsNullOrWhiteSpace(_jwt.Audience) ||
            _jwt.ExpiresMinutes < 1)
        {
            throw new InvalidOperationException("JWT issuer, audience, and positive expiry must be configured.");
        }
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var existingUser = await _mongo.Users
            .Find(user => user.Email == email)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingUser is not null)
            throw new EmailAlreadyRegisteredException();

        var user = new User
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Username = request.Username.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, 12),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _mongo.Users.InsertOneAsync(user, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Code == 11000)
        {
            // The unique email index also protects against simultaneous registrations.
            throw new EmailAlreadyRegisteredException();
        }

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _mongo.Users
            .Find(candidate => candidate.Email == email)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        return CreateAuthResponse(user);
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        if (string.IsNullOrWhiteSpace(user.Id))
            throw new InvalidOperationException("The user record has no MongoDB ID.");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
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

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var publicUser = new AuthUser(user.Id, user.Username, user.Email);

        return new AuthResponse(accessToken, publicUser);
    }

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();
}
