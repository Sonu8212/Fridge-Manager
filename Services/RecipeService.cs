namespace FridgeManager.Api.Services;

public record RecipeSuggestion(string Title, string Description, string Url, List<string> Ingredients);

public interface IRecipeService
{
    Task<List<RecipeSuggestion>> SuggestRecipesAsync(List<string> ingredients);
}

public class RecipeService(HttpClient httpClient, IConfiguration config) : IRecipeService
{
    private readonly string _apiKey = config["Spoonacular:ApiKey"] ?? string.Empty;

    public async Task<List<RecipeSuggestion>> SuggestRecipesAsync(List<string> ingredients)
    {
        if (string.IsNullOrEmpty(_apiKey) || ingredients.Count == 0)
            return GetFallbackSuggestions(ingredients);

        try
        {
            var ingredientList = string.Join(",", ingredients);
            var url = $"https://api.spoonacular.com/recipes/findByIngredients?ingredients={Uri.EscapeDataString(ingredientList)}&number=5&apiKey={_apiKey}";

            var response = await httpClient.GetFromJsonAsync<List<SpoonacularRecipe>>(url);

            return response?.Select(r => new RecipeSuggestion(
                r.Title,
                $"Uses {r.UsedIngredientCount} of your expiring ingredients",
                $"https://spoonacular.com/recipes/{r.Title.Replace(" ", "-").ToLower()}-{r.Id}",
                r.UsedIngredients.Select(i => i.Name).ToList()
            )).ToList() ?? GetFallbackSuggestions(ingredients);
        }
        catch
        {
            return GetFallbackSuggestions(ingredients);
        }
    }

    private static List<RecipeSuggestion> GetFallbackSuggestions(List<string> ingredients) =>
    [
        new RecipeSuggestion(
            "Quick Stir Fry",
            $"Use up {string.Join(", ", ingredients.Take(3))} in a quick stir fry with soy sauce and garlic.",
            "",
            ingredients.Take(3).ToList()
        ),
        new RecipeSuggestion(
            "Simple Soup",
            $"Combine {string.Join(", ", ingredients.Take(4))} in a broth for a hearty soup.",
            "",
            ingredients.Take(4).ToList()
        )
    ];
}

// Spoonacular API response shape
file record SpoonacularRecipe(int Id, string Title, int UsedIngredientCount, List<SpoonacularIngredient> UsedIngredients);
file record SpoonacularIngredient(int Id, string Name);
