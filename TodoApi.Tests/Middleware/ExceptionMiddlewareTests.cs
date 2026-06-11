using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using TodoApi.Middleware;
using Xunit;

namespace TodoApi.Tests.Middleware;

public class ExceptionMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionMiddleware>> _mockLogger = new();

    private DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private async Task<string> ReadResponseBody(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(response.Body);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task InvokeAsync_NoException_ShouldCallNextAndKeepStatusCode200()
    {
        // Arrange
        var context = CreateHttpContext();
        RequestDelegate next = (ctx) =>
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.OK;
            return Task.CompletedTask;
        };

        var middleware = new ExceptionMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.OK);
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_ShouldReturn400WithErrors()
    {
        // Arrange
        var context = CreateHttpContext();
        var validationErrors = new List<ValidationFailure>
        {
            new("Title", "Title is required."),
            new("Description", "Description is too long.")
        };
        var valException = new ValidationException(validationErrors);

        RequestDelegate next = (ctx) => throw valException;
        var middleware = new ExceptionMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        context.Response.ContentType.Should().StartWith("application/json");

        var responseString = await ReadResponseBody(context.Response);
        var responseJson = JsonDocument.Parse(responseString);
        responseJson.RootElement.GetProperty("statusCode").GetInt32().Should().Be(400);
        responseJson.RootElement.GetProperty("message").GetString().Should().Be("Validation failed.");
        
        var errors = responseJson.RootElement.GetProperty("errors");
        errors.GetProperty("Title").GetString().Should().Be("Title is required.");
        errors.GetProperty("Description").GetString().Should().Be("Description is too long.");
    }

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_ShouldReturn404()
    {
        // Arrange
        var context = CreateHttpContext();
        RequestDelegate next = (ctx) => throw new KeyNotFoundException("Todo item not found.");
        var middleware = new ExceptionMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        var responseString = await ReadResponseBody(context.Response);
        var responseJson = JsonDocument.Parse(responseString);
        responseJson.RootElement.GetProperty("statusCode").GetInt32().Should().Be(404);
        responseJson.RootElement.GetProperty("message").GetString().Should().Be("Todo item not found.");
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_ShouldReturn401()
    {
        // Arrange
        var context = CreateHttpContext();
        RequestDelegate next = (ctx) => throw new UnauthorizedAccessException("Invalid token.");
        var middleware = new ExceptionMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
        var responseString = await ReadResponseBody(context.Response);
        var responseJson = JsonDocument.Parse(responseString);
        responseJson.RootElement.GetProperty("statusCode").GetInt32().Should().Be(401);
        responseJson.RootElement.GetProperty("message").GetString().Should().Be("Invalid token.");
    }

    [Fact]
    public async Task InvokeAsync_InvalidOperationException_ShouldReturn400()
    {
        // Arrange
        var context = CreateHttpContext();
        RequestDelegate next = (ctx) => throw new InvalidOperationException("Email already exists.");
        var middleware = new ExceptionMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        var responseString = await ReadResponseBody(context.Response);
        var responseJson = JsonDocument.Parse(responseString);
        responseJson.RootElement.GetProperty("statusCode").GetInt32().Should().Be(400);
        responseJson.RootElement.GetProperty("message").GetString().Should().Be("Email already exists.");
    }

    [Fact]
    public async Task InvokeAsync_GeneralException_ShouldReturn500()
    {
        // Arrange
        var context = CreateHttpContext();
        RequestDelegate next = (ctx) => throw new Exception("Unexpected error.");
        var middleware = new ExceptionMiddleware(next, _mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        var responseString = await ReadResponseBody(context.Response);
        var responseJson = JsonDocument.Parse(responseString);
        responseJson.RootElement.GetProperty("statusCode").GetInt32().Should().Be(500);
        responseJson.RootElement.GetProperty("message").GetString().Should().Be("An internal server error occurred.");
    }
}
