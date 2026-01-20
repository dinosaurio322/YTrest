using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using YTapi.Domain.Common;

namespace YTapi.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that automatically validates commands and queries
/// using FluentValidation before they are handled.
/// </summary>
/// <typeparam name="TRequest">The request type (Command or Query)</typeparam>
/// <typeparam name="TResponse">The response type (usually Result<T>)</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // If no validators are registered for this request type, skip validation
        if (!_validators.Any())
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;
        _logger.LogDebug("Validating {RequestName}", requestName);

        // Run all validators
        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // Collect all validation failures
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        // If there are validation errors, return a failure result
        if (failures.Any())
        {
            _logger.LogWarning(
                "Validation failed for {RequestName}. Errors: {Errors}",
                requestName,
                string.Join("; ", failures.Select(f => f.ErrorMessage)));

            // Create a validation error
            var errorMessages = string.Join(", ", failures.Select(f => f.ErrorMessage));
            var error = Error.Validation(
                $"{requestName}.ValidationFailed",
                errorMessages);

            // Try to create a Result<T> failure response
            return CreateValidationFailureResponse(error);
        }

        _logger.LogDebug("Validation passed for {RequestName}", requestName);

        // Validation passed, continue to the handler
        return await next();
    }

    /// <summary>
    /// Creates a validation failure response.
    /// Works with Result<T> pattern.
    /// </summary>
    private static TResponse CreateValidationFailureResponse(Error error)
    {
        // Get the Result<T> type from TResponse
        var responseType = typeof(TResponse);

        // Check if response is Result<T>
        if (responseType.IsGenericType && 
            responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = responseType.GetGenericArguments()[0];
            var failureMethod = responseType.GetMethod("Failure");

            if (failureMethod != null)
            {
                var result = failureMethod.Invoke(null, new object[] { error });
                return (TResponse)result!;
            }
        }

        // If not Result<T>, throw exception (fallback)
        throw new ValidationException(error.Message);
    }
}

/// <summary>
/// Logging behavior that logs all requests passing through MediatR.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Handling {RequestName}", requestName);

        var response = await next();

        _logger.LogInformation("Handled {RequestName}", requestName);

        return response;
    }
}

/// <summary>
/// Performance monitoring behavior that logs slow requests.
/// </summary>
public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly System.Diagnostics.Stopwatch _timer;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
        _timer = new System.Diagnostics.Stopwatch();
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _timer.Start();

        var response = await next();

        _timer.Stop();

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

        // Log warning if request took longer than 500ms
        if (elapsedMilliseconds > 500)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogWarning(
                "Long Running Request: {RequestName} ({ElapsedMilliseconds} ms)",
                requestName,
                elapsedMilliseconds);
        }

        return response;
    }
}