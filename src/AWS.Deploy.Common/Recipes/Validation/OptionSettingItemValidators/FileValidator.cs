// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// Validates that a recipe or deployment bundle option with a FilePath typehint points to an actual file.
    /// This can either be an absolute path to the file or relative to the project path for the current recommendation.
    /// </summary>
    public class FileValidator : IOptionSettingItemValidator
    {
        public static readonly string defaultValidationFailedMessage = "The specified file does not exist";

        public string ValidationFailedMessage { get; set; } = defaultValidationFailedMessage;

        ValidationResult IOptionSettingItemValidator.Validate(object input, Recommendation recommendation)
        {
            IFileManager fileManager = new FileManager();
            var inputFilePath = input?.ToString() ?? string.Empty;

            // Allow the user to clear the option intentionally
            if (inputFilePath == string.Empty)
            {
                return ValidationResult.Valid();
            }

            // Otherwise if there is a value, verify that it points to an actual file
            if (fileManager.Exists(inputFilePath, recommendation.GetProjectDirectory()))
            {
                return ValidationResult.Valid();
            }
            else
            {
                return ValidationResult.Failed($"The specified file {inputFilePath} does not exist");
            }
        }
    }
}
