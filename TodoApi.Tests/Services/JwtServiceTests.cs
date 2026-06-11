using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using TodoApi.Models;
using TodoApi.Services;
using Xunit;

namespace TodoApi.Tests.Services;

public class JwtServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration = new();

    [Fact]
    public void GenerateToken_ValidConfiguration_ShouldGenerateValidToken()
    {
        // Arrange
        var user = new User
        {
            Id = 42,
            Name = "John Doe",
            Email = "john.doe@example.com"
        };

        _mockConfiguration.Setup(x => x["JWT_SECRET"]).Returns("SuperSecretKeyForTodoAppAuthJWTToken2026");
        _mockConfiguration.Setup(x => x["JWT_ISSUER"]).Returns("TodoApi");
        _mockConfiguration.Setup(x => x["JWT_AUDIENCE"]).Returns("TodoUi");
        _mockConfiguration.Setup(x => x["Jwt:ExpirationDays"]).Returns("7");

        var jwtService = new JwtService(_mockConfiguration.Object);

        // Act
        var token = jwtService.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be("TodoApi");
        jwtToken.Audiences.Should().Contain("TodoUi");

        // Verify claims
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "42");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "John Doe");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "john.doe@example.com");

        // Verify expiration is roughly 7 days from now
        jwtToken.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void GenerateToken_MissingSecretKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        var user = new User { Id = 1, Name = "A", Email = "a@a.com" };
        
        // Return null for secrets
        _mockConfiguration.Setup(x => x["JWT_SECRET"]).Returns((string)null!);
        _mockConfiguration.Setup(x => x["Jwt:Secret"]).Returns((string)null!);

        var jwtService = new JwtService(_mockConfiguration.Object);

        // Act
        Action act = () => jwtService.GenerateToken(user);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithMessage("*JWTSecret key is not configured*");
    }
}
