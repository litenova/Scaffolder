using Scaffolder.Abstracts;

namespace Scaffolder.Providers.ApiController;

/// <summary>
/// Provides specifications for generating API controller code.
/// </summary>
public class ApiControllerSpecificationProvider : ICodeGenerator
{
    public IEnumerable<CodeGenerationSpecification> Generate(CodeGenerationContext context)
    {
        var outputDirectory = Path.Combine(context.SolutionDirectory.FullName, "src", "Api", "Controllers");

        yield return new CodeGenerationSpecification
        {
            TemplateName = "ApiController",
            TemplateModel = new { context.AggregateRoot, context.ApplicationProject, UseCases = context.AggregateRoot.UseCases },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, $"{context.AggregateRoot.Name.Plural}Controller.cs"))
        };
    }
}