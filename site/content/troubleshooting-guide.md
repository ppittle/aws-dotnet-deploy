
# Troubleshooting Guide

This guide outlines some failure scenarios and their possible solutions.

## Node.js not installed
AWS.Deploy.Tools relies on AWS CDK to provision resources for the user's cloud application. AWS CDK requires Node.js to be installed in the user's machine.

**Minimum Node.js version >= 10.13.0**

See [here](https://nodejs.org/en/download/) to install Node.js on your system.

## Docker related issues
AWS.Deploy.Tool requires Docker to perform containerized deployments.

The following recipes are container based:

 - ASP.NET Core App to Amazon ECS using Fargate
 - ASP.NET Core App to AWS App Runner
 - Service on Amazon ECS using Fargate
 - Scheduled Task on Amazon ECS using Fargate
 - Push Container Images to Amazon Elastic Container Registry

### Docker not installed
See [here](https://docs.docker.com/get-docker/) to install Docker for your operating system.

### Docker not running in Linux mode
If you are on a Windows operating system, it is likely that you are running Docker in a Windows container. AWS.Deploy.Tools requires Docker to be running in a Linux container.

See [here](https://docs.docker.com/desktop/windows/#switch-between-windows-and-linux-containers) to switch between Windows and Linux containers.

### Failed to push Docker Image
AWS.Deploy.Tools builds the Docker image and pushes it to Amazon Elastic Container Registry (Amazon ECR). 

If a user is missing the required IAM permissions to perform actions against ECR repositories, the deployment may fail the following error message:

```
Failed to push Docker Image
```

See [here](https://docs.aws.amazon.com/AmazonECR/latest/userguide/repository-policy-examples.html) for guidance on how to set IAM policy statements to allow actions on Amazon ECR repositories.

## Invalid project path provided

The CLI deployment command takes in an optional `--project-path` option.

For example:
```
dotnet aws deploy --project-path
```
The `--project-path` can be absolute or relative to the current working directory and must be one of the following:

 - A file path pointing to a `*.csproj` or `*.fsproj` file.
 - A directory path that contains a `*.csproj` or `*.fsproj` file.

If a `--project-path` option is not provided, then AWS.Deploy.Tools will look for a `*.csproj` or `*.fsproj` file in the current working directory.

## Failed to find compatible deployment recommendations
Behind the scenes, AWS.Deploy.Tools uses a recipe configuration file to provide an opinionated deployment experience. See [here](docs/features/recipe.md) to know more about recipes.

Recipe configurations target different AWS services and there may be incompatibilities between the chosen recipe and the user's .NET application.

The following table summarizes valid mappings between recipes and application types

| Recipe                                                     | Target SDK Type                                                                        | Target Framework                             | Docker required |
|------------------------------------------------------------|----------------------------------------------------------------------------------------|----------------------------------------------|-----------------|
| ASP.NET Core App to Amazon ECS using Fargate               | Microsoft.NET.Sdk.Web                                                                  | netcoreapp2.1, netcoreapp3.1, net5.0, net6.0 | Yes             |
| ASP.NET Core App to AWS App Runner                         | Microsoft.NET.Sdk.Web                                                                  | netcoreapp2.1, netcoreapp3.1, net5.0, net6.0 | Yes             |
| Service on Amazon ECS using Fargate                        | Microsoft.NET.Sdk                                                                      | netcoreapp2.1, netcoreapp3.1, net5.0, net6.0 | Yes             |
| Scheduled Task on Amazon ECS using Fargate                 | Microsoft.NET.Sdk                                                                      | netcoreapp2.1, netcoreapp3.1, net5.0, net6.0 | Yes             |
| Blazor WebAssembly App                                     | Microsoft.NET.Sdk.BlazorWebAssembly (for.NET 5.0+) Microsoft.NET.Sdk.Web (for.NET 3.1) | netstandard2.1                               | No              |
| ASP.NET Core App to AWS Elastic Beanstalk on Linux         | Microsoft.NET.Sdk.Web                                                                  | netcoreapp2.1, netcoreapp3.1, net5.0, net6.0 | No              |
| Push Container Images to Amazon Elastic Container Registry | N/A                                                                                    | N/A                                          | Yes             |

You need to ensure that your .NET application falls into one of the categories in order to leverage AWS.Deploy.Tools

Another reason why there are no recommendations generated is if your application `.csproj` file is using a variable for the `TargetFramework` property.


    <Project Sdk="Microsoft.NET.Sdk.Web">
	    <PropertyGroup>
		    <TargetFrameworkVersion>net5.0</TargetFrameworkVersion>
		    <TargetFramework>$(TargetFrameworkVersion)</TargetFramework>
	    </PropertyGroup>
	</Project>
    
No recommendations will be generated for the above `.csproj` file.
**This is a bug which we will address.** Meanwhile, provide explicit values for the `TargetFramework` property.

## Failed to create zip archive of the application
Non-container based deployments types (such as deployments to AWS Elastic Beanstalk) create a zip file of the artifacts produced by the `dotnet publish` command.

The zip command line utility is not installed by default on **Linux** based operating systems. If you are deploying using a non-container based option and encounter an error saying `failed to create a deployment bundle`, it is likely that zip is not installed on your system.

To install zip on Linux OS, run the following command:
```
$ sudo apt-get install zip
```

After installation, use the command to verify that zip was installed correctly.
```
$ zip
```


## Deployment failures related to JSON configuration file

AWS.Deploy.Tools allows for prompt-less deployments using a JSON configuration file. This workflow can easily be plugged into you CI/CD pipeline for automated deployments. It is possible that the configuration file has the wrong definition or the wrong format. 

See [here](docs/feature/config-file.md) for an example of a valid JSON configuration file.

## Insufficient IAM Permissions
Access to AWS is governed by IAM policies. They are a group of permissions which determine whether the request to an AWS resource/service is allowed or denied.

AWS.Deploy.Tools, internally uses a variety of different services to host your .NET application on AWS. If you an encounter an error saying `user is not authorized to perform action because no identity based policies allow it`, that means you need to add the corresponding permission to the IAM policy that is used by the current IAM role/user.

**Note: Exact wording for an insufficient permissions related errors may differ from the above**

* See [here](https://docs.aws.amazon.com/IAM/latest/UserGuide/tutorial_managed-policies.html) for a tutorial on how to create customer managed IAM policies.
* See [here](https://docs.aws.amazon.com/IAM/latest/UserGuide/troubleshoot_policies.html) for troubleshooting IAM policies.


