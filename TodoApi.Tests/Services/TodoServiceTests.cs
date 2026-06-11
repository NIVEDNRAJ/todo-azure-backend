using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using TodoApi.DTOs;
using TodoApi.Mapping;
using TodoApi.Models;
using TodoApi.Repositories;
using TodoApi.Services;
using Xunit;

namespace TodoApi.Tests.Services;

public class TodoServiceTests
{
    private readonly Mock<ITodoRepository> _mockTodoRepository = new();
    private readonly IMapper _mapper;
    private readonly TodoService _todoService;

    public TodoServiceTests()
    {
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = mapperConfig.CreateMapper();
        _todoService = new TodoService(_mockTodoRepository.Object, _mapper);
    }

    [Fact]
    public async Task GetByIdAsync_TodoNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _mockTodoRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Todo)null!);

        // Act
        Func<Task> act = async () => await _todoService.GetByIdAsync(1, 100);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Todo item not found.");
    }

    [Fact]
    public async Task GetByIdAsync_TodoOwnedByAnotherUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var todo = new Todo { Id = 1, UserId = 200, Title = "Title" };
        _mockTodoRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);

        // Act
        Func<Task> act = async () => await _todoService.GetByIdAsync(1, 100); // User 100 requests User 200's Todo

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You are not authorized to access this todo item.");
    }

    [Fact]
    public async Task GetByIdAsync_ValidOwner_ShouldReturnTodoDto()
    {
        // Arrange
        var todo = new Todo { Id = 1, UserId = 100, Title = "Finish homework", Description = "Math homework" };
        _mockTodoRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);

        // Act
        var result = await _todoService.GetByIdAsync(1, 100);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Title.Should().Be("Finish homework");
        result.Description.Should().Be("Math homework");
    }

    [Fact]
    public async Task GetPagedAsync_ShouldReturnMappedPagedResult()
    {
        // Arrange
        var items = new List<Todo>
        {
            new() { Id = 1, UserId = 100, Title = "Todo 1" },
            new() { Id = 2, UserId = 100, Title = "Todo 2" }
        };
        _mockTodoRepository.Setup(x => x.GetPagedAsync(100, "search", true, 1, 10))
            .ReturnsAsync((items, 2));

        // Act
        var result = await _todoService.GetPagedAsync(100, "search", true, 1, 10);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items.Should().HaveCount(2);
        result.Items.First().Title.Should().Be("Todo 1");
    }

    [Fact]
    public async Task CreateAsync_ShouldAddTodoAndSave()
    {
        // Arrange
        var dto = new CreateTodoDto { Title = "New task", Description = "Some description" };
        _mockTodoRepository.Setup(x => x.AddAsync(It.IsAny<Todo>()))
            .Callback<Todo>(t => t.Id = 5)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _todoService.CreateAsync(dto, 100);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(5);
        result.Title.Should().Be("New task");
        result.Description.Should().Be("Some description");
        result.IsCompleted.Should().BeFalse();

        _mockTodoRepository.Verify(x => x.AddAsync(It.Is<Todo>(t => 
            t.Title == "New task" && 
            t.UserId == 100 && 
            t.IsCompleted == false
        )), Times.Once);
        _mockTodoRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_TodoNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _mockTodoRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Todo)null!);

        // Act
        Func<Task> act = async () => await _todoService.UpdateAsync(1, new UpdateTodoDto(), 100);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Todo item not found.");
    }

    [Fact]
    public async Task UpdateAsync_UnauthorizedOwner_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var todo = new Todo { Id = 1, UserId = 200, Title = "Original Title" };
        _mockTodoRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);

        // Act
        Func<Task> act = async () => await _todoService.UpdateAsync(1, new UpdateTodoDto { Title = "New Title" }, 100);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You are not authorized to update this todo item.");
    }

    [Fact]
    public async Task UpdateAsync_ValidOwner_ShouldUpdateAndSave()
    {
        // Arrange
        var todo = new Todo { Id = 1, UserId = 100, Title = "Original Title", Description = "Original description", IsCompleted = false };
        _mockTodoRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);

        var updateDto = new UpdateTodoDto { Title = "Updated Title", Description = "Updated description", IsCompleted = true };

        // Act
        var result = await _todoService.UpdateAsync(1, updateDto, 100);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Updated Title");
        result.Description.Should().Be("Updated description");
        result.IsCompleted.Should().BeTrue();

        _mockTodoRepository.Verify(x => x.Update(todo), Times.Once);
        _mockTodoRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_TodoNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _mockTodoRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync((Todo)null!);

        // Act
        Func<Task> act = async () => await _todoService.DeleteAsync(1, 100);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Todo item not found.");
    }

    [Fact]
    public async Task DeleteAsync_UnauthorizedOwner_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var todo = new Todo { Id = 1, UserId = 200 };
        _mockTodoRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);

        // Act
        Func<Task> act = async () => await _todoService.DeleteAsync(1, 100);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("You are not authorized to delete this todo item.");
    }

    [Fact]
    public async Task DeleteAsync_ValidOwner_ShouldDeleteAndSave()
    {
        // Arrange
        var todo = new Todo { Id = 1, UserId = 100 };
        _mockTodoRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);

        // Act
        await _todoService.DeleteAsync(1, 100);

        // Assert
        _mockTodoRepository.Verify(x => x.Delete(todo), Times.Once);
        _mockTodoRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }
}
