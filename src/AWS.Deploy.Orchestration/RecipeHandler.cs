// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Recipes;
using Newtonsoft.Json;

namespace AWS.Deploy.Orchestration
{
    public class RecipeHandler : IRecipeHandler
    {
        private readonly ICustomRecipeLocator _customRecipeLocator;

        public RecipeHandler(ICustomRecipeLocator customRecipeLocator)
        {
            _customRecipeLocator = customRecipeLocator;
        }

        public async Task<List<RecipeDefinition>> GetRecipeDefinitions(ProjectDefinition? projectDefinition, List<string>? recipeDefinitionPaths = null)
        {
            recipeDefinitionPaths ??= new List<string>();
            recipeDefinitionPaths.Add(RecipeLocator.FindRecipeDefinitionsPath());
            if(projectDefinition != null)
            {
                var targetApplicationFullPath = new DirectoryInfo(projectDefinition.ProjectPath).FullName;
                var solutionDirectoryPath = !string.IsNullOrEmpty(projectDefinition.ProjectSolutionPath) ?
                    new DirectoryInfo(projectDefinition.ProjectSolutionPath).Parent.FullName : string.Empty;

                var customPaths = await _customRecipeLocator.LocateCustomRecipePaths(targetApplicationFullPath, solutionDirectoryPath);
                recipeDefinitionPaths = recipeDefinitionPaths.Union(customPaths).ToList();
            }

            var recipeDefinitions = new List<RecipeDefinition>();
            var uniqueRecipeId = new HashSet<string>();

            try
            {
                foreach(var recipeDefinitionsPath in recipeDefinitionPaths)
                {
                    foreach (var recipeDefinitionFile in Directory.GetFiles(recipeDefinitionsPath, "*.recipe", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            var content = File.ReadAllText(recipeDefinitionFile);
                            var definition = JsonConvert.DeserializeObject<RecipeDefinition>(content);
                            if (definition == null)
                                throw new FailedToDeserializeException(DeployToolErrorCode.FailedToDeserializeRecipe, $"Failed to Deserialize Recipe Definition [{recipeDefinitionFile}]");
                            definition.RecipePath = recipeDefinitionFile;
                            if (!uniqueRecipeId.Contains(definition.Id))
                            {
                                recipeDefinitions.Add(definition);
                                uniqueRecipeId.Add(definition.Id);
                            }
                        }
                        catch (Exception e)
                        {
                            throw new FailedToDeserializeException(DeployToolErrorCode.FailedToDeserializeRecipe, $"Failed to Deserialize Recipe Definition [{recipeDefinitionFile}]: {e.Message}", e);
                        }
                    }
                }
            }
            catch(IOException)
            {
                throw new NoRecipeDefinitionsFoundException(DeployToolErrorCode.FailedToFindRecipeDefinitions, "Failed to find recipe definitions");
            }

            return recipeDefinitions;
        }
    }
}
