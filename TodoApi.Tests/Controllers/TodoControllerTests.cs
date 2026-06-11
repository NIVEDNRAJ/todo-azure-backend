using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TodoApi.Controllers;
using TodoApi.DTOs;
using TodoApi.Services;
using Xunit;

namespace TodoApi.Tests.Controllers;

public class TodoControllerTests
{
    private readonly Mock<ITodoService> _mockTodoService = new();
    private readonly Mock<IValidator<CreateTodoDto>> _mockCreateValidator = new();
    private readonly Mock<IValidator<UpdateTodoDto>> _mockUpdateValidator = new();
    private readonly TodoController _controller;

    public TodoControllerTests()
    {
        _controller = new TodoController(
            _mockTodoService.Object,
            _mockCreateValidator.Object,
            _mockUpdateValidator.Object
        );
        SetupControllerUser("100"); // default user 100
    }

    private void SetupControllerUser(string? userIdClaimValue)
    {
        var claims = new List<Claim>();
        if (userIdClaimValue != null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userIdClaimValue));
        }

        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task GetCurrentUserId_MissingUserIdClaim_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        SetupControllerUser(null); // No claims

        // Act
        Func<Task> act = async () => await _controller.GetById(1);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User is not authenticated or user ID is invalid.");
    }

    [Fact]
    public async Task GetCurrentUserId_InvalidUserIdClaim_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        SetupControllerUser("not-an-integer");

        // Act
        Func<Task> act = async () => await _controller.GetById(1);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User is not authenticated or user ID is invalid.");
    }

    [Fact]
    public async Task GetPaged_ShouldReturnOkWithResult()
    {
        // Arrange
        var pagedResult = new PagedResultResult(); // Helper to make mock clean
        var items = new List<TodoDto> { new() { Id = 1, Title = "Todo" } };
        var serviceResult = new PagedResultDto<TodoDto>
        {
            Items = items,
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _mockTodoService.Setup(s => s.GetPagedAsync(100, "search", true, 1, 10))
            .ReturnsAsync(serviceResult);

        // Act
        var result = await _controller.GetPaged("search", true, 1, 10);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(serviceResult);
    }

    [Fact]
    public async Task GetById_ShouldReturnOkWithTodo()
    {
        // Arrange
        var todo = new TodoDto { Id = 1, Title = "Task 1" };
        _mockTodoService.Setup(s => s.GetByIdAsync(1, 100)).ReturnsAsync(todo);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(todo);
    }

    [Fact]
    public async Task Create_ValidationFails_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new CreateTodoDto();
        var validationFailures = new List<ValidationFailure> { new("Title", "Required") };
        _mockCreateValidator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act
        Func<Task> act = async () => await _controller.Create(dto);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        _mockTodoService.Verify(s => s.CreateAsync(It.IsAny<CreateTodoDto>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Create_ValidationPasses_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var dto = new CreateTodoDto { Title = "Task 1" };
        _mockCreateValidator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult()); // valid

        var createdTodo = new TodoDto { Id = 5, Title = "Task 1" };
        _mockTodoService.Setup(s => s.CreateAsync(dto, 100)).ReturnsAsync(createdTodo);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(TodoController.GetById));
        createdResult.RouteValues!["id"].Should().Be(5);
        createdResult.Value.Should().Be(createdTodo);
    }

    [Fact]
    public async Task Update_ValidationFails_ShouldThrowValidationException()
    {
        // Arrange
        var dto = new UpdateTodoDto();
        var validationFailures = new List<ValidationFailure> { new("Title", "Required") };
        _mockUpdateValidator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act
        Func<Task> act = async () => await _controller.Update(1, dto);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        _mockTodoService.Verify(s => s.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateTodoDto>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Update_ValidationPasses_ShouldReturnOkWithTodo()
    {
        // Arrange
        var dto = new UpdateTodoDto { Title = "Updated" };
        _mockUpdateValidator.Setup(v => v.ValidateAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult()); // valid

        var updatedTodo = new TodoDto { Id = 1, Title = "Updated" };
        _mockTodoService.Setup(s => s.UpdateAsync(1, dto, 100)).ReturnsAsync(updatedTodo);

        // Act
        var result = await _controller.Update(1, dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(updatedTodo);
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent()
    {
        // Arrange
        _mockTodoService.Setup(s => s.DeleteAsync(1, 100)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockTodoService.Verify(s => s.DeleteAsync(1, 100), Times.Once);
    }
}
internal class PagedResultResult {}
