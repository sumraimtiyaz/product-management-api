using FluentValidation;
using ProductManagement.Application.DTOs;

namespace ProductManagement.Application.Validators;

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator() => RuleFor(x => x.ProductName).MustBeValidProductName();
}

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator() => RuleFor(x => x.ProductName).MustBeValidProductName();
}
