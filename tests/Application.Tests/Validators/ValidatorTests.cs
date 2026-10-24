using FluentValidation.TestHelper;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Validators;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Application.Tests.Validators;

public class ValidatorTests
{
    [Fact]
    public void CreateProduct_accepts_a_valid_name()
    {
        new CreateProductRequestValidator()
            .TestValidate(new CreateProductRequest("Widget"))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateProduct_rejects_a_blank_name(string name)
    {
        new CreateProductRequestValidator()
            .TestValidate(new CreateProductRequest(name))
            .ShouldHaveValidationErrorFor(request => request.ProductName);
    }

    [Fact]
    public void CreateProduct_rejects_a_name_longer_than_the_column()
    {
        new CreateProductRequestValidator()
            .TestValidate(new CreateProductRequest(new string('a', Product.NameMaxLength + 1)))
            .ShouldHaveValidationErrorFor(request => request.ProductName);
    }

    [Fact]
    public void UpdateProduct_applies_the_same_rules_as_create()
    {
        var validator = new UpdateProductRequestValidator();

        validator.TestValidate(new UpdateProductRequest("Widget")).ShouldNotHaveAnyValidationErrors();
        validator.TestValidate(new UpdateProductRequest("")).ShouldHaveValidationErrorFor(request => request.ProductName);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(25, true)]
    [InlineData(-1, false)]
    public void Item_quantity_must_not_be_negative(int quantity, bool valid)
    {
        var create = new CreateItemRequestValidator().TestValidate(new CreateItemRequest(quantity));
        var update = new UpdateItemRequestValidator().TestValidate(new UpdateItemRequest(quantity));

        Assert.Equal(valid, create.IsValid);
        Assert.Equal(valid, update.IsValid);
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(5, 100, true)]
    [InlineData(0, 20, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 101, false)]
    public void Paging_is_limited_to_sensible_values(int pageNumber, int pageSize, bool valid)
    {
        var result = new PagedQueryValidator().TestValidate(new PagedQuery { PageNumber = pageNumber, PageSize = pageSize });

        Assert.Equal(valid, result.IsValid);
    }

    [Fact]
    public void Paging_defaults_are_valid()
    {
        new PagedQueryValidator().TestValidate(new PagedQuery()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("alice@example.com", "Str0ng!Passw0rd", true)]
    [InlineData("not-an-email", "Str0ng!Passw0rd", false)]
    [InlineData("", "Str0ng!Passw0rd", false)]
    [InlineData("alice@example.com", "short", false)]
    [InlineData("alice@example.com", "", false)]
    public void Register_requires_an_email_and_a_long_enough_password(string email, string password, bool valid)
    {
        var result = new RegisterRequestValidator().TestValidate(new RegisterRequest(email, password));

        Assert.Equal(valid, result.IsValid);
    }

    [Fact]
    public void Register_rejects_an_email_that_would_not_fit_the_audit_columns()
    {
        var email = new string('a', Product.AuditUserMaxLength) + "@example.com";

        new RegisterRequestValidator()
            .TestValidate(new RegisterRequest(email, "Str0ng!Passw0rd"))
            .ShouldHaveValidationErrorFor(request => request.Email);
    }

    [Fact]
    public void Login_and_refresh_require_their_fields()
    {
        new LoginRequestValidator().TestValidate(new LoginRequest("", "")).ShouldHaveValidationErrorFor(r => r.Email);
        new LoginRequestValidator().TestValidate(new LoginRequest("a@b.c", "")).ShouldHaveValidationErrorFor(r => r.Password);
        new RefreshTokenRequestValidator().TestValidate(new RefreshTokenRequest("")).ShouldHaveValidationErrorFor(r => r.RefreshToken);
    }
}
