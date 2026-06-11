using System;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using TodoApi.DTOs;
using TodoApi.Models;
using TodoApi.Repositories;
using TodoApi.Services;
using Xunit;

namespace TodoApi.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository = new();
    private readonly Mock<IJwtService> _mockJwtService = new();
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _authService = new AuthService(_mockUserRepository.Object, _mockJwtService.Object);
    }

    [Fact]
    public async Task RegisterAsync_EmailAlreadyExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Name = "John Doe",
            Email = "duplicate@example.com",
            Password = "password123"
        };
        _mockUserRepository.Setup(x => x.EmailExistsAsync(request.Email)).ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _authService.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A user with this email already exists.");
        _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
        _mockUserRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_ShouldCreateUserAndReturnToken()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Name = "John Doe",
            Email = "john@example.com",
            Password = "password123"
        };
        _mockUserRepository.Setup(x => x.EmailExistsAsync(request.Email)).ReturnsAsync(false);
        _mockUserRepository.Setup(x => x.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => u.Id = 123) // simulate DB auto-increment ID assignment
            .Returns(Task.CompletedTask);
        _mockJwtService.Setup(x => x.GenerateToken(It.IsAny<User>())).Returns("mock-jwt-token");

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(123);
        result.Name.Should().Be("John Doe");
        result.Email.Should().Be("john@example.com");
        result.Token.Should().Be("mock-jwt-token");

        _mockUserRepository.Verify(x => x.AddAsync(It.Is<User>(u => 
            u.Name == "John Doe" && 
            u.Email == "john@example.com" && 
            !string.IsNullOrWhiteSpace(u.PasswordHash) &&
            u.PasswordHash != "password123" // must be hashed
        )), Times.Once);
        _mockUserRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UserDoesNotExist_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequestDto { Email = "nonexistent@example.com", Password = "password" };
        _mockUserRepository.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync((User)null!);

        // Act
        Func<Task> act = async () => await _authService.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");
        _mockJwtService.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_PasswordIsIncorrect_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var request = new LoginRequestDto { Email = "john@example.com", Password = "wrongpassword" };
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("correctpassword");
        var existingUser = new User
        {
            Id = 1,
            Email = "john@example.com",
            PasswordHash = hashedPassword
        };
        _mockUserRepository.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync(existingUser);

        // Act
        Func<Task> act = async () => await _authService.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");
        _mockJwtService.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnAuthResponse()
    {
        // Arrange
        var request = new LoginRequestDto { Email = "john@example.com", Password = "correctpassword" };
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("correctpassword");
        var existingUser = new User
        {
            Id = 99,
            Name = "John",
            Email = "john@example.com",
            PasswordHash = hashedPassword
        };
        _mockUserRepository.Setup(x => x.GetByEmailAsync(request.Email)).ReturnsAsync(existingUser);
        _mockJwtService.Setup(x => x.GenerateToken(existingUser)).Returns("valid-jwt-token");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(99);
        result.Name.Should().Be("John");
        result.Email.Should().Be("john@example.com");
        result.Token.Should().Be("valid-jwt-token");
        _mockJwtService.Verify(x => x.GenerateToken(existingUser), Times.Once);
    }
}
