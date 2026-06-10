using System.Threading.Tasks;
using TodoApi.DTOs;

namespace TodoApi.Services;

public interface ITodoService
{
    Task<TodoDto> GetByIdAsync(int id, int userId);
    Task<PagedResultDto<TodoDto>> GetPagedAsync(int userId, string? search, bool? isCompleted, int pageNumber, int pageSize);
    Task<TodoDto> CreateAsync(CreateTodoDto dto, int userId);
    Task<TodoDto> UpdateAsync(int id, UpdateTodoDto dto, int userId);
    Task DeleteAsync(int id, int userId);
}
