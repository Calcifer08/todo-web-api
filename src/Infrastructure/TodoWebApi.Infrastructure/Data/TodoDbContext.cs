using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using TodoWebApi.Domain.Entities;
using TodoWebApi.Application.Interfaces;

namespace TodoWebApi.Infrastructure.Data;

public class TodoDbContext : IdentityDbContext<ApiUser>, IApplicationDbContext
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options)
    : base(options)
    {
        //Database.EnsureCreated();   // не использовать при использовании миграций
    }

    public DbSet<Todo> Todos { get; set; }
}