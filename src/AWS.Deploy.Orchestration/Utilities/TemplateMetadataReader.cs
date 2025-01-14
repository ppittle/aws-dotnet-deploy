// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.CloudFormation.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Recipes.CDK.Common;
using Newtonsoft.Json;
using YamlDotNet.RepresentationModel;

namespace AWS.Deploy.Orchestration.Utilities
{
    public interface ITemplateMetadataReader
    {
        /// <summary>
        /// For a given Cloud Application loads the metadata for it. This includes the settings used to deploy and the recipe information.
        /// </summary>
        Task<CloudApplicationMetadata> LoadCloudApplicationMetadata(string cloudApplication);
    }

    /// <summary>
    /// A class for reading the metadata section of an CloudFormation template to pull out the AWS .NET deployment tool settings.
    /// </summary>
    public class TemplateMetadataReader : ITemplateMetadataReader
    {
        private readonly IAWSClientFactory _awsClientFactory;

        public TemplateMetadataReader(IAWSClientFactory awsClientFactory)
        {
            _awsClientFactory = awsClientFactory;
        }

        public async Task<CloudApplicationMetadata> LoadCloudApplicationMetadata(string cloudApplication)
        {
            using var client = _awsClientFactory.GetAWSClient<Amazon.CloudFormation.IAmazonCloudFormation>();

            var request = new GetTemplateRequest
            {
                StackName = cloudApplication
            };

            var response = await client.GetTemplateAsync(request);

            if(IsJsonCFTemplate(response.TemplateBody))
                return ReadSettingsFromJSONCFTemplate(response.TemplateBody);
            else
                return ReadSettingsFromYAMLCFTemplate(response.TemplateBody);
        }

        /// <summary>
        /// Read the AWS .NET deployment tool metadata from the CloudFormation template which is in JSON format.
        /// </summary>
        /// <returns></returns>
        private static CloudApplicationMetadata ReadSettingsFromJSONCFTemplate(string templateBody)
        {
            try
            {
                var cfTemplate = JsonConvert.DeserializeObject<CFTemplate>(templateBody);

                var cloudApplicationMetadata = new CloudApplicationMetadata(
                    cfTemplate?.Metadata?[Constants.CloudFormationIdentifier.STACK_METADATA_RECIPE_ID] ??
                        throw new Exception("Error parsing existing application's metadata to retrieve Recipe ID."),
                    cfTemplate?.Metadata?[Constants.CloudFormationIdentifier.STACK_METADATA_RECIPE_VERSION] ??
                        throw new Exception("Error parsing existing application's metadata to retrieve Recipe Version.")
                    );

                var jsonString = cfTemplate.Metadata[Constants.CloudFormationIdentifier.STACK_METADATA_SETTINGS];
                cloudApplicationMetadata.Settings = JsonConvert.DeserializeObject<IDictionary<string, object>>(jsonString ?? "") ?? new Dictionary<string, object>();

                return cloudApplicationMetadata;
            }
            catch (Exception e)
            {
                throw new ParsingExistingCloudApplicationMetadataException(DeployToolErrorCode.ErrorParsingApplicationMetadata, "Error parsing existing application's metadata", e);
            }
        }

        /// <summary>
        /// Read the AWS .NET deployment tool metadata from the CloudFormation template which is in YAML format.
        /// </summary>
        /// <returns></returns>
        private static CloudApplicationMetadata ReadSettingsFromYAMLCFTemplate(string templateBody)
        {
            try
            {
                var metadataSection = ExtractMetadataSection(templateBody);

                var yamlMetadata = new YamlStream();
                using var reader = new StringReader(metadataSection);
                yamlMetadata.Load(reader);
                var root = (YamlMappingNode)yamlMetadata.Documents[0].RootNode;
                var metadataNode = (YamlMappingNode)root.Children[new YamlScalarNode("Metadata")];

                var cloudApplicationMetadata = new CloudApplicationMetadata(
                    ((YamlScalarNode)metadataNode.Children[new YamlScalarNode(Constants.CloudFormationIdentifier.STACK_METADATA_RECIPE_ID)]).Value ??
                        throw new Exception("Error parsing existing application's metadata to retrieve Recipe ID."),
                    ((YamlScalarNode)metadataNode.Children[new YamlScalarNode(Constants.CloudFormationIdentifier.STACK_METADATA_RECIPE_VERSION)]).Value ??
                        throw new Exception("Error parsing existing application's metadata to retrieve Recipe Version.")
                    );

                var jsonString = ((YamlScalarNode)metadataNode.Children[new YamlScalarNode(Constants.CloudFormationIdentifier.STACK_METADATA_SETTINGS)]).Value;
                cloudApplicationMetadata.Settings = JsonConvert.DeserializeObject<IDictionary<string, object>>(jsonString ?? "") ?? new Dictionary<string, object>();

                return cloudApplicationMetadata;
            }
            catch(Exception e)
            {
                throw new ParsingExistingCloudApplicationMetadataException(DeployToolErrorCode.ErrorParsingApplicationMetadata, "Error parsing existing application's metadata", e);
            }
        }

        /// <summary>
        /// YamlDotNet does not like CloudFormation short hand notation. To avoid getting any parse failures due to use of the short hand notation
        /// using string parsing to extract just the Metadata section from the template.
        /// </summary>
        /// <returns></returns>
        private static string ExtractMetadataSection(string templateBody)
        {
            var builder = new StringBuilder();
            bool inMetadata = false;
            using var reader = new StringReader(templateBody);
            string? line;
            while((line = reader.ReadLine()) != null)
            {
                if(!inMetadata)
                {
                    // See if we found the start of the Metadata section
                    if(line.StartsWith("Metadata:"))
                    {
                        builder.AppendLine(line);
                        inMetadata = true;
                    }
                }
                else
                {
                    // See if we have found the next top level node signaling the end of the Metadata section
                    if (line.Length > 0 && char.IsLetterOrDigit(line[0]))
                    {
                        break;
                    }

                    builder.AppendLine(line);
                }
            }

            return builder.ToString();
        }

        private bool IsJsonCFTemplate(string templateBody)
        {
            try
            {
                JsonConvert.DeserializeObject(templateBody);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class CFTemplate
    {
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
