namespace OneMoi.Application.Common;

/// <summary>Business-rule failures. The API turns these into clean JSON errors.</summary>
public class AppException : Exception
{
    public int StatusCode { get; }
    public IDictionary<string, string[]>? Errors { get; }

    public AppException(string message, int statusCode = 400, IDictionary<string, string[]>? errors = null) : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}

public class NotFoundException(string what) : AppException($"{what} not found.", 404);

public class ForbiddenException(string message = "You do not have access to this.") : AppException(message, 403);

public class ValidationException : AppException
{
    public ValidationException(string field, string message)
        : base(message, 400, new Dictionary<string, string[]> { [field] = new[] { message } }) { }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("Please correct the highlighted fields.", 400, errors) { }
}

/// <summary>Collects several field errors, then throws once.</summary>
public class Validator
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public Validator Require(string field, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value)) Add(field, message);
        return this;
    }

    public Validator Check(bool condition, string field, string message)
    {
        if (!condition) Add(field, message);
        return this;
    }

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var list)) _errors[field] = list = new List<string>();
        list.Add(message);
    }

    public void ThrowIfInvalid()
    {
        if (_errors.Count > 0) throw new ValidationException(_errors.ToDictionary(k => k.Key, v => v.Value.ToArray()));
    }
}
