namespace ProductManagement.Domain.Exceptions;

/// <summary>The requested resource does not exist. Surfaces to API clients as 404 Not Found.</summary>
public sealed class NotFoundException(string message) : DomainException(message)
{
    public NotFoundException(string resourceName, object key)
        : this($"{resourceName} with id '{key}' was not found.")
    {
    }
}
