using FluentValidation;
using FoodHub.Application.Common;
using FoodHub.Application.Dtos;
using FoodHub.Application.Interfaces;
using FoodHub.Domain.Entities;
using FoodHub.Domain.Enums;

namespace FoodHub.Application.Services;

public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IShipperRepository _shippers;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IShipperRepository shippers,
        IPasswordHasher hasher,
        IJwtTokenService jwt,
        IUnitOfWork uow,
        IClock clock,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _shippers = shippers;
        _hasher = hasher;
        _jwt = jwt;
        _uow = uow;
        _clock = clock;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        await RequestValidator.EnsureValidAsync(_registerValidator, request, ct);

        if (request.Role == Role.Admin)
        {
            throw new ForbiddenException("Admin accounts cannot be self registered.");
        }

        if (await _users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct) is not null)
        {
            throw new ConflictException("Email is already registered.");
        }

        var user = User.Create(request.Email, _hasher.Hash(request.Password), request.FullName, request.Role);
        await _users.AddAsync(user, ct);

        if (user.Role == Role.Shipper)
        {
            await _shippers.AddAsync(ShipperProfile.Create(user.Id, request.VehiclePlate!), ct);
        }

        var response = await IssueTokensAsync(user, ct);
        await _uow.SaveChangesAsync(ct);
        return response;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        await RequestValidator.EnsureValidAsync(_loginValidator, request, ct);

        var user = await _users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), ct);
        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAppException("Invalid email or password.");
        }

        var response = await IssueTokensAsync(user, ct);
        await _uow.SaveChangesAsync(ct);
        return response;
    }

    /// <summary>Exchanges a valid refresh token for a new pair and revokes the old one.</summary>
    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var stored = string.IsNullOrWhiteSpace(request.RefreshToken)
            ? null
            : await _refreshTokens.GetByHashAsync(_jwt.HashRefreshToken(request.RefreshToken), ct);

        if (stored is null || !stored.IsActive(_clock.UtcNow))
        {
            throw new UnauthorizedAppException("Refresh token is invalid or expired.");
        }

        var user = await _users.GetByIdAsync(stored.UserId, ct)
            ?? throw new UnauthorizedAppException("Refresh token is invalid or expired.");

        stored.Revoke(_clock.UtcNow);
        var response = await IssueTokensAsync(user, ct);
        await _uow.SaveChangesAsync(ct);
        return response;
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var access = _jwt.CreateAccessToken(user);
        var (plain, hash) = _jwt.CreateRefreshToken();
        await _refreshTokens.AddAsync(
            RefreshToken.Create(user.Id, hash, _clock.UtcNow.Add(_jwt.RefreshTokenLifetime)), ct);
        return new AuthResponse(user.Id, user.Role, access.Value, access.ExpiresAtUtc, plain);
    }
}
