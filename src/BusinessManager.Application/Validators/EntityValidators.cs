using System;
using System.Linq;
using FluentValidation;
using BusinessManager.Domain.Entities;
using BusinessManager.Domain.Enums;

namespace BusinessManager.Application.Validators;

public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
    {
        RuleFor(u => u.Username)
            .NotEmpty().WithMessage("Username is required")
            .Length(3, 100).WithMessage("Username must be between 3 and 100 characters")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Username can only contain letters, numbers, and underscores");

        RuleFor(u => u.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .Length(2, 100).WithMessage("Full name must be between 2 and 100 characters");

        RuleFor(u => u.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .When(u => !string.IsNullOrEmpty(u.Email));
    }
}

public class SaleValidator : AbstractValidator<Sale>
{
    public SaleValidator()
    {
        RuleFor(s => s.ReceiptNumber)
            .NotEmpty().WithMessage("Receipt number is required")
            .Length(5, 50).WithMessage("Receipt number must be between 5 and 50 characters");

        RuleFor(s => s.TotalAmount)
            .GreaterThan(0).WithMessage("Total amount must be greater than 0");

        RuleFor(s => s.AmountPaid)
            .GreaterThanOrEqualTo(0).WithMessage("Amount paid must be non-negative");

        RuleFor(s => s.UserId)
            .GreaterThan(0).WithMessage("Valid user is required");

        RuleFor(s => s.SaleItems)
            .NotEmpty().WithMessage("At least one sale item is required")
            .Must(items => items.All(item => item.Quantity > 0)).WithMessage("All item quantities must be greater than 0")
            .Must(items => items.All(item => item.UnitPrice > 0)).WithMessage("All item prices must be greater than 0");
    }
}

public class ExpenseValidator : AbstractValidator<Expense>
{
    public ExpenseValidator()
    {
        RuleFor(e => e.Description)
            .NotEmpty().WithMessage("Description is required")
            .Length(5, 200).WithMessage("Description must be between 5 and 200 characters");

        RuleFor(e => e.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0");

        RuleFor(e => e.ExpenseCategoryId)
            .GreaterThan(0).WithMessage("Valid expense category is required");

        RuleFor(e => e.UserId)
            .GreaterThan(0).WithMessage("Valid user is required");
    }
}

public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Product name is required")
            .Length(2, 100).WithMessage("Product name must be between 2 and 100 characters");

        RuleFor(p => p.BuyingPrice)
            .GreaterThan(0).WithMessage("Buying price must be greater than 0");

        RuleFor(p => p.SellingPrice)
            .GreaterThan(0).WithMessage("Selling price must be greater than 0")
            .Must((product, sellingPrice) => sellingPrice > product.BuyingPrice)
            .WithMessage("Selling price must be greater than buying price");

        RuleFor(p => p.CurrentStock)
            .GreaterThanOrEqualTo(0).WithMessage("Current stock must be non-negative");

        RuleFor(p => p.ReorderLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Reorder level must be non-negative");

        RuleFor(p => p.MaxStock)
            .GreaterThan(0).WithMessage("Max stock must be greater than 0")
            .Must((product, maxStock) => maxStock > product.ReorderLevel)
            .WithMessage("Max stock must be greater than reorder level");

        RuleFor(p => p.SKU)
            .Length(3, 100).WithMessage("SKU must be between 3 and 100 characters")
            .When(p => !string.IsNullOrEmpty(p.SKU));
    }
}

public class ServiceItemValidator : AbstractValidator<ServiceItem>
{
    public ServiceItemValidator()
    {
        RuleFor(si => si.Name)
            .NotEmpty().WithMessage("Service name is required")
            .Length(2, 100).WithMessage("Service name must be between 2 and 100 characters");

        RuleFor(si => si.DefaultPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Default price must be non-negative");
    }
}
