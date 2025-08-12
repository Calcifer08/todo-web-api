using AutoMapper;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using TodoWebApi.Application.DTOs;
using TodoWebApi.Application.Interfaces;
using TodoWebApi.Domain.Entities;

namespace TodoWebApi.Application.Services;

public class TodoService : ITodoService
{
  private readonly IApplicationDbContext _context;
  private readonly IHttpContextAccessor _httpContextAccessor;
  private readonly IFileStorageService _fileStorageService;
  private readonly IDistributedCache _cache;
  private readonly IMapper _mapper;

  public TodoService(IApplicationDbContext context, IHttpContextAccessor httpContextAccessor,
    IFileStorageService fileStorageService, IDistributedCache cache, IMapper mapper)
  {
    _context = context;
    _httpContextAccessor = httpContextAccessor;
    _fileStorageService = fileStorageService;
    _cache = cache;
    _mapper = mapper;
  }

  private string? GetCurrentUserId()
  {
    return _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
  }

  public async Task<List<TodoViewDto>> GetAllAsync()
  {
    var currentUserId = GetCurrentUserId();
    var cacheKey = $"todos_{currentUserId}";

    var cachedData = await _cache.GetStringAsync(cacheKey);
    if (cachedData is not null)
    {
      return JsonSerializer.Deserialize<List<TodoViewDto>>(cachedData)!;
    }

    var todos = await _context.Todos.Where(t => t.UserId == currentUserId).ToListAsync();

    var todosDto = _mapper.Map<List<TodoViewDto>>(todos);

    var cacheOptions = new DistributedCacheEntryOptions()
      .SetSlidingExpiration(TimeSpan.FromMinutes(5))
      .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

    var serializedTodos = JsonSerializer.Serialize(todosDto);

    await _cache.SetStringAsync(cacheKey, serializedTodos, cacheOptions);

    return todosDto;
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
    _context.Todos.Add(todo);
    await _context.SaveChangesAsync();
    await _cache.RemoveAsync($"todos_{GetCurrentUserId()}");

    return todo;
  }

  public async Task UpdateAsync(Todo newTodo)
  {
    _context.Todos.Update(newTodo);
    await _context.SaveChangesAsync();
    await _cache.RemoveAsync($"todos_{GetCurrentUserId()}");
  }

  public async Task DeleteAsync(Todo todoDel)
  {
    _context.Todos.Remove(todoDel);
    await _context.SaveChangesAsync();
    await _cache.RemoveAsync($"todos_{GetCurrentUserId()}");
  }

  public async Task<string?> AddAttachmentAsync(int todoId, IFormFile file)
  {
    var todo = await GetByIdAsync(todoId);
    if (todo is null) return null;

    if (!string.IsNullOrEmpty(todo.StoredFileName))
    {
      await _fileStorageService.DeleteFileAsync(todo.StoredFileName);
    }

    var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

    await using var stream = file.OpenReadStream();
    var fileUrl = await _fileStorageService.UploadFileAsync(stream, storedFileName, file.ContentType);

    todo.AttachmentUrl = fileUrl;
    todo.StoredFileName = storedFileName;
    todo.OriginalFileName = file.FileName;

    await _context.SaveChangesAsync();
    await _cache.RemoveAsync($"todos_{GetCurrentUserId()}");

    return fileUrl;
  }

  public async Task DeleteAttachmentAsync(int todoId)
  {
    var todo = await GetByIdAsync(todoId);
    if (todo is null || string.IsNullOrEmpty(todo.StoredFileName)) return;

    await _fileStorageService.DeleteFileAsync(todo.StoredFileName);

    todo.AttachmentUrl = null;
    todo.StoredFileName = null;
    todo.OriginalFileName = null;

    await _context.SaveChangesAsync();
    await _cache.RemoveAsync($"todos_{GetCurrentUserId()}");
  }
}