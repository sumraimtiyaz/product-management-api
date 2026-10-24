using FluentValidation;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Application.Validators;

/// <summary>Rules shared by more than one request validator, so each is defined once.</summary>
internal static class ProductRules
{
    public static IRuleBuilderOptions<T, string> MustBeValidProductName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(Product.NameMaxLength);

    public static IRuleBuilderOptions<T, int> MustBeValidQuantity<T>(this IRuleBuilder<T, int> rule) =>
        rule.GreaterThanOrEqualTo(0);
}
