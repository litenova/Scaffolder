using System.Text;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Microsoft.Extensions.Logging;
using Scaffolder.Abstracts;
using Scaffolder.Internal.Descriptors;

namespace Scaffolder.Cli;

[Command]
public sealed class ScaffolderCommand(
    ITemplateRepository templateRepository,
    ITemplatingEngine templatingEngine,
    IEnumerable<ICodeGenerator> codeGenerators,
    ILogger<ScaffolderCommand> logger)
    : ICommand
{
    [CommandParameter(0, Description = "Path to the solution file")]
    public string SolutionPath { get; init; } = string.Empty;

    [CommandOption("overwrite", 'o', Description = "Overwrite existing files (default is false)")]
    public bool OverwriteExisting { get; init; } = false;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        logger.LogInformation("Processing solution path: {SolutionPath}", SolutionPath);
        logger.LogInformation("Overwrite existing files: {OverwriteExisting}", OverwriteExisting);

        var solution = await SolutionDescriptor.LoadAsync(SolutionPath, logger);

        var applicationProject = solution.Projects.Single(p => p.Name.Original.Contains("Application", StringComparison.OrdinalIgnoreCase));
        var webApiProject = solution.Projects.Single(p => p.Name.Original.Contains("WebApi", StringComparison.OrdinalIgnoreCase));

        foreach (var project in solution.Projects)
        {
            logger.LogInformation("Processing project: {ProjectName}", project.Name);

            foreach (var aggregateRoot in project.AggregateRoots)
            {
                var context = new CodeGenerationContext
                {
                    Solution = solution,
                    AggregateRoot = aggregateRoot,
                    ApplicationProject = applicationProject,
                    WebApiProject = webApiProject,
                    SolutionDirectory = solution.Directory,
                    Options = new CodeGenerationOptions()
                };

                var specifications = codeGenerators.SelectMany(g => g.Generate(context));

                foreach (var specification in specifications)
                {
                    var outputFile = specification.OutputFile;

                    if (outputFile.Exists && !OverwriteExisting)
                    {
                        logger.LogWarning("File already exists and overwrite is disabled. Skipping: {OutputPath}", outputFile.FullName);
                        continue;
                    }

                    var template = templateRepository.GetTemplate(specification.TemplateName);
                    var output = await templatingEngine.RenderAsync(template, specification.TemplateModel);

                    // Ensure the directory exists
                    outputFile.Directory?.Create();

                    await File.WriteAllTextAsync(outputFile.FullName, output, Encoding.UTF8);

                    logger.LogInformation("Generated file: {OutputPath}", outputFile.FullName);
                }

                logger.LogInformation("Processing aggregate root: {AggregateRootName}", aggregateRoot.Name);
            }
        }
    }
}