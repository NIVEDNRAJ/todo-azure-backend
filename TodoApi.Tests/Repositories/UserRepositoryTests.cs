using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Repositories;
using Xunit;

namespace TodoApi.Tests.Repositories;

public class UserRepositoryTests
{
    private AppDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ShouldAddUserToDatabase()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            var repo = new UserRepository(context);
            var user = new User
            {
                Name = "John Doe",
                Email = "John@Example.Com", // Mixed casing
                PasswordHash = "hashed"
            };

            // Act
            await repo.AddAsync(user);
            await repo.SaveChangesAsync();
        }

        // Assert
        using (var context = CreateDbContext(dbName))
        {
            var userInDb = await context.Users.FirstOrDefaultAsync(u => u.Name == "John Doe");
            userInDb.Should().NotBeNull();
            userInDb!.Email.Should().Be("john@example.com"); // Email should be lowercased
        }
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCorrectUser()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            context.Users.Add(new User { Id = 10, Name = "Alice", Email = "alice@example.com" });
            context.Users.Add(new User { Id = 20, Name = "Bob", Email = "bob@example.com" });
            await context.SaveChangesAsync();
        }

        // Act & Assert
        using (var context = CreateDbContext(dbName))
        {
            var repo = new UserRepository(context);
            var user = await repo.GetByIdAsync(10);
            user.Should().NotBeNull();
            user!.Name.Should().Be("Alice");

            var nonExistent = await repo.GetByIdAsync(99);
            nonExistent.Should().BeNull();
        }
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            context.Users.Add(new User { Id = 1, Name = "Alice", Email = "alice@example.com" });
            await context.SaveChangesAsync();
        }

        // Act & Assert
        using (var context = CreateDbContext(dbName))
        {
            var repo = new UserRepository(context);
            
            var user = await repo.GetByEmailAsync("ALICE@example.com");
            user.Should().NotBeNull();
            user!.Name.Should().Be("Alice");

            var nonExistent = await repo.GetByEmailAsync("bob@example.com");
            nonExistent.Should().BeNull();
        }
    }

    [Fact]
    public async Task EmailExistsAsync_ShouldReturnTrueIfEmailExists()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            context.Users.Add(new User { Id = 1, Name = "Alice", Email = "alice@example.com" });
            await context.SaveChangesAsync();
        }

        // Act & Assert
        using (var context = CreateDbContext(dbName))
        {
            var repo = new UserRepository(context);

            var exists = await repo.EmailExistsAsync("ALICE@EXAMPLE.COM");
            exists.Should().BeTrue();

            var doesNotExist = await repo.EmailExistsAsync("bob@example.com");
            doesNotExist.Should().BeFalse();
        }
    }
}
