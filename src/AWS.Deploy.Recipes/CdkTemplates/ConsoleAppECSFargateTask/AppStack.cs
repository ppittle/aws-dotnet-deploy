using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ECS.Patterns;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.IAM;
using ConsoleAppEcsFargateTask.Configurations;
using Schedule = Amazon.CDK.AWS.ApplicationAutoScaling.Schedule;

namespace ConsoleAppEcsFargateTask
{
    public class AppStack : Stack
    {
        /// <summary>
        /// Tag key of the CloudFormation stack
        /// used to uniquely identify a stack that is deployed by aws-dotnet-deploy
        /// </summary>
        private const string STACK_TAG_KEY = "StackTagKey-Placeholder";

        internal AppStack(Construct scope, string id, Configuration configuration, IStackProps props = null) : base(scope, id, props)
        {
            Tags.SetTag(STACK_TAG_KEY, "true");

            IVpc vpc;
            if (configuration.Vpc.IsDefault)
            {
                vpc = Vpc.FromLookup(this, "Vpc", new VpcLookupOptions
                {
                    IsDefault = true
                });
            }
            else if (configuration.Vpc.CreateNew)
            {
                vpc = new Vpc(this, "Vpc", new VpcProps
                {
                    MaxAzs = 2
                });
            }
            else
            {
                vpc = Vpc.FromLookup(this, "Vpc", new VpcLookupOptions
                {
                    VpcId = configuration.Vpc.VpcId
                });
            }


#if (UseExistingECSCluster)
            var cluster = Cluster.FromClusterAttributes(this, "Cluster", new ClusterAttributes
            {
                ClusterArn = configuration.ExistingClusterArn,
                // ClusterName is required field, but is ignored
                ClusterName = ""
                SecurityGroups = new ISecurityGroup[0],
                Vpc = vpc
            });
#else
            var cluster = new Cluster(this, "Cluster", new ClusterProps
            {
                Vpc = vpc,
                ClusterName = configuration.NewClusterName
            });
#endif

            IRole executionRole;
            if (configuration.ApplicationIAMRole.CreateNew)
            {
                executionRole = new Role(this, "ExecutionRole", new RoleProps
                {
                    AssumedBy = new ServicePrincipal("ecs-tasks.amazonaws.com"),
                    ManagedPolicies = new[]
                    {
                        ManagedPolicy.FromAwsManagedPolicyName("service-role/AmazonECSTaskExecutionRolePolicy"),
                    }
                });
            }
            else
            {
                executionRole = Role.FromRoleArn(this, "ExecutionRole", configuration.ApplicationIAMRole.RoleArn, new FromRoleArnOptions {
                    Mutable = false
                });
            }

            var taskDefinition = new FargateTaskDefinition(this, "TaskDefinition", new FargateTaskDefinitionProps
            {
                ExecutionRole = executionRole,
                Cpu = configuration.CpuLimit,
                MemoryLimitMiB = configuration.MemoryLimit
            });

            var logging = new AwsLogDriver(new AwsLogDriverProps
            {
                StreamPrefix = configuration.StackName
            });

            var dockerExecutionDirectory = @"DockerExecutionDirectory-Placeholder";
            if (string.IsNullOrEmpty(dockerExecutionDirectory))
            {
                if (string.IsNullOrEmpty(configuration.ProjectSolutionPath))
                {
                    dockerExecutionDirectory = new FileInfo(configuration.DockerfileDirectory).FullName;
                }
                else
                {
                    dockerExecutionDirectory = new FileInfo(configuration.ProjectSolutionPath).Directory.FullName;
                }
            }
            var relativePath = Path.GetRelativePath(dockerExecutionDirectory, configuration.DockerfileDirectory);
            var container = taskDefinition.AddContainer("Container", new ContainerDefinitionOptions
            {
                Image = ContainerImage.FromAsset(dockerExecutionDirectory, new AssetImageProps
                {
                    File = Path.Combine(relativePath, configuration.DockerfileName),
#if (AddDockerBuildArgs)
                    BuildArgs = GetDockerBuildArgs("DockerBuildArgs-Placeholder")
#endif
                }),
                Logging = logging,
                Environment = configuration.EnvironmentVariables.DecodeJsonDictionary()
            });

            var portMappings =
                configuration.PortMappings.DecodeJsonDictionary()
                    .Where(x => int.TryParse(x.Key, out _) && int.TryParse(x.Value, out _))
                    .Select(x => new PortMapping
                    {
                        HostPort = int.Parse(x.Key),
                        ContainerPort = int.Parse(x.Value)
                    })
                    .Cast<IPortMapping>()
                    .ToArray();
            container.AddPortMappings(portMappings);

            if (!string.IsNullOrEmpty(configuration.Schedule))
            {
                new ScheduledFargateTask(this,
                    "FargateScheduledTask",
                    new ScheduledFargateTaskProps
                    {
                        Cluster = cluster,
                        DesiredTaskCount = configuration.DesiredTaskCount,
                        Schedule = Schedule.Expression(configuration.Schedule),
                        Vpc = vpc,

                        ScheduledFargateTaskDefinitionOptions = new ScheduledFargateTaskDefinitionOptions
                        {
                            TaskDefinition = taskDefinition
                        }
                    });
            }
        }

#if (AddDockerBuildArgs)
        private Dictionary<string, string> GetDockerBuildArgs(string buildArgsString)
        {
            return buildArgsString
                .Split(',')
                .Where(x => x.Contains("="))
                .ToDictionary(
                    k => k.Split('=')[0],
                    v => v.Split('=')[1]
                );
        }
#endif
    }

    public static class StringExtensions
    {
        public static Dictionary<string, string> DecodeJsonDictionary(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return new Dictionary<string, string>();

            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(s);
        }
    }
}
