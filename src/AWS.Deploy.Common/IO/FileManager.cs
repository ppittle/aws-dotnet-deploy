// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AWS.Deploy.Common.Utilities;

namespace AWS.Deploy.Common.IO
{
    public interface IFileManager
    {
        /// <summary>
        /// Determines whether the specified file is at a valid path and exists.
        /// This can either be an absolute path or relative to the current working directory.
        /// </summary>
        /// <param name="path">The file to check</param>
        /// <returns>
        /// True if the path is valid, the caller has the required permissions,
        /// and path contains the name of an existing file
        /// </returns>
        bool Exists(string path);

        /// <summary>
        /// Determines whether the specified file is at a valid path and exists.
        /// This can either be an absolute path or relative to the given directory.
        /// </summary>
        /// <param name="path">The file to check</param>
        /// <param name="directory">Directory to consider the path as relative to</param>
        /// <returns>
        /// True if the path is valid, the caller has the required permissions,
        /// and path contains the name of an existing file
        /// </returns>
        bool Exists(string path, string directory);

        Task<string> ReadAllTextAsync(string path);
        Task<string[]> ReadAllLinesAsync(string path);
        Task WriteAllTextAsync(string filePath, string contents, CancellationToken cancellationToken = default);
        FileStream OpenRead(string filePath);
        string GetExtension(string filePath);
        long GetSizeInBytes(string filePath);
    }

    /// <summary>
    /// Wrapper for <see cref="File"/> class to allow mock-able behavior for static methods.
    /// </summary>
    public class FileManager : IFileManager
    {
        public bool Exists(string path) => IsFileValid(path);

        public bool Exists(string path, string directory)
        {
            if (Path.IsPathRooted(path))
            {
                return Exists(path);
            }
            else
            {
                return Exists(Path.Combine(directory, path));
            }
        }

        public Task<string> ReadAllTextAsync(string path) => File.ReadAllTextAsync(path);

        public Task<string[]> ReadAllLinesAsync(string path) => File.ReadAllLinesAsync(path);

        public Task WriteAllTextAsync(string filePath, string contents, CancellationToken cancellationToken) =>
            File.WriteAllTextAsync(filePath, contents, cancellationToken);

        public FileStream OpenRead(string filePath) => File.OpenRead(filePath);

        public string GetExtension(string filePath) => Path.GetExtension(filePath);

        public long GetSizeInBytes(string filePath) => new FileInfo(filePath).Length;

        private bool IsFileValid(string filePath)
        {
            if (!PathUtilities.IsPathValid(filePath))
                return false;

            if (!File.Exists(filePath))
                return false;

            return true;
        }
    }
}
