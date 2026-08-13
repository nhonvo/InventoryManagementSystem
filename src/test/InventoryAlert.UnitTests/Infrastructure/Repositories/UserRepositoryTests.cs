using InventoryAlert.Domain.Entities.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres;
using InventoryAlert.Infrastructure.Persistence.Postgres.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InventoryAlert.UnitTests.Infrastructure.Repositories;

public class UserRepositoryTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetByUsernameAsync_ReturnsUser_WhenUserExists()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new UserRepository(context);
        var user = new User { Id = Guid.NewGuid(), Username = "testuser", Email = "test@example.com", PasswordHash = "hash" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByUsernameAsync("testuser");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("testuser", result.Username);
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public async Task GetByUsernameAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new UserRepository(context);

        // Act
        var result = await repo.GetByUsernameAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenUserExists()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new UserRepository(context);
        context.Users.Add(new User { Id = Guid.NewGuid(), Username = "existinguser", Email = "e@example.com", PasswordHash = "hash" });
        await context.SaveChangesAsync();

        // Act
        var result = await repo.ExistsAsync("existinguser");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenUserDoesNotExist()
    {
        // Arrange
        using var context = CreateDbContext();
        var repo = new UserRepository(context);

        // Act
        var result = await repo.ExistsAsync("ghostuser");

        // Assert
        Assert.False(result);
    }
}
