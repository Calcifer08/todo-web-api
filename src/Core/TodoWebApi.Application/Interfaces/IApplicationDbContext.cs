using TodoWebApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TodoWebApi.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Todo> Todos { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}