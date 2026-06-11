using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Repositories;

public class TodoRepository : ITodoRepository
{
    private readonly AppDbContext _context;

    public TodoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Todo?> GetByIdAsync(int id)
    {
        return await _context.Todos.FindAsync(id);
    }

    public async Task<(IEnumerable<Todo> Items, int TotalCount)> GetPagedAsync(
        int userId, 
        string? search, 
        bool? isCompleted, 
        int pageNumber, 
        int pageSize)
    {
        var query = _context.Todos.Where(t => t.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(searchLower) || 
                                     (t.Description != null && t.Description.ToLower().Contains(searchLower)));
        }

        if (isCompleted.HasValue)
        {
            query = query.Where(t => t.IsCompleted == isCompleted.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(Todo todo)
    {
        await _context.Todos.AddAsync(todo);
    }

    public void Update(Todo todo)
    {
        todo.UpdatedAt = DateTime.UtcNow;
        _context.Todos.Update(todo);
    }

    public void Delete(Todo todo)
    {
        _context.Todos.Remove(todo);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
