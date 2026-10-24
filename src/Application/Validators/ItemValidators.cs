using FluentValidation;
using ProductManagement.Application.DTOs;

namespace ProductManagement.Application.Validators;

public sealed class CreateItemRequestValidator : AbstractValidator<CreateItemRequest>
{
    public CreateItemRequestValidator() => RuleFor(x => x.Quantity).MustBeValidQuantity();
}

public sealed class UpdateItemRequestValidator : AbstractValidator<UpdateItemRequest>
{
    public UpdateItemRequestValidator() => RuleFor(x => x.Quantity).MustBeValidQuantity();
}
