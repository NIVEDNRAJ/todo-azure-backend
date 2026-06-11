using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Repositories;
using Xunit;

namespace TodoApi.Tests.Repositories;

public class TodoRepositoryTests
{
    private AppDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Add_Update_Delete_ShouldModifyDatabase()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var todo = new Todo
        {
            Id = 1,
            Title = "Task 1",
            Description = "Description 1",
            UserId = 10,
            IsCompleted = false
        };

        // Act & Assert (Add)
        using (var context = CreateDbContext(dbName))
        {
            var repo = new TodoRepository(context);
            await repo.AddAsync(todo);
            await repo.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var retrieved = await context.Todos.FindAsync(1);
            retrieved.Should().NotBeNull();
            retrieved!.Title.Should().Be("Task 1");
        }

        // Act & Assert (Update)
        using (var context = CreateDbContext(dbName))
        {
            var repo = new TodoRepository(context);
            todo.Title = "Updated Task 1";
            todo.IsCompleted = true;
            repo.Update(todo);
            await repo.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var retrieved = await context.Todos.FindAsync(1);
            retrieved!.Title.Should().Be("Updated Task 1");
            retrieved.IsCompleted.Should().BeTrue();
        }

        // Act & Assert (Delete)
        using (var context = CreateDbContext(dbName))
        {
            var repo = new TodoRepository(context);
            repo.Delete(todo);
            await repo.SaveChangesAsync();
        }

        using (var context = CreateDbContext(dbName))
        {
            var retrieved = await context.Todos.FindAsync(1);
            retrieved.Should().BeNull();
        }
    }

    [Fact]
    public async Task GetPagedAsync_ShouldFilterSearchAndPageCorrectly()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using (var context = CreateDbContext(dbName))
        {
            var todos = new List<Todo>
            {
                new() { Id = 1, UserId = 1, Title = "Shopping list", Description = "Buy milk", IsCompleted = false, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
                new() { Id = 2, UserId = 1, Title = "Clean kitchen", Description = "Clean floor", IsCompleted = true, CreatedAt = DateTime.UtcNow.AddMinutes(-5) },
                new() { Id = 3, UserId = 1, Title = "Math homework", Description = "Solve equations", IsCompleted = false, CreatedAt = DateTime.UtcNow },
                new() { Id = 4, UserId = 2, Title = "User 2 task", Description = "Other user", IsCompleted = false } // Owned by User 2
            };

            context.Todos.AddRange(todos);
            await context.SaveChangesAsync();
        }

        // Act & Assert
        using (var context = CreateDbContext(dbName))
        {
            var repo = new TodoRepository(context);

            // Test 1: Get all User 1 tasks, ordered by CreatedAt desc
            var (items, count) = await repo.GetPagedAsync(1, null, null, 1, 10);
            count.Should().Be(3);
            items.Should().HaveCount(3);
            items.First().Id.Should().Be(3); // Most recent

            // Test 2: Search term 'kitchen'
            var (itemsSearch, countSearch) = await repo.GetPagedAsync(1, "kitchen", null, 1, 10);
            countSearch.Should().Be(1);
            itemsSearch.First().Id.Should().Be(2);

            // Test 3: Search term in description 'equations'
            var (itemsDesc, countDesc) = await repo.GetPagedAsync(1, "equations", null, 1, 10);
            countDesc.Should().Be(1);
            itemsDesc.First().Id.Should().Be(3);

            // Test 4: Filter by completed = false
            var (itemsComp, countComp) = await repo.GetPagedAsync(1, null, false, 1, 10);
            countComp.Should().Be(2);
            itemsComp.Select(t => t.Id).Should().Contain(new[] { 1, 3 });

            // Test 5: Pagination (Page 2, Size 2)
            var (itemsPage, countPage) = await repo.GetPagedAsync(1, null, null, 2, 2);
            countPage.Should().Be(3); // Total count for user 1 remains 3
            itemsPage.Should().HaveCount(1); // Page 2 contains the last item (oldest: Id 1)
            itemsPage.First().Id.Should().Be(1);
        }
    }
}
