using FluentValidation;

namespace Sql.Baseline.Api.Infrastructure;

public sealed class ValidationFilter : IEndpointFilter
{
    private readonly IServiceProvider _sp;
    public ValidationFilter(IServiceProvider sp) => _sp = sp;


    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var arg in context.Arguments)
        {
            if (arg is null) continue;
            var validatorType = typeof(IValidator<>).MakeGenericType(arg.GetType());
            var validator = _sp.GetService(validatorType) as IValidator;
            if (validator is null) continue;


            var result = await validator.ValidateAsync(new ValidationContext<object>(arg));
            if (!result.IsValid)
            {
                var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
                return Results.ValidationProblem(errors);
            }
        }
        return await next(context);
    }
}