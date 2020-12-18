using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.TemplateEngine.Edge;
using Microsoft.TemplateEngine.Utils;
using Microsoft.TemplateEngine.Edge.TemplateUpdates;
using AWS.DeploymentCommon;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.IDE;
using Microsoft.TemplateEngine.Edge.Template;
using System.Threading.Tasks;
using System;

namespace AWS.Deploy.Orchestrator
{
    public class TemplateEngine
    {
        private const string HostIdentifier = "aws-net-deploy-template-generator";
        private const string HostVersion = "v1.0.0";
        private readonly Bootstrapper _bootstrapper;

        public TemplateEngine()
        {
            _bootstrapper = new Bootstrapper(CreateHost(), null, virtualizeConfiguration: true);
        }

        public async Task GenerateCDKProjectFromTemplate(Recommendation recommendation, string outputDirectory)
        {
            //The location of the base template that will be installed into the templating engine
            var cdkProjectTemplateDirectory = Path.Combine(Path.GetDirectoryName(recommendation.Recipe.RecipePath), recommendation.Recipe.CdkProjectTemplate);

            //Installing the base template into the templating engine to make it available for generation
            InstallTemplates(cdkProjectTemplateDirectory);

            //Looking up the installed template in the templating engine
            var template =
                _bootstrapper
                    .ListTemplates(
                        true,
                        WellKnownSearchFilters.NameFilter(recommendation.Recipe.CdkProjectTemplateId))
                    .FirstOrDefault()
                    ?.Info;

            //If the template is not found, throw an exception
            if (template == null)
                throw new Exception($"Failed to find a Template for [{recommendation.Recipe.CdkProjectTemplateId}]");

            try
            {
                //Generate the CDK project using the installed template into the output directory
                await _bootstrapper.CreateAsync(template, recommendation.ProjectDefinition.AssemblyName, outputDirectory, new Dictionary<string, string>(), false, "");
            }
            catch
            {
                throw new TemplateGenerationFailedException();
            }
        }

        private void InstallTemplates(string folderLocation)
        {
            try
            {
                _bootstrapper.Install(folderLocation);
            }
            catch
            {
                throw new DefaultTemplateInstallationFailedException();
            }
        }

        private ITemplateEngineHost CreateHost()
        {
            var preferences = new Dictionary<string, string>
            {
                { "prefs:language", "C#" }
            };

            var builtIns = new AssemblyComponentCatalog(new[]
            {
                typeof(RunnableProjectGenerator).GetTypeInfo().Assembly,            // for assembly: Microsoft.TemplateEngine.Orchestrator.RunnableProjects
                typeof(AssemblyComponentCatalog).GetTypeInfo().Assembly,            // for assembly: Microsoft.TemplateEngine.Edge
            });

            ITemplateEngineHost host = new DefaultTemplateEngineHost(HostIdentifier, HostVersion, CultureInfo.CurrentCulture.Name, preferences, builtIns, null);

            return host;
        }
    }
}