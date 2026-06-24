using FluentValidation;
using FridgeManager.Api.DTOs;

namespace FridgeManager.Api.Validators;

public class CreateShoppingItemValidator : AbstractValidator<CreateShoppingItemDto>
{
    public CreateShoppingItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Unit).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.EstimatedCost).GreaterThanOrEqualTo(0);
    }
}
