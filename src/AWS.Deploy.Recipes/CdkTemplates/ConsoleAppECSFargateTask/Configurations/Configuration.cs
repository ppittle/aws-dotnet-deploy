// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;

namespace ConsoleAppEcsFargateTask.Configurations
{
    public class Configuration
    {
        /// <summary>
        /// The name of the CloudFormation Stack to create or update.
        /// </summary>
        public string StackName { get; set; }

        /// <summary>
        /// The path of csproj file to be deployed.
        /// </summary>
        public string ProjectPath { get; set; }

        /// <summary>
        /// The path of sln file to be deployed.
        /// </summary>
        public string ProjectSolutionPath { get; set; }

        /// <summary>
        /// The path of directory that contains the Dockerfile.
        /// </summary>
        public string DockerfileDirectory { get; set; }

        /// <summary>
        /// The file name of the Dockerfile.
        /// </summary>
        public string DockerfileName { get; set; } = "Dockerfile";

        /// <summary>
        /// The Identity and Access Management Role that provides AWS credentials to the application to access AWS services.
        /// </summary>
        public IAMRoleConfiguration ApplicationIAMRole { get; set; }

        /// <summary>
        /// The schedule or rate (frequency) that determines when CloudWatch Events runs the rule.
        /// </summary>
        public string Schedule { get; set; }

        /// <inheritdoc cref="ClusterProps.ClusterName"/>
        /// <remarks>
        /// This is only consumed if the deployment is configured
        /// to use a new cluster.  Otherwise, <see cref="ExistingClusterArn"/>
        /// will be used.
        /// </remarks>
        public string NewClusterName { get; set; }

        /// <inheritdoc cref="ClusterAttributes.ClusterName"/>
        /// <remarks>
        /// This is only consumed if the deployment is configured
        /// to use an existing cluster.  Otherwise, <see cref="NewClusterName"/>
        /// will be used.
        /// </remarks>
        public string ExistingClusterArn { get; set; }

        /// <inheritdoc cref="FargateTaskDefinitionProps.Cpu"/>
        public double? CpuLimit { get; set; }
        
        /// <inheritdoc cref="FargateTaskDefinitionProps.MemoryLimitMiB"/>
        public double? MemoryLimit { get; set; }
        
        /// <inheritdoc cref="ScheduledTaskBase.DesiredTaskCount"/>
        public double? DesiredTaskCount { get; set; }

        /// Json Encoded Dictionary of Environment Variables that will plug into <see cref="ContainerDefinition.PortMappings" />
        public string PortMappings { get; set; }

        /// Json Encoded Dictionary of Environment Variables that will plug into <see cref="ContainerDefinitionOptions.Environment" />
        public string EnvironmentVariables { get; set; }        

        /// <summary>
        /// Virtual Private Cloud to launch container instance into a virtual network.
        /// </summary>
        public VpcConfiguration Vpc { get; set; }
    }
}
