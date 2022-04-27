// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AWS.Deploy.Common;
using AWS.Deploy.Common.IO;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class DockerExecutionDirectoryCommand : ITypeHintCommand
    {
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IDirectoryManager _directoryManager;

        public DockerExecutionDirectoryCommand(IConsoleUtilities consoleUtilities, IDirectoryManager directoryManager)
        {
            _consoleUtilities = consoleUtilities;
            _directoryManager = directoryManager;
        }

        public Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting) => Task.FromResult<List<TypeHintResource>?>(null);

        public Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var settingValue = _consoleUtilities
                .AskUserForValue(
                    string.Empty,
                    recommendation.GetOptionSettingValue<string>(optionSetting),
                    allowEmpty: true,
                    resetValue: recommendation.GetOptionSettingDefaultValue<string>(optionSetting) ?? "",
                    validators: executionDirectory => ValidateExecutionDirectory(executionDirectory, recommendation));

            recommendation.DeploymentBundle.DockerExecutionDirectory = settingValue;
            return Task.FromResult<object>(settingValue);
        }

        /// <summary>
        /// This method will be invoked to set the Docker execution directory in the deployment bundle
        /// when it is specified as part of the user provided configuration file.
        /// </summary>
        /// <param name="recommendation">The selected recommendation settings used for deployment <see cref="Recommendation"/></param>
        /// <param name="executionDirectory">The directory specified for Docker execution.</param>
        public void OverrideValue(Recommendation recommendation, string executionDirectory)
        {
            var resultString = ValidateExecutionDirectory(executionDirectory, recommendation);
            if (!string.IsNullOrEmpty(resultString))
                throw new InvalidOverrideValueException(DeployToolErrorCode.InvalidDockerExecutionDirectory, resultString);
            recommendation.DeploymentBundle.DockerExecutionDirectory = executionDirectory;
        }

        /// <summary>
        /// Validates that the Docker execution directory exists as either an
        /// absolute path or a path relative to the project directory.
        /// </summary>
        /// <param name="executionDirectory">Proposed Docker execution directory</param>
        /// <param name="recommendation">The selected recommendation settings used for deployment</param>
        /// <returns>Empty string if the directory is valid, and error message if not</returns>
        private string ValidateExecutionDirectory(string executionDirectory, Recommendation recommendation)
        {
            if (!string.IsNullOrEmpty(executionDirectory) && !_directoryManager.Exists(executionDirectory, recommendation.GetProjectDirectory()))
                return $"The directory {executionDirectory} specified for Docker execution does not exist.";
            else
                return "";
        }
    }
}
