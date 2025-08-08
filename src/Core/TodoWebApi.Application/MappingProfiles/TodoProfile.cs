using AutoMapper;
using TodoWebApi.Application.DTOs;
using TodoWebApi.Domain.Entities;

namespace TodoWebApi.Application.MappingProfiles;

public class TodoProfile : Profile
{
    public TodoProfile()
    {
        CreateMap<TodoDto, Todo>().ReverseMap();

        CreateMap<Todo, TodoViewDto>();
    }
}