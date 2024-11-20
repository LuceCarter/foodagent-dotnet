using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace FoodAgentDotNet.Plugins;

#pragma warning disable
    public class IngredientsPlugin
    {
        [KernelFunction, Description("Get a list of available ingredients")]
        public static string GetIngredientsFromCupboard()
        {
            // Ensures that the file path functions across all operating systems.
            string baseDir = AppContext.BaseDirectory;
            string projectRoot = Path.Combine(baseDir, "..", "..", "..");
            string filePath = Path.Combine(projectRoot, "Data", "cupboardinventory.txt");
            return File.ReadAllText(filePath);
        }
    }

