using FluentValidation;
using FridgeManager.Api.DTOs;

namespace FridgeManager.Api.Validators;

public class CreateFridgeItemValidator : AbstractValidator<CreateFridgeItemDto>
{
    public CreateFridgeItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than zero.");
        RuleFor(x => x.CostPerUnit).GreaterThanOrEqualTo(0).WithMessage("Cost cannot be negative.");
        RuleFor(x => x.ExpiryDate).GreaterThan(x => x.PurchaseDate).WithMessage("Expiry date must be after purchase date.");
        RuleFor(x => x.ExpiryReminderDays).InclusiveBetween(1, 30);
    }
}

public class UpdateFridgeItemValidator : AbstractValidator<UpdateFridgeItemDto>
{
    public UpdateFridgeItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.CostPerUnit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ExpiryDate).GreaterThan(DateTime.UtcNow).WithMessage("Expiry date must be in the future.");
        RuleFor(x => x.ExpiryReminderDays).InclusiveBetween(1, 30);
    }
}

public class MarkUsedValidator : AbstractValidator<MarkUsedDto>
{
    public MarkUsedValidator()
    {
        RuleFor(x => x.QuantityUsed).GreaterThan(0).WithMessage("Quantity used must be greater than zero.");
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
