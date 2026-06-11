using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using TodoApi.DTOs;
using Xunit;

namespace TodoApi.Tests.Integration;

public class TodoApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TodoApiTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _factory.ResetDatabase();
    }

    private async Task<string> AuthenticateUserAsync(string email, string password)
    {
        var registerRequest = new RegisterRequestDto
        {
            Name = "Integration Test User",
            Email = email,
            Password = password
        };
        var registerResponse = await _client.PostAsJsonAsync("api/auth/register", registerRequest);
        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        return authResult!.Token;
    }

    [Fact]
    public async Task GetPaged_NoToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("api/todo");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Todo_FullCrudFlow_ShouldSucceed()
    {
        // Arrange - Register & Login to get token using unique email
        var email = $"user_{Guid.NewGuid()}@example.com";
        var token = await AuthenticateUserAsync(email, "password123");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Create a Todo
        var createDto = new CreateTodoDto { Title = "Task A", Description = "Desc A" };
        var createResponse = await _client.PostAsJsonAsync("api/todo", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse.Headers.Location.Should().NotBeNull();

        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoDto>();
        createdTodo.Should().NotBeNull();
        createdTodo!.Id.Should().BeGreaterThan(0);
        createdTodo.Title.Should().Be("Task A");
        createdTodo.Description.Should().Be("Desc A");
        createdTodo.IsCompleted.Should().BeFalse();

        // 2. Read Todo by Id
        var getResponse = await _client.GetAsync($"api/todo/{createdTodo.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetchedTodo = await getResponse.Content.ReadFromJsonAsync<TodoDto>();
        fetchedTodo!.Title.Should().Be("Task A");

        // 3. Update Todo
        var updateDto = new UpdateTodoDto { Title = "Task A Updated", Description = "Desc A Updated", IsCompleted = true };
        var updateResponse = await _client.PutAsJsonAsync($"api/todo/{createdTodo.Id}", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedTodo = await updateResponse.Content.ReadFromJsonAsync<TodoDto>();
        updatedTodo!.Title.Should().Be("Task A Updated");
        updatedTodo.IsCompleted.Should().BeTrue();

        // 4. Delete Todo
        var deleteResponse = await _client.DeleteAsync($"api/todo/{createdTodo.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 5. Verify deleted Todo returns 404
        var getAfterDeleteResponse = await _client.GetAsync($"api/todo/{createdTodo.Id}");
        getAfterDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPaged_WithFilters_ShouldReturnFilteredResults()
    {
        // Arrange using unique email
        var email = $"filteruser_{Guid.NewGuid()}@example.com";
        var token = await AuthenticateUserAsync(email, "password123");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Add 3 todos
        await _client.PostAsJsonAsync("api/todo", new CreateTodoDto { Title = "Task Green", Description = "Apples" });
        await _client.PostAsJsonAsync("api/todo", new CreateTodoDto { Title = "Task Red", Description = "Cherries" });
        var response3 = await _client.PostAsJsonAsync("api/todo", new CreateTodoDto { Title = "Task Blue", Description = "Berries" });
        var todo3 = await response3.Content.ReadFromJsonAsync<TodoDto>();
        
        // Complete the 3rd todo
        await _client.PutAsJsonAsync($"api/todo/{todo3!.Id}", new UpdateTodoDto { Title = todo3.Title, Description = todo3.Description, IsCompleted = true });

        // Act & Assert 1: Search 'Green'
        var searchGreen = await _client.GetFromJsonAsync<PagedResultDto<TodoDto>>("api/todo?search=Green");
        searchGreen!.Items.Should().HaveCount(1);
        searchGreen.TotalCount.Should().Be(1);
        searchGreen.Items.Should().Contain(t => t.Title == "Task Green");

        // Act & Assert 2: Search 'berries' (case-insensitive test)
        var searchBerries = await _client.GetFromJsonAsync<PagedResultDto<TodoDto>>("api/todo?search=berries");
        searchBerries!.Items.Should().HaveCount(1);
        searchBerries.Items.Should().Contain(t => t.Title == "Task Blue");

        // Act & Assert 3: Filter completed = true
        var completedTodos = await _client.GetFromJsonAsync<PagedResultDto<TodoDto>>("api/todo?isCompleted=true");
        completedTodos!.Items.Should().HaveCount(1);
        completedTodos.Items.Should().Contain(t => t.Title == "Task Blue");

        // Act & Assert 4: Filter completed = false
        var activeTodos = await _client.GetFromJsonAsync<PagedResultDto<TodoDto>>("api/todo?isCompleted=false");
        activeTodos!.Items.Should().HaveCount(2);
        activeTodos.Items.Should().NotContain(t => t.Title == "Task Blue");
    }
}
