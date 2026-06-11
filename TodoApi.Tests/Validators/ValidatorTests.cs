using FluentAssertions;
using TodoApi.DTOs;
using TodoApi.Validators;
using Xunit;

namespace TodoApi.Tests.Validators;

public class ValidatorTests
{
    private readonly RegisterRequestDtoValidator _registerValidator = new();
    private readonly LoginRequestDtoValidator _loginValidator = new();
    private readonly CreateTodoDtoValidator _createTodoValidator = new();
    private readonly UpdateTodoDtoValidator _updateTodoValidator = new();

    [Fact]
    public void RegisterValidator_ValidData_ShouldPass()
    {
        // Arrange
        var model = new RegisterRequestDto
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            Password = "password123"
        };

        // Act
        var result = _registerValidator.Validate(model);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "john.doe@example.com", "password123", "Name is required.")]
    [InlineData("John Doe", "", "password123", "Email is required.")]
    [InlineData("John Doe", "invalid-email", "password123", "A valid email address is required.")]
    [InlineData("John Doe", "john.doe@example.com", "", "Password is required.")]
    [InlineData("John Doe", "john.doe@example.com", "12345", "Password must be at least 6 characters long.")]
    public void RegisterValidator_InvalidData_ShouldFailWithExpectedMessage(
        string name, string email, string password, string expectedErrorMessage)
    {
        // Arrange
        var model = new RegisterRequestDto { Name = name, Email = email, Password = password };

        // Act
        var result = _registerValidator.Validate(model);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == expectedErrorMessage);
    }

    [Fact]
    public void RegisterValidator_NameTooLong_ShouldFail()
    {
        // Arrange
        var model = new RegisterRequestDto
        {
            Name = new string('A', 101),
            Email = "john.doe@example.com",
            Password = "password123"
        };

        // Act
        var result = _registerValidator.Validate(model);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Name cannot exceed 100 characters.");
    }

    [Fact]
    public void RegisterValidator_EmailTooLong_ShouldFail()
    {
        // Arrange
        var model = new RegisterRequestDto
        {
            Name = "John Doe",
            Email = new string('A', 140) + "@example.com",
            Password = "password123"
        };

        // Act
        var result = _registerValidator.Validate(model);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Email cannot exceed 150 characters.");
    }

    [Fact]
    public void RegisterValidator_PasswordTooLong_ShouldFail()
    {
        // Arrange
        var model = new RegisterRequestDto
        {
            Name = "John Doe",
            Email = "john.doe@example.com",
            Password = new string('P', 101)
        };

        // Act
        var result = _registerValidator.Validate(model);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Password cannot exceed 100 characters.");
    }

    [Fact]
    public void LoginValidator_ValidData_ShouldPass()
    {
        // Arrange
        var model = new LoginRequestDto
        {
            Email = "john.doe@example.com",
            Password = "password123"
        };

        // Act
        var result = _loginValidator.Validate(model);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "password123", "Email is required.")]
    [InlineData("invalid-email", "password123", "A valid email address is required.")]
    [InlineData("john.doe@example.com", "", "Password is required.")]
    public void LoginValidator_InvalidData_ShouldFailWithExpectedMessage(
        string email, string password, string expectedErrorMessage)
    {
        // Arrange
        var model = new LoginRequestDto { Email = email, Password = password };

        // Act
        var result = _loginValidator.Validate(model);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == expectedErrorMessage);
    }

    [Fact]
    public void CreateTodoValidator_ValidData_ShouldPass()
    {
        // Arrange
        var model = new CreateTodoDto
        {
            Title = "Finish Homework",
            Description = "Math and Science projects"
        };

        // Act
        var result = _createTodoValidator.Validate(model);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateTodoValidator_EmptyTitle_ShouldFail()
    {
        // Arrange
        var model = new CreateTodoDto { Title = "", Description = "Desc" };

        // Act
        var result = _createTodoValidator.Validate(model);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Title is required.");
    }

    [Fact]
    public void CreateTodoValidator_TitleTooLong_ShouldFail()
    {
        // Arrange
        var model = new CreateTodoDto
        {
            Title = new string('A', 201),
            Description = "Desc"
        };

        // Act
        var result = _createTodoValidator.Validate(model);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Title cannot exceed 200 characters.");
    }

    [Fact]
    public void CreateTodoValidator_DescriptionTooLong_ShouldFail()
    {
        // Arrange
        var model = new CreateTodoDto
        {
            Title = "Valid Title",
            Description = new string('A', 1001)
        };

        // Act
        var result = _createTodoValidator.Validate(model);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Description cannot exceed 1000 characters.");
    }

    [Fact]
    public void UpdateTodoValidator_ValidData_ShouldPass()
    {
        // Arrange
        var model = new UpdateTodoDto
        {
            Title = "Updated Title",
            Description = "Updated description",
            IsCompleted = true
        };

        // Act
        var result = _updateTodoValidator.Validate(model);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateTodoValidator_EmptyTitle_ShouldFail()
    {
        // Arrange
        var model = new UpdateTodoDto { Title = "", Description = "Desc", IsCompleted = false };

        // Act
        var result = _updateTodoValidator.Validate(model);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Title is required.");
    }

    [Fact]
    public void UpdateTodoValidator_TitleTooLong_ShouldFail()
    {
        // Arrange
        var model = new UpdateTodoDto { Title = new string('A', 201), Description = "Desc", IsCompleted = false };

        // Act
        var result = _updateTodoValidator.Validate(model);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Title cannot exceed 200 characters.");
    }

    [Fact]
    public void UpdateTodoValidator_DescriptionTooLong_ShouldFail()
    {
        // Arrange
        var model = new UpdateTodoDto { Title = "Valid Title", Description = new string('A', 1001), IsCompleted = false };

        // Act
        var result = _updateTodoValidator.Validate(model);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Description cannot exceed 1000 characters.");
    }
}
