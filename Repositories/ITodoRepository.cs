using System.Collections.Generic;
using System.Threading.Tasks;
using TodoApi.Models;

namespace TodoApi.Repositories;

public interface ITodoRepository
{
    Task<Todo?> GetByIdAsync(int id);
    Task<(IEnumerable<Todo> Items, int TotalCount)> GetPagedAsync(
        int userId, 
        string? search, 
        bool? isCompleted, 
        int pageNumber, 
        int pageSize);
    Task AddAsync(Todo todo);
    void Update(Todo todo);
    void Delete(Todo todo);
    Task SaveChangesAsync();
}
