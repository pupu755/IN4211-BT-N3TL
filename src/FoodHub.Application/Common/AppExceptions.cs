namespace FoodHub.Application.Common;

/// <summary>Base type for failures that the API turns into a JSON error response.</summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message)
    {
    }
}

/// <summary>Maps to HTTP 404.</summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message)
    {
    }
}

/// <summary>Maps to HTTP 409.</summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(message)
    {
    }
}

/// <summary>Maps to HTTP 403.</summary>
public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message)
    {
    }
}

/// <summary>Maps to HTTP 401.</summary>
public sealed class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message) : base(message)
    {
    }
}

/// <summary>Maps to HTTP 422.</summary>
public sealed class RequestValidationException : AppException
{
    public RequestValidationException(IDictionary<string, string[]> errors) : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }
}
