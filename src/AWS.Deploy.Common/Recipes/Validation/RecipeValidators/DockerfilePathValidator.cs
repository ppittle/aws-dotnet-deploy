// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using AWS.Deploy.Common.IO;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// This validates that the Dockerfile is within the build context.
    ///
    /// Per https://docs.docker.com/engine/reference/commandline/build/#text-files
    /// "The path must be to a file within the build context."
    /// </summary>
    public class DockerfilePathValidator : IRecipeValidator
    {
        public ValidationResult Validate(Recommendation recommendation, IDeployToolValidationContext deployValidationContext)
        {
            IDirectoryManager directoryManager = new DirectoryManager();

            // Dockerfile uses the generic FilePath typehint, so we must load the latest value from the option setting.
            // We also assume that if we detected a Dockerfile automatically it was set here via the replacement token
            var dockerfilePath = recommendation.GetOptionSettingValue<string>(recommendation.GetOptionSetting("DockerfilePath"));

            // Docker execution directory has its own typehint, which sets the value here
            var dockerExecutionDirectory = recommendation.DeploymentBundle.DockerExecutionDirectory;

            // We're only checking the interaction here against a user-specified file and execution directory,
            // it's still possible that we generate a dockerfile and/or compute the execution directory later.
            if (dockerfilePath == string.Empty || dockerExecutionDirectory == string.Empty)
            {
                return ValidationResult.Valid();
            }

            if (!directoryManager.ExistsInsideDirectory(dockerExecutionDirectory, dockerfilePath))
            {
                return ValidationResult.Failed($"The specified Dockerfile \"{dockerfilePath}\" is not located within the specified Docker execution directory \"{dockerExecutionDirectory}\"");
            }

            return ValidationResult.Valid();
        }
    }
}
