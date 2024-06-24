using Scaffolder.Abstracts;

namespace Scaffolder.Providers.GeneralCommand;

/// <summary>
/// Provides specifications for generating command-related code.
/// </summary>
public class UseCaseCommandSpecificationProvider : ICodeGenerator
{
    public IEnumerable<CodeGenerationSpecification> Generate(CodeGenerationContext context)
    {
        foreach (var useCase in context.AggregateRoot.UseCases)
        {
            var outputDirectory = Path.Combine(context.SolutionDirectory.FullName, "src", "Application", context.AggregateRoot.Name.Plural, useCase.Name);

            yield return new CodeGenerationSpecification
            {
                TemplateName = "Command",
                TemplateModel = new { context.AggregateRoot, UseCase = useCase },
                OutputFile = new FileInfo(Path.Combine(outputDirectory, $"{useCase.Name}{context.AggregateRoot.Name}Command.cs"))
            };

            yield return new CodeGenerationSpecification
            {
                TemplateName = "CommandHandler",
                TemplateModel = new { context.AggregateRoot, UseCase = useCase, context.Options },
                OutputFile = new FileInfo(Path.Combine(outputDirectory, $"{useCase.Name}{context.AggregateRoot.Name}CommandHandler.cs"))
            };

            yield return new CodeGenerationSpecification
            {
                TemplateName = "CommandValidator",
                TemplateModel = new { context.AggregateRoot, UseCase = useCase },
                OutputFile = new FileInfo(Path.Combine(outputDirectory, $"{useCase.Name}{context.AggregateRoot.Name}CommandValidator.cs"))
            };
        }
    }
}