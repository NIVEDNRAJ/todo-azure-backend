using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TodoApi.Controllers;
using TodoApi.DTOs;
using TodoApi.Services;
using Xunit;

namespace TodoApi.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _mockAuthService = new();
    private readonly Mock<IValidator<RegisterRequestDto>> _mockRegisterValidator = new();
    private readonly Mock<IValidator<LoginRequestDto>> _mockLoginValidator = new();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _controller = new AuthController(
            _mockAuthService.Object,
            _mockRegisterValidator.Object,
            _mockLoginValidator.Object
        );
    }

    [Fact]
    public async Task Register_ValidationFails_ShouldThrowValidationException()
    {
        // Arrange
        var request = new RegisterRequestDto();
        var validationFailures = new List<ValidationFailure> { new("Email", "Email is required") };
        _mockRegisterValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act
        Func<Task> act = async () => await _controller.Register(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        _mockAuthService.Verify(s => s.RegisterAsync(It.IsAny<RegisterRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task Register_ValidationPasses_ShouldReturnOkWithResponse()
    {
        // Arrange
        var request = new RegisterRequestDto();
        _mockRegisterValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult()); // valid

        var response = new AuthResponseDto { Token = "token123" };
        _mockAuthService.Setup(s => s.RegisterAsync(request)).ReturnsAsync(response);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task Login_ValidationFails_ShouldThrowValidationException()
    {
        // Arrange
        var request = new LoginRequestDto();
        var validationFailures = new List<ValidationFailure> { new("Password", "Password is required") };
        _mockLoginValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(validationFailures));

        // Act
        Func<Task> act = async () => await _controller.Login(request);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        _mockAuthService.Verify(s => s.LoginAsync(It.IsAny<LoginRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task Login_ValidationPasses_ShouldReturnOkWithResponse()
    {
        // Arrange
        var request = new LoginRequestDto();
        _mockLoginValidator.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult()); // valid

        var response = new AuthResponseDto { Token = "token123" };
        _mockAuthService.Setup(s => s.LoginAsync(request)).ReturnsAsync(response);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }
}
