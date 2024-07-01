using Scaffolder.Abstracts;

namespace Scaffolder.Internal.Generators;

/// <summary>
/// Provides specifications for generating API controller code.
/// </summary>
public sealed class ApiControllerCodeGenerator : ICodeGenerator
{
    public IEnumerable<CodeGenerationSpecification> Generate(CodeGenerationContext context)
    {
        var outputDirectory = Path.Combine(context.SolutionDirectory.FullName, "src", "Api", "Controllers");

        yield return new CodeGenerationSpecification
        {
            TemplateName = "ApiController",
            TemplateModel = new
            {
                context.AggregateRoot,
                context.ApplicationProject,
                context.AggregateRoot.UseCases
            },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, $"{context.AggregateRoot.Name.Plural}Controller.cs"))
        };
    }
}