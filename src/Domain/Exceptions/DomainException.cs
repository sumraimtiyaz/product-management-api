namespace ProductManagement.Domain.Exceptions;

/// <summary>A business rule or invariant was violated. Surfaces to API clients as 400 Bad Request.</summary>
public class DomainException(string message) : Exception(message);
