using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.IAM;
using System;
using System.IO;
using Protocol = Amazon.CDK.AWS.ECS.Protocol;

namespace AspNetAppEcsFargate
{
    public class AppStack : Stack
    {
        internal AppStack(Construct scope, string id, Configuration configuration, IStackProps props = null) : base(scope, id, props)
        {
            var vpc = new Vpc(this, "Vpc", new VpcProps
            {
                MaxAzs = 2
            });

            var cluster = new Cluster(this, "Cluster", new ClusterProps
            {
                Vpc = vpc,
                ClusterName = configuration.ClusterName
            });

            var executionRole = new Role(this, "ExecutionRole", new RoleProps
            {
                AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
                RoleName = configuration.ApplicationIAMRole,
                ManagedPolicies = new[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AmazonECSTaskExecutionRolePolicy"),
                }
            });

            var taskDefinition = new FargateTaskDefinition(this, "TaskDefinition", new FargateTaskDefinitionProps
            {
                ExecutionRole = executionRole,
            });

            var dockerExecutionDirectory = string.Empty;
            if (string.IsNullOrEmpty(configuration.ProjectSolutionPath))
            {
                dockerExecutionDirectory = new FileInfo(configuration.DockerfileDirectory).FullName;
            }
            else
            {
                dockerExecutionDirectory = new FileInfo(configuration.ProjectSolutionPath).Directory.FullName;
            }
            var relativePath = Path.GetRelativePath(dockerExecutionDirectory, configuration.DockerfileDirectory);
            var container = taskDefinition.AddContainer("Container", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromAsset(dockerExecutionDirectory, new AssetImageProps 
                { 
                    File = Path.Combine(relativePath, configuration.DockerfileName)
                })
            });

            container.AddPortMappings(new PortMapping
            {
                ContainerPort = 80,
                Protocol = Protocol.TCP
            });

            new ApplicationLoadBalancedFargateService(this, "FargateService", new ApplicationLoadBalancedFargateServiceProps
            {
                Cluster = cluster,
                TaskDefinition = taskDefinition,
                DesiredCount = configuration.DesiredCount,
                ServiceName = configuration.ECSServiceName
            });
        }
    }
}