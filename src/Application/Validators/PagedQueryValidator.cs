using FluentValidation;
using ProductManagement.Application.DTOs;

namespace ProductManagement.Application.Validators;

public sealed class PagedQueryValidator : AbstractValidator<PagedQuery>
{
    public PagedQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PagedQuery.MaxPageSize);
    }
}
