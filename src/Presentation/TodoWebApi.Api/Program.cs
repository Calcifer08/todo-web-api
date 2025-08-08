using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using TodoWebApi.Api.Middleware;
using TodoWebApi.Api.Swagger;
using TodoWebApi.Application.DTOs;
using TodoWebApi.Application.Interfaces;
using TodoWebApi.Application.MappingProfiles;
using TodoWebApi.Application.Services;
using TodoWebApi.Application.Validators;
using TodoWebApi.Domain.Entities;
using TodoWebApi.Infrastructure.Data;

namespace TodoWebApi.Api
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var builder = WebApplication.CreateBuilder(args);

      builder.Services.AddControllers();

      builder.Services.AddDbContext<IApplicationDbContext, TodoDbContext>(options =>
          options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

      builder.Services.AddIdentity<ApiUser, IdentityRole>(options =>
      {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
      })
      .AddEntityFrameworkStores<TodoDbContext>();

      var jwtIssuer = builder.Configuration["JWT:Issuer"];
      var jwtAudience = builder.Configuration["JWT:Audience"];
      var jwtKey = builder.Configuration["JWT:Key"];

      if (string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience) || string.IsNullOrEmpty(jwtKey))
      {
        throw new InvalidOperationException("Ключевые параметры JWT (Issuer, Audience, Key) не настроены в конфигурации");
      }

      builder.Services.AddAuthentication(options =>
      {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
      }).AddJwtBearer(options =>
      {
        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer = jwtIssuer,
          ValidAudience = jwtAudience,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
      });

      builder.Services.AddAuthorization();

      builder.Services.AddScoped<ITodoService, TodoService>();
      builder.Services.AddScoped<ITokenService, TokenService>();
      builder.Services.AddHttpContextAccessor();
      builder.Services.AddAutoMapper(mp =>
      {
        mp.AddProfile<TodoProfile>();
      });
      builder.Services.AddScoped<IValidator<TodoDto>, TodoDtoValidator>();

      builder.Services.AddCors(options =>
      {
        options.AddPolicy("WebAppPolicy", policy =>
        {
          policy.WithOrigins("http://localhost:5042", "https://localhost:7096")
            .WithHeaders(HeaderNames.ContentType, HeaderNames.Authorization)
            .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete);
        });
      });

      builder.Services.AddEndpointsApiExplorer();
      builder.Services.AddSwaggerGen(options =>
      {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "Todo Web API (Controllers)", Version = "v1" });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
          In = ParameterLocation.Header,
          Description = "Пожалуйста, введите JWT токен",
          Name = "Authorization",
          Type = SecuritySchemeType.Http,
          BearerFormat = "JWT",
          Scheme = "bearer"
        });
        options.OperationFilter<SecurityRequirementsOperationFilter>();
      });


      var app = builder.Build();

      if (app.Environment.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI();
      }
      else
      {
        app.UseExceptionHandler(appBuilder =>
        {
          appBuilder.Run(async context =>
          {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { message = "Произошла непредвиденная ошибка на сервере." });
          });
        });
      }

      app.UseHttpsRedirection();
      app.UseDefaultFiles();
      app.UseStaticFiles();
      app.UseCors("WebAppPolicy");
      app.UseAuthentication();
      app.UseAuthorization();
      app.UseMiddleware<RequestLoggingMiddleware>();


      app.MapControllers();

      app.Run();
    }
  }
}
