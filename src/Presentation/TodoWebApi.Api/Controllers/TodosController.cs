using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoWebApi.Application.DTOs;
using TodoWebApi.Application.Interfaces;
using TodoWebApi.Domain.Entities;

namespace TodoWebApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TodosController : ControllerBase
{
    private readonly ITodoService _todoService;
    private readonly IMapper _mapper;
    private readonly IValidator<TodoDto> _validator;
    public TodosController(ITodoService todoService, IMapper mapper, IValidator<TodoDto> validator)
    {
        _todoService = todoService;
        _mapper = mapper;
        _validator = validator;
    }

    // /api/todos
    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        var todos = await _todoService.GetAllAsync();
        var todosViewDto = _mapper.Map<List<TodoViewDto>>(todos);
        return Ok(todosViewDto);
    }

    // /api/todos/1
    [HttpGet("{id:int}")]
    [ActionName(nameof(GetByIdAsync))]
    public async Task<IActionResult> GetByIdAsync(int id)
    {
        var todo = await _todoService.GetByIdAsync(id);

        if (todo is null)
            return NotFound();

        var todoViewDto = _mapper.Map<TodoViewDto>(todo);
        return Ok(todoViewDto);
    }

    // /api/todos
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] TodoDto todoDto)
    {
        var validResult = await _validator.ValidateAsync(todoDto);
        if (!validResult.IsValid)
        {
            return BadRequest(validResult.ToDictionary());
        }

        var newTodo = _mapper.Map<Todo>(todoDto);
        var createdTodo = await _todoService.CreateAsync(newTodo);
        var todoViewDto = _mapper.Map<TodoViewDto>(createdTodo);

        return CreatedAtAction(nameof(GetByIdAsync), new { id = todoViewDto.Id }, todoViewDto);
    }

    // /api/todos/1
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] TodoDto todoDto)
    {
        var todoToUpdate = await _todoService.GetByIdAsync(id);
        if (todoToUpdate is null)
        {
            return NotFound();
        }

        var validResult = await _validator.ValidateAsync(todoDto);
        if (!validResult.IsValid)
        {
            return BadRequest(validResult.ToDictionary());
        }

        _mapper.Map(todoDto, todoToUpdate);

        await _todoService.UpdateAsync(todoToUpdate);
        return NoContent();
    }

    // /api/todos/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var todoToDelete = await _todoService.GetByIdAsync(id);
        if (todoToDelete is null)
        {
            return NotFound();
        }

        await _todoService.DeleteAsync(todoToDelete);
        return NoContent();
    }
}