using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using TodoApi.DTOs;
using Xunit;

namespace TodoApi.Tests.Integration;

public class AuthApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthApiTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _factory.ResetDatabase();
    }

    [Fact]
    public async Task Register_ValidPayload_ShouldReturnTokenAndUserData()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Name = "Alice Tester",
            Email = "alice@example.com",
            Password = "securepassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        data.Should().NotBeNull();
        data!.Token.Should().NotBeNullOrWhiteSpace();
        data.UserId.Should().BeGreaterThan(0);
        data.Name.Should().Be("Alice Tester");
        data.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public async Task Register_DuplicateEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var request1 = new RegisterRequestDto { Name = "Alice Tester", Email = "duplicate@example.com", Password = "password" };
        var request2 = new RegisterRequestDto { Name = "Bob Tester", Email = "DUPLICATE@example.com", Password = "password" };

        await _client.PostAsJsonAsync("api/auth/register", request1);

        // Act
        var response = await _client.PostAsJsonAsync("api/auth/register", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("A user with this email already exists.");
    }

    [Fact]
    public async Task Register_InvalidPayload_ShouldReturnBadRequestValidationErrors()
    {
        // Arrange
        var request = new RegisterRequestDto { Name = "", Email = "invalid-email", Password = "123" };

        // Act
        var response = await _client.PostAsJsonAsync("api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Name is required.");
        body.Should().Contain("A valid email address is required.");
        body.Should().Contain("Password must be at least 6 characters long.");
    }

    [Fact]
    public async Task Login_ValidCredentials_ShouldReturnAuthResponse()
    {
        // Arrange
        var registerRequest = new RegisterRequestDto { Name = "Bob Tester", Email = "bob@example.com", Password = "correctpassword" };
        await _client.PostAsJsonAsync("api/auth/register", registerRequest);

        var loginRequest = new LoginRequestDto { Email = "BOB@example.com", Password = "correctpassword" };

        // Act
        var response = await _client.PostAsJsonAsync("api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        data.Should().NotBeNull();
        data!.Token.Should().NotBeNullOrWhiteSpace();
        data.Name.Should().Be("Bob Tester");
    }

    [Fact]
    public async Task Login_InvalidPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var registerRequest = new RegisterRequestDto { Name = "Bob Tester", Email = "bob2@example.com", Password = "correctpassword" };
        await _client.PostAsJsonAsync("api/auth/register", registerRequest);

        var loginRequest = new LoginRequestDto { Email = "bob2@example.com", Password = "wrongpassword" };

        // Act
        var response = await _client.PostAsJsonAsync("api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Invalid email or password.");
    }
}
