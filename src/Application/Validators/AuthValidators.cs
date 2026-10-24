using FluentValidation;
using ProductManagement.Application.DTOs;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Application.Validators;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    // The email is the user name that ends up in the CreatedBy/ModifiedBy audit columns, so it has to fit them.
    internal const int EmailMaxLength = Product.AuditUserMaxLength;

    // Complexity rules (digits, casing, symbols) are owned by the identity store's password policy.
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(EmailMaxLength).EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(PasswordMinLength).MaximumLength(PasswordMaxLength);
    }
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(RegisterRequestValidator.EmailMaxLength);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(RegisterRequestValidator.PasswordMaxLength);
    }
}

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(512);
}
