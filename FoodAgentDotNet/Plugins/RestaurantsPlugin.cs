using FoodAgentDotNet.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodAgentDotNet.Plugins;

#pragma warning disable
public class RestaurantsPlugin
{
    static readonly string mongoDBConnectionString = Environment.GetEnvironmentVariable("MONGODB_ATLAS_CONNECTION_STRING");
    static readonly string openAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    [KernelFunction, Description("Find a restaurant to eat at")]
    public static async Task<List<Restaurant>> GetRecommendedRestaurant(
           [Description("The cuisine to find a restaurant for")] string cuisine)
    {
        // Be sure to update the index name here if you chose to create an index with a different name.
        var mongoDBMemoryStore = new MongoDBMemoryStore(mongoDBConnectionString, "sample_restaurants", "restaurants_index");
        var memoryBuilder = new MemoryBuilder();
        memoryBuilder.WithOpenAITextEmbeddingGeneration(
            "text-embedding-3-small",
            openAIApiKey    );
        memoryBuilder.WithMemoryStore(mongoDBMemoryStore);
        var memory = memoryBuilder.Build();

        var restaurants = memory.SearchAsync(
            "embedded_cuisines",
            cuisine,
            limit: 5,
            minRelevanceScore: 0.5
            );

        List<Restaurant> recommendedRestaurants = new();

        await foreach(var restaurant in restaurants)
        {
            recommendedRestaurants.Add(new Restaurant
            {
                Name = restaurant.Metadata.Description,
                // We include the cuisine so that the AI has this information available to it
                Cuisine = restaurant.Metadata.AdditionalMetadata,
            });
        }
        return recommendedRestaurants;
    }



}