// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.Recipes.Validation;
using Moq;
using Xunit;

namespace AWS.Deploy.CLI.Common.UnitTests.Recipes.Validation
{
    /// <summary>
    /// Tests for the recipe-level validation between the Dockerfile path and
    /// docker execution recipe options, <see cref="DockerfilePathValidator"/>
    /// </summary>
    public class DockerfilePathValidationTests
    {
        private readonly RecipeDefinition _recipeDefinition;

        public DockerfilePathValidationTests()
        {
            _recipeDefinition = new Mock<RecipeDefinition>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DeploymentTypes>(),
                It.IsAny<DeploymentBundleTypes>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()).Object;
        }

        /// <summary>
        /// We generate a Dockerfile later if one isn't specified, so not invalid at this point
        /// </summary>
        [Fact]
        public void MissingDockerFileValid()
        {
            var dockerfilePath = "";
            var dockerExecutionDirectory = Path.Combine("C", "project");

            DockerfilePathValidationHelper(dockerfilePath, dockerExecutionDirectory, true);
        }

        /// <summary>
        /// We compute the execution directory later if one isn't specified, so not invalid at this point
        /// </summary>
        [Fact]
        public void MissingDockerExecutionDirectoryValid()
        {
            var dockerfilePath = Path.Combine("C", "project", "Dockerfile");
            var dockerExecutionDirectory = "";

            DockerfilePathValidationHelper(dockerfilePath, dockerExecutionDirectory, true);
        }

        /// <summary>
        /// Dockerfile is in the execution directory
        /// </summary>
        [Fact]
        public void DockerfileInExecutionDirectoryValid()
        {
            var dockerfilePath = Path.Combine("C", "project", "Dockerfile");
            var dockerExecutionDirectory = Path.Combine("C", "project");

            DockerfilePathValidationHelper(dockerfilePath, dockerExecutionDirectory, true);
        }

        /// <summary>
        /// Dockerfile is further down in execution directory
        /// </summary>
        [Fact]
        public void DockerfileNestedInExecutionDirectoryValid()
        {
            var dockerfilePath = Path.Combine("C", "project", "child", "Dockerfile");
            var dockerExecutionDirectory = Path.Combine("C", "project");

            DockerfilePathValidationHelper(dockerfilePath, dockerExecutionDirectory, true);
        }

        /// <summary>
        /// Dockerfile is outside of the execution directory, which is invalid
        /// </summary>
        [Fact]
        public void DockerfileNestedInExecutionDirectoryInvalid()
        {
            var dockerfilePath = Path.Combine("C", "project", "Dockerfile");
            var dockerExecutionDirectory = Path.Combine("C", "foo");

            DockerfilePathValidationHelper(dockerfilePath, dockerExecutionDirectory, false);
        }

        private void DockerfilePathValidationHelper(string dockerfilePath, string dockerExecutionDirectory, bool expectedToBeValid)
        {
            var validator = new DockerfilePathValidator();
            var options = new List<OptionSettingItem>()
            {
                new OptionSettingItem("DockerfilePath", "", "")
            };
            var recommendation = new Recommendation(_recipeDefinition, null, options, 100, new Dictionary<string, string>());

            recommendation.DeploymentBundle.DockerExecutionDirectory = dockerExecutionDirectory;
            recommendation.GetOptionSetting("DockerfilePath").SetValueOverride(dockerfilePath, recommendation);

            var validationResult = validator.Validate(recommendation, null);

            Assert.Equal(expectedToBeValid, validationResult.IsValid);
        }
    }
}
