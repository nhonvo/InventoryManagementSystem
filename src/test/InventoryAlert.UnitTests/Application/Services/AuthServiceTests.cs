using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InventoryAlert.Api.Configuration;
using InventoryAlert.Api.Services;
using InventoryAlert.Domain.Common.Exceptions;
using InventoryAlert.Domain.DTOs;
using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace InventoryAlert.UnitTests.Application.Services;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<ILogger<AuthService>> _loggerMock = new();
    private readonly ApiSettings _settings;

    public AuthServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepoMock.Object);
        _unitOfWorkMock
            .Setup(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((action, _) => action());

        _settings = new ApiSettings
        {
            Jwt = new JwtSettings
            {
                Key = "SuperSecretAuthTestKeyForInventoryAlertUnitTests1234567890!",
                Issuer = "InventoryAlert",
                Audience = "InventoryAlertUI",
                ExpiryMinutes = 60,
                RefreshExpiryDays = 7
            }
        };
    }

    [Fact]
    public async Task RegisterAsync_CreatesUser_WhenValid()
    {
        // Arrange
        var service = new AuthService(_unitOfWorkMock.Object, _settings, _loggerMock.Object);
        var request = new RegisterRequest("newuser", "Password123!", "newuser@example.com");

        _userRepoMock.Setup(r => r.ExistsAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User u, CancellationToken _) => u);

        // Act
        var result = await service.RegisterAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Registration successful", result.Message);
        Assert.Equal("newuser", result.Username);
        _userRepoMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.ExecuteTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ThrowsUserFriendlyException_WhenUsernameTaken()
    {
        // Arrange
        var service = new AuthService(_unitOfWorkMock.Object, _settings, _loggerMock.Object);
        var request = new RegisterRequest("existinguser", "Password123!", "existing@example.com");

        _userRepoMock.Setup(r => r.ExistsAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UserFriendlyException>(() => service.RegisterAsync(request));
        Assert.Contains("already taken", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokenPair_WhenCredentialsValid()
    {
        // Arrange
        var service = new AuthService(_unitOfWorkMock.Object, _settings, _loggerMock.Object);
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "validuser",
            Email = "valid@example.com",
            PasswordHash = passwordHash,
            Role = "User"
        };

        _userRepoMock.Setup(r => r.GetByUsernameAsync("validuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var request = new LoginRequest("validuser", "CorrectPassword123!", RememberMe: false);

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Auth.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
    }

    [Fact]
    public async Task LoginAsync_ThrowsUserFriendlyException_WhenPasswordInvalid()
    {
        // Arrange
        var service = new AuthService(_unitOfWorkMock.Object, _settings, _loggerMock.Object);
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "validuser",
            Email = "valid@example.com",
            PasswordHash = passwordHash,
            Role = "User"
        };

        _userRepoMock.Setup(r => r.GetByUsernameAsync("validuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var request = new LoginRequest("validuser", "WrongPassword!", RememberMe: false);

        // Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(() => service.LoginAsync(request));
    }

    [Fact]
    public async Task LoginAsync_ThrowsUserFriendlyException_WhenUserNotFound()
    {
        // Arrange
        var service = new AuthService(_unitOfWorkMock.Object, _settings, _loggerMock.Object);
        _userRepoMock.Setup(r => r.GetByUsernameAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var request = new LoginRequest("nonexistent", "Password123!", RememberMe: false);

        // Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(() => service.LoginAsync(request));
    }

    [Fact]
    public async Task RefreshAsync_ThrowsUnauthorizedAccessException_WhenTokenMalformed()
    {
        // Arrange
        var service = new AuthService(_unitOfWorkMock.Object, _settings, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RefreshAsync("not.a.validtokenparts"));
    }

    [Fact]
    public async Task RefreshAsync_ReturnsNewTokenPair_WhenRefreshTokenValid()
    {
        // Arrange
        var service = new AuthService(_unitOfWorkMock.Object, _settings, _loggerMock.Object);
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = "refresheduser",
            Email = "refreshed@example.com",
            PasswordHash = "hash",
            Role = "User"
        };

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Generate a valid refresh token using the key
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("typ", "refresh")
        };
        var token = new JwtSecurityToken(_settings.Jwt.Issuer, _settings.Jwt.Audience, claims, expires: DateTime.UtcNow.AddDays(1), signingCredentials: creds);
        var refreshTokenString = new JwtSecurityTokenHandler().WriteToken(token);

        // Act
        var result = await service.RefreshAsync(refreshTokenString);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Auth.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
    }

    [Fact]
    public async Task LogoutAsync_CompletesSuccessfully()
    {
        // Arrange
        var service = new AuthService(_unitOfWorkMock.Object, _settings, _loggerMock.Object);

        // Act & Assert
        await service.LogoutAsync();
    }

    [Fact]
    public async Task RefreshAsync_ThrowsUnauthorizedAccessException_WhenUserNoLongerExists()
    {
        // Arrange
        var service = new AuthService(_unitOfWorkMock.Object, _settings, _loggerMock.Object);
        var userId = Guid.NewGuid();

        _userRepoMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("typ", "refresh")
        };
        var token = new JwtSecurityToken(_settings.Jwt.Issuer, _settings.Jwt.Audience, claims, expires: DateTime.UtcNow.AddDays(1), signingCredentials: creds);
        var refreshTokenString = new JwtSecurityTokenHandler().WriteToken(token);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RefreshAsync(refreshTokenString));
    }

    [Fact]
    public async Task RefreshAsync_ThrowsUnauthorizedAccessException_WhenTokenTypeNotRefresh()
    {
        // Arrange
        var service = new AuthService(_unitOfWorkMock.Object, _settings, _loggerMock.Object);
        var userId = Guid.NewGuid();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("typ", "access") // Wrong typ!
        };
        var token = new JwtSecurityToken(_settings.Jwt.Issuer, _settings.Jwt.Audience, claims, expires: DateTime.UtcNow.AddDays(1), signingCredentials: creds);
        var refreshTokenString = new JwtSecurityTokenHandler().WriteToken(token);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RefreshAsync(refreshTokenString));
    }
}
