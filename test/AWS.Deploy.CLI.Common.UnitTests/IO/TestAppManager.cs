// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AWS.Deploy.CLI.Common.UnitTests.Extensions;

namespace AWS.Deploy.CLI.Common.UnitTests.IO
{
    public class TestAppManager
    {
        public string GetProjectPath(string path)
        {
            var tempDir = GetTempDir();
            var parentDir = new DirectoryInfo("testapps");
            parentDir.CopyTo(tempDir, true);
            return Path.Combine(tempDir, path);
        }

        private string GetTempDir()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
    }
}
