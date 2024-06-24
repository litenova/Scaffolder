using Scaffolder.Abstracts;

namespace Scaffolder.Providers.CreateCommand;

/// <summary>
/// Provides specifications for generating command-related code.
/// </summary>
public class CreateCommandSpecificationProvider : ICodeGenerator
{
    public IEnumerable<CodeGenerationSpecification> Generate(CodeGenerationContext context)
    {
        foreach (var useCase in context.AggregateRoot.UseCases)
        {
            var outputDirectory = Path.Combine(context.SolutionDirectory.FullName, "src", "Application", "UseCases", useCase.Name);

            yield return new CodeGenerationSpecification
            {
                TemplateName = "Command",
                TemplateModel = new { AggregateRoot = context.AggregateRoot, UseCase = useCase },
                OutputFile = new FileInfo(Path.Combine(outputDirectory, $"{useCase.Name}Command.cs"))
            };

            yield return new CodeGenerationSpecification
            {
                TemplateName = "CommandHandler",
                TemplateModel = context,
                OutputFile = new FileInfo(Path.Combine(outputDirectory, $"{useCase.Name}CommandHandler.cs"))
            };

            yield return new CodeGenerationSpecification
            {
                TemplateName = "CommandValidator",
                TemplateModel = new { AggregateRoot = context.AggregateRoot, UseCase = useCase },
                OutputFile = new FileInfo(Path.Combine(outputDirectory, $"{useCase.Name}CommandValidator.cs"))
            };
        }
    }
}