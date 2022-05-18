// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.CloudControlApi.Model;
using AWS.Deploy.Common.Data;

namespace AWS.Deploy.Common.Recipes.Validation
{
    public class ExistingResourceValidator : IOptionSettingItemValidator
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;

        public string? Type { get; set; }

        public ExistingResourceValidator(IAWSResourceQueryer awsResourceQueryer)
        {
            _awsResourceQueryer = awsResourceQueryer;
        }

        public async Task<ValidationResult> Validate(object input)
        {
            if (string.IsNullOrEmpty(Type))
                throw new MissingValidatorConfigurationException(DeployToolErrorCode.MissingValidatorConfiguration, $"The validator of type '{typeof(ExistingResourceValidator)}' is missng the configuration property '{nameof(Type)}'.");
            var resourceName = input.ToString();
            if (string.IsNullOrEmpty(resourceName))
                return ValidationResult.Failed($"The resource name is empty and cannot be validated.");

            switch (Type)
            {
                case "AWS::ElasticBeanstalk::Application":
                    var beanstalkApplications = await _awsResourceQueryer.ListOfElasticBeanstalkApplications(input.ToString());
                    if (beanstalkApplications.Any(x => x.ApplicationName.Equals(input.ToString())))
                        return ValidationResult.Failed($"An Elastic Beanstalk application already exists with the name '{input.ToString()}'.");
                    break;

                case "AWS::ElasticBeanstalk::Environment":
                    var beanstalkEnvironments = await _awsResourceQueryer.ListOfElasticBeanstalkEnvironments(environmentName: input.ToString());
                    if (beanstalkEnvironments.Any(x => x.EnvironmentName.Equals(input.ToString())))
                        return ValidationResult.Failed($"An Elastic Beanstalk environment already exists with the name '{input.ToString()}'.");
                    break;

                default:
                    try
                    {
                        var resource = await _awsResourceQueryer.GetCloudControlApiResource(Type, resourceName);
                        return ValidationResult.Failed($"A resource of type '{Type}' and name '{resourceName}' already exists.");
                    }
                    catch (ResourceQueryException ex) when (ex.InnerException is ResourceNotFoundException)
                    {
                        break;
                    }
            }

            return ValidationResult.Valid();
        }
    }
}
