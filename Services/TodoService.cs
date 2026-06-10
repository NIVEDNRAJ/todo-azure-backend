using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using TodoApi.DTOs;
using TodoApi.Models;
using TodoApi.Repositories;

namespace TodoApi.Services;

public class TodoService : ITodoService
{
    private readonly ITodoRepository _todoRepository;
    private readonly IMapper _mapper;

    public TodoService(ITodoRepository todoRepository, IMapper mapper)
    {
        _todoRepository = todoRepository;
        _mapper = mapper;
    }

    public async Task<TodoDto> GetByIdAsync(int id, int userId)
    {
        var todo = await _todoRepository.GetByIdAsync(id);
        if (todo == null)
        {
            throw new KeyNotFoundException("Todo item not found.");
        }

        if (todo.UserId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to access this todo item.");
        }

        return _mapper.Map<TodoDto>(todo);
    }

    public async Task<PagedResultDto<TodoDto>> GetPagedAsync(int userId, string? search, bool? isCompleted, int pageNumber, int pageSize)
    {
        var (items, totalCount) = await _todoRepository.GetPagedAsync(userId, search, isCompleted, pageNumber, pageSize);
        
        var todoDtos = _mapper.Map<IEnumerable<TodoDto>>(items);

        return new PagedResultDto<TodoDto>
        {
            Items = todoDtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<TodoDto> CreateAsync(CreateTodoDto dto, int userId)
    {
        var todo = _mapper.Map<Todo>(dto);
        todo.UserId = userId;
        todo.CreatedAt = DateTime.UtcNow;
        todo.UpdatedAt = DateTime.UtcNow;
        todo.IsCompleted = false;

        await _todoRepository.AddAsync(todo);
        await _todoRepository.SaveChangesAsync();

        return _mapper.Map<TodoDto>(todo);
    }

    public async Task<TodoDto> UpdateAsync(int id, UpdateTodoDto dto, int userId)
    {
        var todo = await _todoRepository.GetByIdAsync(id);
        if (todo == null)
        {
            throw new KeyNotFoundException("Todo item not found.");
        }

        if (todo.UserId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to update this todo item.");
        }

        _mapper.Map(dto, todo);
        todo.UpdatedAt = DateTime.UtcNow;

        _todoRepository.Update(todo);
        await _todoRepository.SaveChangesAsync();

        return _mapper.Map<TodoDto>(todo);
    }

    public async Task DeleteAsync(int id, int userId)
    {
        var todo = await _todoRepository.GetByIdAsync(id);
        if (todo == null)
        {
            throw new KeyNotFoundException("Todo item not found.");
        }

        if (todo.UserId != userId)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete this todo item.");
        }

        _todoRepository.Delete(todo);
        await _todoRepository.SaveChangesAsync();
    }
}
