using TodoWebApi.Api.DTOs;
using FluentValidation;

namespace TodoWebApi.Api.Validators;

public class TodoDtoValidator : AbstractValidator<TodoDto>
{
    public TodoDtoValidator()
    {
        RuleFor(dto => dto.Name)
        .NotEmpty().WithMessage("Имя задачи не может быть пустым")
        .MaximumLength(100).WithMessage("Имя задачи не может быть длиннее 100 символов");
    }
}