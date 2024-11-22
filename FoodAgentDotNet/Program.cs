using FoodAgentDotNet.Plugins;
using FoodAgentDotNet.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using Microsoft.SemanticKernel.Memory;
using MongoDB.Driver;

string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new ArgumentNullException("Environment.GetEnvironmentVariable(\"OPENAI_API_KEY\")");
string modelName = Environment.GetEnvironmentVariable("OPENAI_MODEL_NAME") ?? "gpt-4o-mini";

var builder = Kernel.CreateBuilder();

builder.Services.AddOpenAIChatCompletion(
    modelName,
    apiKey);

var kernel = builder.Build();

kernel.ImportPluginFromType<IngredientsPlugin>();
kernel.ImportPluginFromType<RestaurantsPlugin>();


// The root directory of the project can change depending on IDE and Operating System so this code ensures it works regardless.
string baseDir = AppContext.BaseDirectory;
string projectRoot = Path.Combine(baseDir, "..", "..", "..");
var plugins = kernel.CreatePluginFromPromptDirectory(projectRoot + "/Prompts");

OpenAIPromptExecutionSettings settings = new()
{
    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
};

/*
 * You will need to uncomment this and run it the first time you run the application to generate the embeddings for the cuisine.
 * This is a one-time operation and can be commented out after the embeddings are generated.
 * It can take a few minutes to complete.
 * Note: You will need the sample dataset loaded into your cluster.
 */
//await GenerateEmbeddingsForCuisine();

Console.WriteLine("What would you like to make for dinner?");
var input = Console.ReadLine();

string ingredientsPrompt = @"This is a list of ingredients available to the user:
{{IngredientsPlugin.GetIngredientsFromCupboard}}

Based on their requested dish " + input + ", list what ingredients they are missing from their cupboard to make that meal and return just the list of missing ingredients. If they have similar items such as paste instead of a specific type of pasta, don't consider it missing";

var ingredientsResult = await kernel.InvokePromptAsync(ingredientsPrompt, new(settings));

// The response from the AI usually includes extra text so this just formats to the missing ingredients
var missing = ingredientsResult.ToString().ToLower()
    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
    .Where(line => line.StartsWith("- "))
    .ToList();

var cuisineResult = await kernel.InvokeAsync(
    plugins["GetCuisine"],
    new() { { "cuisine", input } }
);

if (missing.Count >= 5)
{
    string restaurantPrompt = @"This is the cuisine that the user requested: " + cuisineResult + @". Based on this cuisine, recommend a restaurant for the user to eat at. Include the name and address
    {{RestaurantsPlugin.GetRecommendedRestaurant}}";

    var kernelArguments = new KernelArguments(settings)
    {
        { "cuisine", cuisineResult }
    };

    var restaurantResult = await kernel.InvokePromptAsync(restaurantPrompt, kernelArguments);

    Console.WriteLine($"You have so many missing ingredients ({missing.Count}!), why bother? {restaurantResult}");
}
else if(missing.Count < 5 && missing.Count > 0)
{
    Console.WriteLine($"You have most of the ingredients to make {input}. You are missing: ");
    foreach (var ingredient in missing)
    {
        Console.WriteLine(ingredient);
    }
    string similarPrompt = @"The user requested to make " + input + @" but is missing some ingredients. Based on what they want to eat, suggest another meal that is similar from the " + cuisineResult + " cuisine they can make and tell them the name of it but do not return a full recipe";
    var similarResult = await kernel.InvokePromptAsync(similarPrompt, new(settings));

    Console.WriteLine(similarResult);
}

else {
    Console.WriteLine("You have all the ingredients to make " + input + "!");
    string recipePrompt = @"Find a recipe for making " + input;
    var recipeResult = await kernel.InvokePromptAsync(recipePrompt, new(settings));
    Console.WriteLine(recipeResult);
}

// Some features are still considered experimental so this suppresses warnings
#pragma warning disable
async Task GenerateEmbeddingsForCuisine()
{
    string mongoDBConnectionString = Environment.GetEnvironmentVariable("MONGODB_ATLAS_CONNECTION_STRING");
    MongoClientSettings mongoClientSettings = MongoClientSettings.FromConnectionString(mongoDBConnectionString);
    
    var mongoDBClient = new MongoClient(mongoClientSettings);
    
    // Ensure you have the sample dataset loaded into your cluster
    var database = mongoDBClient.GetDatabase("sample_restaurants");
    var collection = database.GetCollection<Restaurant>("restaurants");

    var mongoDBMemoryStore = new MongoDBMemoryStore(mongoDBConnectionString, database.DatabaseNamespace.DatabaseName);
    var memoryBuilder = new MemoryBuilder();
    
    memoryBuilder.WithOpenAITextEmbeddingGeneration(
        "text-embedding-3-small",
        apiKey
    );
    
    memoryBuilder.WithMemoryStore(mongoDBMemoryStore);
    var memory = memoryBuilder.Build();
    
    // This fetches and saves 1000 docouments into our memory store for a bigger sample but you can always adjust this number up or down.
    var restaurants = await collection.Find(r => true).Limit(1000).ToListAsync();

    foreach (var restaurant in restaurants)
    {
        try
        {
            await memory.SaveReferenceAsync(
                collection: "embedded_cuisines",
                description: restaurant.Name + " " + restaurant.Address.Building + " " + restaurant.Address.Street + " " + restaurant.Address.Zipcode,
                // text is the field on which the embeddings are generated
                text: restaurant.Cuisine,
                // externalId is the unique identifier for the document which in this context is more valuable as the name.
                externalId: restaurant.Name,
                externalSourceName: "Sample_Restaurants_Restaurants",
                additionalMetadata: restaurant.Cuisine
            );
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}