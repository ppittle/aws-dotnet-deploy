// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.\r
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// Checks for existing resources that might conflict with the deployment.
    /// </summary>
    public class ExistingResourcesValidator : IRecipeValidator
    {
        private readonly IOptionSettingHandler _optionSettingHandler;
        private readonly IValidatorFactory _validatorFactory;

        public ExistingResourcesValidator(IOptionSettingHandler optionSettingHandler, IValidatorFactory validatorFactory)
        {
            _optionSettingHandler = optionSettingHandler;
            _validatorFactory = validatorFactory;
        }

        /// <inheritdoc cref="ExistingResourcesValidator"/>
        public async Task<ValidationResult> Validate(Recommendation recommendation, IDeployToolValidationContext deployValidationContext)
        {
            if (!recommendation.IsExistingCloudApplication)
            {
                return await ValidateOptionSettings(recommendation, recommendation.Recipe.OptionSettings);
            }

            return ValidationResult.Valid();
        }

        private async Task<ValidationResult> ValidateOptionSettings(Recommendation recommendation, List<OptionSettingItem> optionSettings)
        {
            foreach (var optionSetting in optionSettings)
            {
                var validators = _validatorFactory.BuildValidators(optionSetting, validator => validator.ValidatorType == OptionSettingItemValidatorList.ExistingResource);
                foreach (var validator in validators)
                {
                    var optionSettingValue = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, optionSetting);
                    var result = await validator.Validate(optionSettingValue);
                    if (!result.IsValid)
                        return result;
                }

                var childValidation = await ValidateOptionSettings(recommendation, optionSetting.ChildOptionSettings);
                if (!childValidation.IsValid)
                    return childValidation;
            }

            return ValidationResult.Valid();
        }
    }
}
