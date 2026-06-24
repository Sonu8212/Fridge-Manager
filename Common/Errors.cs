using ErrorOr;

namespace FridgeManager.Api.Common;

public static class Errors
{
    public static class FridgeItem
    {
        public static Error NotFound(int id) =>
            Error.NotFound("FridgeItem.NotFound", $"Fridge item with id '{id}' was not found.");

        public static Error QuantityExceedsStock(decimal available) =>
            Error.Validation("FridgeItem.QuantityExceedsStock", $"Quantity used cannot exceed available stock of {available}.");

        public static Error AlreadyUsed =>
            Error.Conflict("FridgeItem.AlreadyUsed", "This item has already been fully consumed.");
    }

    public static class ShoppingItem
    {
        public static Error NotFound(int id) =>
            Error.NotFound("ShoppingItem.NotFound", $"Shopping list item with id '{id}' was not found.");
    }
}
