namespace ProductManagement.Domain.Exceptions;

/// <summary>Credentials or a refresh token were rejected. Surfaces to API clients as 401 Unauthorized.</summary>
public sealed class AuthenticationFailedException(string message) : DomainException(message);
