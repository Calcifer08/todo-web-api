using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TodoWebApi.Application.Interfaces;
using TodoWebApi.Domain.Entities;

namespace TodoWebApi.Application.Services;

public class TodoService : ITodoService
{
  private readonly IApplicationDbContext _context;
  private readonly IHttpContextAccessor _httpContextAccessor;

  public TodoService(IApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
  {
    _context = context;
    _httpContextAccessor = httpContextAccessor;
  }

  private string? GetCurrentUserId()
  {
    return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
  }

  public async Task<List<Todo>> GetAllAsync()
  {
    var currentUserId = GetCurrentUserId();

    return await _context.Todos.Where(t => t.UserId == currentUserId).ToListAsync();
  }

  public async Task<Todo?> GetByIdAsync(int id)
  {
    var currentUserId = GetCurrentUserId();
    return await _context.Todos.FirstOrDefaultAsync(t => t.Id == id && t.UserId == currentUserId);
  }

  public async Task<Todo> CreateAsync(Todo todo)
  {
    var currentUserId = GetCurrentUserId();

    if (string.IsNullOrEmpty(currentUserId))
    {
      throw new InvalidOperationException("Не удалось определить пользователя для создания задачи");
    }

    todo.UserId = currentUserId;
    await _context.Todos.AddAsync(todo);
    await _context.SaveChangesAsync();

    return todo;
  }

  public async Task UpdateAsync(Todo newTodo)
  {
    _context.Todos.Update(newTodo);

    await _context.SaveChangesAsync();
  }

  public async Task DeleteAsync(Todo todoDel)
  {
    _context.Todos.Remove(todoDel);
    await _context.SaveChangesAsync();
  }
}