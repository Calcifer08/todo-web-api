using AutoMapper;
using TodoWebApi.Api.DTOs;
using TodoWebApi.Application.DTOs;
using TodoWebApi.Domain.Entities;

namespace TodoWebApi.Api.MappingProfiles;

public class TodoProfile : Profile
{
    public TodoProfile()
    {
        CreateMap<TodoDto, Todo>().ReverseMap();

        CreateMap<Todo, TodoViewDto>();
    }
}