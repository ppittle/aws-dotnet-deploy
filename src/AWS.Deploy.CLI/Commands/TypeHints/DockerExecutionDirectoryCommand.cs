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
        private readonly IOptionSettingHandler _optionSettingHandler;

        public DockerExecutionDirectoryCommand(IConsoleUtilities consoleUtilities, IDirectoryManager directoryManager, IOptionSettingHandler optionSettingHandler)
        {
            _consoleUtilities = consoleUtilities;
            _directoryManager = directoryManager;
            _optionSettingHandler = optionSettingHandler;
        }

        public Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting) => Task.FromResult<List<TypeHintResource>?>(null);

        public Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var settingValue = _consoleUtilities
                .AskUserForValue(
                    string.Empty,
                    _optionSettingHandler.GetOptionSettingValue<string>(recommendation, optionSetting),
                    allowEmpty: true,
                    resetValue: _optionSettingHandler.GetOptionSettingDefaultValue<string>(recommendation, optionSetting) ?? "",
                    validators: executionDirectory => ValidateExecutionDirectory(executionDirectory));

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
            var resultString = ValidateExecutionDirectory(executionDirectory);
            if (!string.IsNullOrEmpty(resultString))
                throw new InvalidOverrideValueException(DeployToolErrorCode.InvalidDockerExecutionDirectory, resultString);
            recommendation.DeploymentBundle.DockerExecutionDirectory = executionDirectory;
        }

        private string ValidateExecutionDirectory(string executionDirectory)
        {
            if (!string.IsNullOrEmpty(executionDirectory) && !_directoryManager.Exists(executionDirectory))
                return "The directory specified for Docker execution does not exist.";
            else
                return "";
        }
    }
}
