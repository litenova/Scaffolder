using Scaffolder.Abstracts;

namespace Scaffolder.Providers.ApiModels;

/// <summary>
/// Provides specifications for generating request/response model code.
/// </summary>
public class ApiModelSpecificationProvider : ICodeGenerator
{
    public IEnumerable<CodeGenerationSpecification> Generate(CodeGenerationContext context)
    {
        var outputDirectory = Path.Combine(context.SolutionDirectory.FullName, "src", "Application", context.AggregateRoot.Name.Plural);

        // General response model
        yield return new CodeGenerationSpecification
        {
            TemplateName = "ResponseModel",
            TemplateModel = new { context.AggregateRoot },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, $"{context.AggregateRoot.Name}ResponseModel.cs"))
        };

        foreach (var useCase in context.AggregateRoot.UseCases)
        {
            var useCaseDirectory = Path.Combine(outputDirectory, useCase.Name);

            yield return new CodeGenerationSpecification
            {
                TemplateName = "RequestModel",
                TemplateModel = new { context.AggregateRoot, UseCase = useCase },
                OutputFile = new FileInfo(Path.Combine(useCaseDirectory, $"{useCase.Name}{context.AggregateRoot.Name}Request.cs"))
            };

            yield return new CodeGenerationSpecification
            {
                TemplateName = "ResponseModel",
                TemplateModel = new { context.AggregateRoot, UseCase = useCase },
                OutputFile = new FileInfo(Path.Combine(useCaseDirectory, $"{useCase.Name}{context.AggregateRoot.Name}Response.cs"))
            };
        }
    }
}