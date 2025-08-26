using FluentValidation;


namespace Sql.Baseline.Api.Features.Todos;


public class CreateTodoValidator : AbstractValidator<CreateTodo>
{
    public CreateTodoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}