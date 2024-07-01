using Scaffolder.Abstracts;

namespace Scaffolder.Internal.Generators;

/// <summary>
/// Provides specifications for generating request/response model code.
/// </summary>
public sealed class ApiModelCodeGenerator : ICodeGenerator
{
    public IEnumerable<CodeGenerationSpecification> Generate(CodeGenerationContext context)
    {
        var outputDirectory = Path.Combine(context.SolutionDirectory.FullName, "src", context.WebApiProject.Name, "Models", context.AggregateRoot.Name.Plural);

        // General response model
        yield return new CodeGenerationSpecification
        {
            TemplateName = "SharedResponseModel",
            TemplateModel = new { context.AggregateRoot },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, "Shared", $"{context.AggregateRoot.Name}ResponseModel.cs"))
        };

        // Create request model
        yield return new CodeGenerationSpecification
        {
            TemplateName = "CreateRequestModel",
            TemplateModel = new { context.AggregateRoot, context.ApplicationProject },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, "Create", $"Create{context.AggregateRoot.Name}Request.cs"))
        };

        // GetAll request model
        yield return new CodeGenerationSpecification
        {
            TemplateName = "GetAllRequestModel",
            TemplateModel = new { context.AggregateRoot },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, "GetAll", $"GetAll{context.AggregateRoot.Name.Plural}Request.cs"))
        };

        foreach (var useCase in context.AggregateRoot.UseCases.Where(uc => uc.Name != "Create" && uc.Name != "GetById" && uc.Name != "GetAll"))
        {
            var useCaseDirectory = Path.Combine(outputDirectory, useCase.Name);

            yield return new CodeGenerationSpecification
            {
                TemplateName = "UseCaseRequestModel",
                TemplateModel = new { context.AggregateRoot, UseCase = useCase, context.ApplicationProject },
                OutputFile = new FileInfo(Path.Combine(useCaseDirectory, $"{useCase.Name}{context.AggregateRoot.Name}Request.cs"))
            };
        }
    }
}