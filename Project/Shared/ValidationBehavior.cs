using FluentValidation;
using MediatR;
using Project.Shared;

namespace Project.Shared;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0) return await next();

        var error = string.Join("; ", failures.Select(x => x.ErrorMessage));
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var genericArg = responseType.GetGenericArguments()[0];
            var method = typeof(Result<>).MakeGenericType(genericArg).GetMethod(nameof(Result<object>.Failure), [typeof(string)]);
            return (TResponse)method!.Invoke(null, [error])!;
        }

        throw new ValidationException(failures);
    }
}
