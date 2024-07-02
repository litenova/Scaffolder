using Scaffolder.Abstracts;

namespace Scaffolder.Internal.Generators;

/// <summary>
/// Provides specifications for generating Create command-related code.
/// </summary>
public sealed class CreateCommandCodeGenerator : ICodeGenerator
{
    public IEnumerable<CodeGenerationSpecification> Generate(CodeGenerationContext context)
    {
        var outputDirectory = Path.Combine(context.ApplicationProject.Directory.FullName, context.AggregateRoot.Name.Plural, "Create");

        yield return new CodeGenerationSpecification
        {
            TemplateName = "CreateCommand",
            TemplateModel = new
            {
                context.ApplicationProject,
                context.AggregateRoot
            },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, $"Create{context.AggregateRoot.Name}Command.cs"))
        };

        yield return new CodeGenerationSpecification
        {
            TemplateName = "CreateCommandHandler",
            TemplateModel = new
            {
                DomainProject = context.ApplicationProject,
                context.ApplicationProject,
                context.AggregateRoot,
                context.Options
            },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, $"Create{context.AggregateRoot.Name}CommandHandler.cs"))
        };

        yield return new CodeGenerationSpecification
        {
            TemplateName = "CreateCommandValidator",
            TemplateModel = new
            {
                context.ApplicationProject,
                context.AggregateRoot
            },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, $"Create{context.AggregateRoot.Name}CommandValidator.cs"))
        };

        // Generate Create Command Result
        yield return new CodeGenerationSpecification
        {
            TemplateName = "CreateCommandResult",
            TemplateModel = new
            {
                context.ApplicationProject,
                context.AggregateRoot
            },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, $"Create{context.AggregateRoot.Name}CommandResult.cs"))
        };
    }
}