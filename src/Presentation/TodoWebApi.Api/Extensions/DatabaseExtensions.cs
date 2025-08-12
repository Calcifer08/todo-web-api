using Microsoft.EntityFrameworkCore;
using TodoWebApi.Application.Interfaces;
using TodoWebApi.Infrastructure.Data;

namespace TodoWebApi.Api.Extensions;

public static class DatabaseExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        if (dbContext is DbContext concreteDbContext)
        {
            concreteDbContext.Database.Migrate();
        }
    }
}
