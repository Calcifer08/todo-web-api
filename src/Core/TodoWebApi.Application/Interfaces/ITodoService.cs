using TodoWebApi.Domain.Entities;

namespace TodoWebApi.Application.Interfaces;

public interface ITodoService
{
    Task<List<Todo>> GetAllAsync();
    Task<Todo?> GetByIdAsync(int id);
    Task<Todo> CreateAsync(Todo todo);
    Task UpdateAsync(Todo newTodo);
    Task DeleteAsync(Todo todoDel);
}