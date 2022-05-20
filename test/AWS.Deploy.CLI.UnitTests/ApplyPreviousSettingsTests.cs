// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using AWS.Deploy.CLI.TypeHintResponses;
using AWS.Deploy.CLI.UnitTests.Utilities;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Recipes;
using AWS.Deploy.Orchestration;
using AWS.Deploy.Orchestration.RecommendationEngine;
using Moq;
using Newtonsoft.Json;
using Xunit;
using Assert = Should.Core.Assertions.Assert;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using AWS.Deploy.Common.DeploymentManifest;

namespace AWS.Deploy.CLI.UnitTests
{
    public class ApplyPreviousSettingsTests
    {
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly Orchestrator _orchestrator;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICustomRecipeLocator _customRecipeLocator;
        private readonly IDeploymentManifestEngine _deploymentManifestEngine;
        private readonly IOrchestratorInteractiveService _orchestratorInteractiveService;
        private readonly IDirectoryManager _directoryManager;
        private readonly IFileManager _fileManager;
        private readonly IRecipeHandler _recipeHandler;

        public ApplyPreviousSettingsTests()
        {
            _directoryManager = new DirectoryManager();
            _fileManager = new FileManager();
            _deploymentManifestEngine = new DeploymentManifestEngine(_directoryManager, _fileManager);
            _orchestratorInteractiveService = new TestToolOrchestratorInteractiveService();
            _customRecipeLocator = new CustomRecipeLocator(_deploymentManifestEngine, _orchestratorInteractiveService, _directoryManager);
            _recipeHandler = new RecipeHandler(_customRecipeLocator);
            _serviceProvider = new Mock<IServiceProvider>().Object;
            _optionSettingHandler = new OptionSettingHandler(new ValidatorFactory(_serviceProvider));
            _orchestrator = new Orchestrator(null, null, null, null, null, null, null, null, null, null, null, null, null, null, _optionSettingHandler);
        }

        private async Task<RecommendationEngine> BuildRecommendationEngine(string testProjectName)
        {
            var fullPath = SystemIOUtilities.ResolvePath(testProjectName);

            var parser = new ProjectDefinitionParser(_fileManager, _directoryManager);
            var awsCredentials = new Mock<AWSCredentials>();
            var session =  new OrchestratorSession(
                await parser.Parse(fullPath),
                awsCredentials.Object,
                "us-west-2",
                "123456789012")
            {
                AWSProfileName = "default"
            };

            return new RecommendationEngine(session, _recipeHandler);
        }

        [Theory]
        [InlineData(true, null)]
        [InlineData(false, "arn:aws:iam::123456789012:group/Developers")]
        public async Task ApplyApplicationIAMRolePreviousSettings(bool createNew, string roleArn)
        {
            var engine = await BuildRecommendationEngine("WebAppNoDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var beanstalkRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_BEANSTALK_RECIPE_ID);

            var roleArnValue = roleArn == null ? "null" : $"\"{roleArn}\"";

            var serializedSettings = @$"
            {{
                ""ApplicationIAMRole"": {{
                    ""RoleArn"": {roleArnValue},
                    ""CreateNew"": {createNew.ToString().ToLower()}
                }}
            }}";

            var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(serializedSettings);

            beanstalkRecommendation = _orchestrator.ApplyRecommendationPreviousSettings(beanstalkRecommendation, settings);

            var applicationIAMRoleOptionSetting = beanstalkRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("ApplicationIAMRole"));
            var typeHintResponse = _optionSettingHandler.GetOptionSettingValue<IAMRoleTypeHintResponse>(beanstalkRecommendation, applicationIAMRoleOptionSetting);

            Assert.Equal(roleArn, typeHintResponse.RoleArn);
            Assert.Equal(createNew, typeHintResponse.CreateNew);
        }

        [Theory]
        [InlineData(true, false, "")]
        [InlineData(false, true, "")]
        [InlineData(false, false, "vpc-88888888")]
        public async Task ApplyVpcPreviousSettings(bool isDefault, bool createNew, string vpcId)
        {
            var engine = await BuildRecommendationEngine("WebAppWithDockerFile");

            var recommendations = await engine.ComputeRecommendations();

            var fargateRecommendation = recommendations.First(r => r.Recipe.Id == Constants.ASPNET_CORE_ASPNET_CORE_FARGATE_RECIPE_ID);

            var vpcIdValue = string.IsNullOrEmpty(vpcId) ? "\"\"" : $"\"{vpcId}\"";

            var serializedSettings = @$"
            {{
                ""Vpc"": {{
                    ""IsDefault"": {isDefault.ToString().ToLower()},
                    ""CreateNew"": {createNew.ToString().ToLower()},
                    ""VpcId"": {vpcIdValue}
                }}
            }}";

            var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(serializedSettings);

            fargateRecommendation = _orchestrator.ApplyRecommendationPreviousSettings(fargateRecommendation, settings);

            var vpcOptionSetting = fargateRecommendation.Recipe.OptionSettings.First(optionSetting => optionSetting.Id.Equals("Vpc"));

            Assert.Equal(isDefault, _optionSettingHandler.GetOptionSettingValue(fargateRecommendation, vpcOptionSetting.ChildOptionSettings.First(optionSetting => optionSetting.Id.Equals("IsDefault"))));
            Assert.Equal(createNew, _optionSettingHandler.GetOptionSettingValue(fargateRecommendation, vpcOptionSetting.ChildOptionSettings.First(optionSetting => optionSetting.Id.Equals("CreateNew"))));
            Assert.Equal(vpcId, _optionSettingHandler.GetOptionSettingValue(fargateRecommendation, vpcOptionSetting.ChildOptionSettings.First(optionSetting => optionSetting.Id.Equals("VpcId"))));
        }
    }
}
