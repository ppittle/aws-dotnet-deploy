// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.EC2.Model;
using AWS.Deploy.Common;
using AWS.Deploy.Common.Data;
using AWS.Deploy.Common.Recipes;
using AWS.Deploy.Common.TypeHintData;
using AWS.Deploy.Orchestration.Data;

namespace AWS.Deploy.CLI.Commands.TypeHints
{
    public class ExistingVpcCommand : ITypeHintCommand
    {
        private readonly IAWSResourceQueryer _awsResourceQueryer;
        private readonly IConsoleUtilities _consoleUtilities;
        private readonly IOptionSettingHandler _optionSettingHandler;

        public ExistingVpcCommand(IAWSResourceQueryer awsResourceQueryer, IConsoleUtilities consoleUtilities, IOptionSettingHandler optionSettingHandler)
        {
            _awsResourceQueryer = awsResourceQueryer;
            _consoleUtilities = consoleUtilities;
            _optionSettingHandler = optionSettingHandler;
        }

        private async Task<List<Vpc>> GetData()
        {
            return await _awsResourceQueryer.GetListOfVpcs();
        }

        public async Task<List<TypeHintResource>?> GetResources(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var vpcs = await GetData();
            return vpcs.ToDictionary(x => x.VpcId, x =>
            {
                var name = x.Tags?.FirstOrDefault(x => x.Key == "Name")?.Value ?? string.Empty;
                var namePart =
                    string.IsNullOrEmpty(name)
                        ? ""
                        : $" ({name}) ";

                var isDefaultPart =
                    x.IsDefault
                        ? " *** Account Default VPC ***"
                        : "";

                return $"{x.VpcId}{namePart}{isDefaultPart}";
            }).Select(x => new TypeHintResource(x.Key, x.Value)).ToList();
        }

        public async Task<object> Execute(Recommendation recommendation, OptionSettingItem optionSetting)
        {
            var currentVpcValue = _optionSettingHandler.GetOptionSettingValue<string>(recommendation, optionSetting);
            var vpcs = await GetData();

            var userInputConfig = new UserInputConfiguration<Vpc>(
                idSelector: vpc => vpc.VpcId,
                displaySelector: vpc =>
                {
                    var name = vpc.Tags?.FirstOrDefault(x => x.Key == "Name")?.Value ?? string.Empty;
                    var namePart =
                        string.IsNullOrEmpty(name)
                            ? ""
                            : $" ({name}) ";

                    var isDefaultPart =
                        vpc.IsDefault
                            ? " *** Account Default VPC ***"
                            : "";

                    return $"{vpc.VpcId}{namePart}{isDefaultPart}";
                },
                defaultSelector: vpc =>
                    !string.IsNullOrEmpty(currentVpcValue)
                        ? vpc.VpcId == currentVpcValue
                        : vpc.IsDefault)
            {
                CanBeEmpty = true,
                EmptyOption = true,
                CreateNew = false
            };

            var userResponse = _consoleUtilities.AskUserToChooseOrCreateNew(
                vpcs,
                "Select a VPC",
                userInputConfig);

            return userResponse.SelectedOption?.VpcId ?? string.Empty;
        }
    }
}
