using TodoWebApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace TodoWebApi.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Todo> Todos { get; set; }
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}