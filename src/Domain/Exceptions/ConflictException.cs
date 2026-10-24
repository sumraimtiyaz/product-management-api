namespace ProductManagement.Domain.Exceptions;

/// <summary>The request conflicts with the current state of a resource. Surfaces to API clients as 409 Conflict.</summary>
public sealed class ConflictException(string message) : DomainException(message);
