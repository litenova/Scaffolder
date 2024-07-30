using Scaffolder.Abstracts;

namespace Scaffolder.Internal.Generators;

/// <summary>
/// Provides specifications for generating command-related code for use cases.
/// </summary>
public sealed class UseCaseCommandCodeGenerator : ICodeGenerator
{
    public IEnumerable<CodeGenerationSpecification> Generate(CodeGenerationContext context)
    {
        foreach (var useCase in context.AggregateRoot.UseCases)
        {
            var outputDirectory = Path.Combine(context.ApplicationProject.Directory.FullName, context.AggregateRoot.Name.Plural, useCase.Name);

            yield return new CodeGenerationSpecification
            {
                TemplateName = "UseCaseCommand",
                TemplateModel = new
                {
                    context.DomainProject,
                    context.ApplicationProject,
                    context.AggregateRoot,
                    UseCase = useCase
                },
                OutputFile = new FileInfo(Path.Combine(outputDirectory, $"{useCase.Name}{context.AggregateRoot.Name}Command.cs"))
            };

            yield return new CodeGenerationSpecification
            {
                TemplateName = "UseCaseCommandHandler",
                TemplateModel = new
                {
                    context.DomainProject,
                    context.ApplicationProject,
                    context.AggregateRoot,
                    UseCase = useCase,
                    context.Options
                },
                OutputFile = new FileInfo(Path.Combine(outputDirectory, $"{useCase.Name}{context.AggregateRoot.Name}CommandHandler.cs"))
            };

            yield return new CodeGenerationSpecification
            {
                TemplateName = "UseCaseCommandValidator",
                TemplateModel = new
                {
                    context.DomainProject,
                    context.ApplicationProject,
                    context.AggregateRoot,
                    UseCase = useCase
                },
                OutputFile = new FileInfo(Path.Combine(outputDirectory, $"{useCase.Name}{context.AggregateRoot.Name}CommandValidator.cs"))
            };
        }
    }
}