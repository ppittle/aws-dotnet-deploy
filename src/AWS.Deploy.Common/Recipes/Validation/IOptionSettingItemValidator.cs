// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

namespace AWS.Deploy.Common.Recipes.Validation
{
    /// <summary>
    /// This interface outlines the framework for OptionSettingItem validators.
    /// Validators such as <see cref="RegexValidator"/> implement this interface and provide custom validation logic
    /// on OptionSettingItems
    /// </summary>
    public interface IOptionSettingItemValidator
    {
        ValidationResult Validate(object input);
    }
}
