using FoodAgentDotNet.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.MongoDB;
using Microsoft.SemanticKernel.Memory;
using MongoDB.Driver;


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
        "<YOUR OPENAI APIKEY>"
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