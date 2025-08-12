using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using TodoWebApi.Application.DTOs;
using TodoWebApi.Application.Interfaces;
using TodoWebApi.Application.Services;
using TodoWebApi.Domain.Entities;
using TodoWebApi.Infrastructure.Data;

namespace TodoWebApi.Application.Tests.Unit;

public class TodoServiceTests
{
  private readonly TodoDbContext _dbContext;
  private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
  private readonly Mock<IDistributedCache> _cacheMock;
  private readonly Mock<IMapper> _mapperMock;
  private readonly Mock<IFileStorageService> _fileStorageServiceMock;
  private readonly ITodoService _sut;

  public TodoServiceTests()
  {
    // --- 1. Создаем НОВУЮ, ЧИСТУЮ БД для КАЖДОГО теста ---
    var options = new DbContextOptionsBuilder<TodoDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
    _dbContext = new TodoDbContext(options);

    // --- 2. Создаем свежие моки для КАЖДОГО теста ---
    _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
    _cacheMock = new Mock<IDistributedCache>();
    _mapperMock = new Mock<IMapper>();
    _fileStorageServiceMock = new Mock<IFileStorageService>();

    // --- 3. Создаем свежий экземпляр сервиса для КАЖДОГО теста ---
    _sut = new TodoService(
        _dbContext,
        _httpContextAccessorMock.Object,
        _fileStorageServiceMock.Object,
        _cacheMock.Object,
        _mapperMock.Object
    );
  }

  // Вспомогательный метод для заполнения нашей чистой БД данными
  private void SeedDatabase()
  {
    _dbContext.Todos.AddRange(
        new Todo { Id = 1, Name = "Задача 1 от user-1", UserId = "user-1" },
        new Todo { Id = 2, Name = "Задача 2 от user-1", UserId = "user-1" },
        new Todo { Id = 3, Name = "Задача 1 от user-2", UserId = "user-2" }
    );
    _dbContext.SaveChanges();
  }

  private void SetupHttpContextForUser(string userId)
  {
    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
    var identity = new ClaimsIdentity(claims);
    var claimsPrincipal = new ClaimsPrincipal(identity);
    var httpContextMock = new Mock<HttpContext>();
    httpContextMock.Setup(x => x.User).Returns(claimsPrincipal);
    _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContextMock.Object);
  }


  [Fact]
  public async Task GetAllAsync_WhenCacheIsMissReturnFromDB()
  {
    // Arrange
    SeedDatabase();
    var currentUserId = "user-1";
    SetupHttpContextForUser(currentUserId);
    var cacheKey = $"todos_{currentUserId}";

    var expectedDtos = new List<TodoViewDto>
    {
        new() { Id = 1, Name = "Задача 1 от user-1" },
        new() { Id = 2, Name = "Задача 2 от user-1" }
    };
    _mapperMock.Setup(m => m.Map<List<TodoViewDto>>(It.IsAny<List<Todo>>()))
      .Returns(expectedDtos);

    _cacheMock.Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
      .ReturnsAsync((byte[])null!);

    // Act
    var result = await _sut.GetAllAsync();

    // Assert
    Assert.NotNull(result);
    Assert.Equal(2, result.Count);
    Assert.Equal(expectedDtos, result);

    _cacheMock.Verify(c => c.SetAsync(cacheKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(),
      It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task GetAllAsync_WhenCacheIsHitReturnDFromCache()
  {
    // Arrange
    var currentUserId = "user-1";
    SetupHttpContextForUser(currentUserId);
    var cacheKey = $"todos_{currentUserId}";

    var cachedDtos = new List<TodoViewDto>
    {
        new() { Id = 99, Name = "Задача из Кэша" }
    };
    var serializedData = JsonSerializer.Serialize(cachedDtos);
    var dataAsBytes = Encoding.UTF8.GetBytes(serializedData);

    _cacheMock.Setup(c => c.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
              .ReturnsAsync(dataAsBytes);

    // Act
    var result = await _sut.GetAllAsync();

    // Assert
    Assert.NotNull(result);
    Assert.Single(result);
    Assert.Equal(99, result.First().Id);
    Assert.Equal("Задача из Кэша", result.First().Name);

    _mapperMock.Verify(m => m.Map<List<TodoViewDto>>(It.IsAny<List<Todo>>()), Times.Never);
  }

  [Fact]
  public async Task GetById_ReturnsTodoIfUserOwnsIt()
  {
    // Arrange
    SeedDatabase();
    var currentUserId = "user-1";
    var todoIdToFind = 1;
    SetupHttpContextForUser(currentUserId);

    // Act
    var result = await _sut.GetByIdAsync(todoIdToFind);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(todoIdToFind, result.Id);
    Assert.Equal(currentUserId, result.UserId);
  }

  [Fact]
  public async Task GetById_ReturnsNullIfNotUserTodo()
  {
    // Arrange
    SeedDatabase();
    var currentUserId = "user-1";
    var otherUserTodoId = 3;
    SetupHttpContextForUser(currentUserId);

    // Act
    var result = await _sut.GetByIdAsync(otherUserTodoId);

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public async Task GetByIdAsync_ReturnsNullIfNoTodo()
  {
    // Arrange
    SeedDatabase();
    var currentUserId = "user-1";
    var todoIdToFind = 9999;
    SetupHttpContextForUser(currentUserId);

    // Act
    var result = await _sut.GetByIdAsync(todoIdToFind);

    // Assert
    Assert.Null(result);
  }

  [Fact]
  public async Task Create_TodoAuthorizedUser()
  {
    // Arrange
    SeedDatabase();
    var currentUserId = "user-1";
    SetupHttpContextForUser(currentUserId);

    var newTodo = new Todo() { Name = "Новая задача user-1", IsComplete = false };

    var initialCountTodo = _dbContext.Todos.Count();

    // Act
    var result = await _sut.CreateAsync(newTodo);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(currentUserId, result.UserId);
    Assert.Equal(initialCountTodo + 1, _dbContext.Todos.Count());
    Assert.Equal(newTodo.Name, result.Name);
    Assert.Equal(newTodo.IsComplete, result.IsComplete);
    _cacheMock.Verify(c => c.RemoveAsync($"todos_{currentUserId}", It.IsAny<CancellationToken>()),
                      Times.Once);
  }

  [Fact]
  public async Task Create_TodoAuthorizedNotUser()
  {
    // Arrange
    SeedDatabase();
    var newTodo = new Todo() { Name = "Новая задача", IsComplete = false };

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(newTodo));
  }

  [Fact]
  public async Task Update_ExistingTodoOwnedUser()
  {
    // Arrange
    SeedDatabase();
    var idTodo = 1;
    var currentUserId = "user-1";
    SetupHttpContextForUser(currentUserId);

    var todoToUpdate = await _dbContext.Todos.FindAsync(idTodo);
    Assert.NotNull(todoToUpdate);

    var newName = "Обновленная задача";
    todoToUpdate.Name = newName;

    // Act
    await _sut.UpdateAsync(todoToUpdate);

    // Assert
    var todoFromDb = await _dbContext.Todos.AsNoTracking().FirstOrDefaultAsync(t => t.Id == idTodo);
    Assert.NotNull(todoFromDb);
    Assert.Equal(newName, todoFromDb.Name);
    _cacheMock.Verify(c => c.RemoveAsync($"todos_{currentUserId}", It.IsAny<CancellationToken>()),
                      Times.Once);
  }

  [Fact]
  public async Task Delete_ExistingTodoOwnedUser()
  {
    // Arrange
    SeedDatabase();
    var idTodo = 1;
    var currentUserId = "user-1";
    SetupHttpContextForUser(currentUserId);

    var initialCountTodo = await _dbContext.Todos.CountAsync();
    var todoToDelete = await _dbContext.Todos.FindAsync(idTodo);
    Assert.NotNull(todoToDelete);

    // Act
    await _sut.DeleteAsync(todoToDelete);

    // Assert
    Assert.Equal(initialCountTodo - 1, _dbContext.Todos.Count());
    var result = await _dbContext.Todos.FindAsync(idTodo);
    Assert.Null(result);
    _cacheMock.Verify(c => c.RemoveAsync($"todos_{currentUserId}", It.IsAny<CancellationToken>()),
                      Times.Once);
  }
}
