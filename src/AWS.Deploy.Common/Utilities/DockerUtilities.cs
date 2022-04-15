// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using AWS.Deploy.Common.IO;

namespace AWS.Deploy.Common.Utilities
{
    /// <summary>
    ///  Utility methods for working with a recommendation's Docker configuration
    /// </summary>
    public static class DockerUtilities
    {
        /// <summary>
        /// Gets the path of a Dockerfile if it exists at the default location: "{ProjectPath}/Dockerfile"
        /// </summary>
        /// <param name="recommendation">The selected recommendation settings used for deployment</param>
        /// <param name="fileManager">File manager, used for validating that the Dockerfile exists</param>
        /// <param name="dockerfilePath">Path to the Dockerfile, relative to the recommendation's project directory</param>
        /// <returns>True if the Dockerfile exists at the default location, false otherwise</returns>
        public static bool TryGetDefaultDockerfile(Recommendation recommendation, IFileManager? fileManager, out string dockerfilePath)
        {
            if (fileManager == null)
            {
                fileManager = new FileManager();
            }

            if (fileManager.Exists(Constants.Docker.DefaultDockerfileName, recommendation.GetProjectDirectory()))
            {
                // Set the default value to the OS-specific ".\Dockerfile"
                dockerfilePath = Path.Combine(".", Constants.Docker.DefaultDockerfileName);
                return true;
            }
            else
            {
                dockerfilePath = string.Empty;
                return false;
            }
        }

        /// <summary>
        /// Gets the path of a the project's Dockerfile if it exists, from either a user-specified or the default location
        /// </summary>
        /// <param name="recommendation">The selected recommendation settings used for deployment</param>
        /// <param name="fileManager">File manager, used for validating that the Dockerfile exists</param>
        /// <param name="dockerfilePath">Absolute path to a Dockerfile</param>
        /// <returns>True if a Dockerfile is specified for this deployment, false otherwise</returns>
        public static bool TryGetDockerfile(Recommendation recommendation, IFileManager? fileManager, out string dockerfilePath)
        {
            // Load the Dockerfile from the option setting. If one was found at the default location,
            // assume it would be set here via the replacement token at this point.
            var dockerFileOptionSetting = recommendation.GetOptionSetting("DockerfilePath");
            dockerfilePath = recommendation.GetOptionSettingValue<string>(dockerFileOptionSetting);

            if (!string.IsNullOrEmpty(dockerfilePath))
            {
                if (fileManager == null)
                {
                    fileManager = new FileManager();
                }

                // Double-check that it still exists in case it was move/deleted after being specified.
                if (fileManager.Exists(dockerfilePath, recommendation.GetProjectDirectory()))
                {
                    return true;
                }
                else
                {
                    dockerfilePath = string.Empty;
                    return false;
                }
            }
            else
            {
                // Check the default location again, for the case where a file was specified
                // in the option but we generated one right before calling docker build.
                var defaultExists = TryGetDefaultDockerfile(recommendation, fileManager, out dockerfilePath);
                return defaultExists;
            }
        }
    }
}
