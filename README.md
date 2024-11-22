# Food AI Agent with Semantic Kernel and C#, OpenAI and MongoDB Atlas

This repo is a sample console application in .NET, showing how to use Semantic Kernel, OpenAI and MongoDB Atlas to create a simple AI Agent for querying missing ingredients to make a recipe, followed by the recpie, another recpie or a recommended restaurant to visit instead.

## Prerequisites
You will need a few things to run this locally:
1. OpenAI account and API Key
2. MongoDB Atlas cluster with sample dataset loaded (the free forever M0 tier is adequate)
3. MongoDB Atlas Connection String

You will need to add these keys to your environment variables to run the application.

E.G:
```bash
export OPENAI_API_KEY="<YOUR OPEN AI API KEY>"
```

## Things to know

The sample data available from MongoDB for restaurants does not have a field for embeddings yet. Thankfully, Semantic Kernel is able to generate embeddings for us using OpenAI. In ```Program.cs``` you will find a method called ```GenerateEmbeddingsForCuisine()```.
This needs to be called just ONCE, the first time you run the application. This will go ahead and fetch documents (currently set to 1000 documents) from the sample_restaurants database, restaurants collection, generate the embeddings and then save them in a collection called *embedded_cuisines* in the format that Semantic Kernel can work with.

## Running the application

1. Ensure you have added the required environment variables
2. Run ```dotnet build``` using the DotNET SDK or inside an IDE such as Visual Studio.
3. Run ```dotnet run``` to run the application.

