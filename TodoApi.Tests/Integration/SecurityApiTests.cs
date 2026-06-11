using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using TodoApi.DTOs;
using Xunit;

namespace TodoApi.Tests.Integration;

public class SecurityApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SecurityApiTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _factory.ResetDatabase();
    }

    private async Task<(string Token, int UserId)> RegisterAndGetTokenAsync(string email, string name = "User")
    {
        var registerRequest = new RegisterRequestDto { Name = name, Email = email, Password = "password123" };
        var registerResponse = await _client.PostAsJsonAsync("api/auth/register", registerRequest);
        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        return (authResult!.Token, authResult.UserId);
    }

    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("admin'--")]
    [InlineData("'; DROP TABLE Users;--")]
    [InlineData("UNION SELECT")]
    public async Task SqlInjection_AuthEndpoints_ShouldBeHandledSafelyOrRejected(string sqlPayload)
    {
        // 1. Test Login SQL Injection
        var loginRequest = new LoginRequestDto { Email = sqlPayload, Password = "password" };
        var loginResponse = await _client.PostAsJsonAsync("api/auth/login", loginRequest);
        
        // Assert: Login fails because the user doesn't exist, and the SQL executes safely without syntax exceptions.
        loginResponse.StatusCode.Should().Match(s => s == HttpStatusCode.Unauthorized || s == HttpStatusCode.BadRequest);

        // 2. Test Register SQL Injection
        var registerRequest = new RegisterRequestDto { Name = "Safe Name", Email = sqlPayload, Password = "password" };
        var registerResponse = await _client.PostAsJsonAsync("api/auth/register", registerRequest);
        
        // Assert: Register should reject or handle it safely (validation failed due to invalid email format).
        registerResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("admin'--")]
    [InlineData("'; DROP TABLE Users;--")]
    [InlineData("UNION SELECT")]
    public async Task SqlInjection_TodoEndpoints_ShouldBeStoredAndQueriedSafely(string sqlPayload)
    {
        // Arrange: Register a valid user with a unique email
        var email = $"sqluser_{Guid.NewGuid()}@example.com";
        var (token, _) = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Create a Todo with SQL Injection payload in the Title
        var createRequest = new CreateTodoDto { Title = sqlPayload, Description = "Safe description" };
        var createResponse = await _client.PostAsJsonAsync("api/todo", createRequest);
        
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoDto>();
        createdTodo!.Title.Should().Be(sqlPayload); // Stored exactly as text, showing parameterized queries worked safely

        // 2. Search for the SQL injection payload
        var searchResponse = await _client.GetAsync($"api/todo?search={WebUtility.UrlEncode(sqlPayload)}");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var searchResult = await searchResponse.Content.ReadFromJsonAsync<PagedResultDto<TodoDto>>();
        searchResult!.Items.Should().Contain(t => t.Id == createdTodo.Id);
    }

    [Fact]
    public async Task JwtTampering_AlteredSignature_ShouldBeRejected()
    {
        // Arrange
        var email = $"jwtuser_{Guid.NewGuid()}@example.com";
        var (token, _) = await RegisterAndGetTokenAsync(email);
        
        // Tamper the token by modifying the last few characters (the signature part)
        var parts = token.Split('.');
        var signaturePart = parts[2];
        var tamperedSignature = signaturePart[..^4] + "AAAA"; // alter last 4 chars
        var tamperedToken = $"{parts[0]}.{parts[1]}.{tamperedSignature}";

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tamperedToken);

        // Act
        var response = await _client.GetAsync("api/todo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task IDOR_AccessOrModifyAnotherUsersTodo_ShouldBeForbiddenOrNotFound()
    {
        // Arrange: Create User A and User B
        var emailA = $"userA_{Guid.NewGuid()}@example.com";
        var emailB = $"userB_{Guid.NewGuid()}@example.com";
        var (tokenA, userIdA) = await RegisterAndGetTokenAsync(emailA, "User A");
        var (tokenB, userIdB) = await RegisterAndGetTokenAsync(emailB, "User B");

        // User A creates a Todo
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        var createResponse = await _client.PostAsJsonAsync("api/todo", new CreateTodoDto { Title = "User A Private Task" });
        var todoA = await createResponse.Content.ReadFromJsonAsync<TodoDto>();

        // Act & Assert 1: User B tries to Read User A's Todo -> Should be unauthorized
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var getResponse = await _client.GetAsync($"api/todo/{todoA!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Act & Assert 2: User B tries to Update User A's Todo -> Should be unauthorized
        var updateResponse = await _client.PutAsJsonAsync($"api/todo/{todoA.Id}", new UpdateTodoDto { Title = "Hacked Title", IsCompleted = true });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Act & Assert 3: User B tries to Delete User A's Todo -> Should be unauthorized
        var deleteResponse = await _client.DeleteAsync($"api/todo/{todoA.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task OverpostingAttack_ShouldBePreventedByMapping()
    {
        // Arrange: Register user
        var email = $"overpost_{Guid.NewGuid()}@example.com";
        var (token, userId) = await RegisterAndGetTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Send payload
        var overpostPayload = new
        {
            Title = "Overpost Task",
            Description = "Description",
            Id = 9999, // Attempt to force ID
            UserId = 12345, // Attempt to assign to another user
            IsCompleted = true, // Attempt to force completed status on create (CreateTodoDto doesn't have IsCompleted)
            CreatedAt = System.DateTime.UtcNow.AddDays(-10) // Attempt to backdate
        };

        // Act
        var response = await _client.PostAsJsonAsync("api/todo", overpostPayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdTodo = await response.Content.ReadFromJsonAsync<TodoDto>();
        
        createdTodo!.Id.Should().NotBe(9999); // Id should be auto-assigned by database, not request
        createdTodo.IsCompleted.Should().BeFalse(); // CreateTodoDto mapping ignores IsCompleted, defaults to false
    }
}
