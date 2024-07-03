using Scaffolder.Abstracts;

namespace Scaffolder.Internal.Generators;

/// <summary>
/// Provides specifications for generating request/response model code.
/// </summary>
public sealed class ApiModelCodeGenerator : ICodeGenerator
{
    public IEnumerable<CodeGenerationSpecification> Generate(CodeGenerationContext context)
    {
        var outputDirectory = Path.Combine(context.WebApiProject.Directory.FullName, "Models", context.AggregateRoot.Name.Plural);

        // Create request model
        yield return new CodeGenerationSpecification
        {
            TemplateName = "CreateRequestModel",
            TemplateModel = new { context.AggregateRoot, context.WebApiProject },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, "Create", $"Create{context.AggregateRoot.Name}RequestModel.cs"))
        };

        // Create request model
        yield return new CodeGenerationSpecification
        {
            TemplateName = "CreateResponseModel",
            TemplateModel = new { context.AggregateRoot, context.WebApiProject },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, "Create", $"Create{context.AggregateRoot.Name}ResponseModel.cs"))
        };

        // General response model
        yield return new CodeGenerationSpecification
        {
            TemplateName = "SharedResponseModel",
            TemplateModel = new { context.AggregateRoot, context.WebApiProject },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, "Shared", $"{context.AggregateRoot.Name}ResponseModel.cs"))
        };

        foreach (var useCase in context.AggregateRoot.UseCases.Where(uc => uc.Name != "Create" && uc.Name != "GetById" && uc.Name != "GetAll"))
        {
            var useCaseDirectory = Path.Combine(outputDirectory, useCase.Name);

            yield return new CodeGenerationSpecification
            {
                TemplateName = "UseCaseRequestModel",
                TemplateModel = new { context.AggregateRoot, UseCase = useCase, context.WebApiProject },
                OutputFile = new FileInfo(Path.Combine(useCaseDirectory, $"{useCase.Name}{context.AggregateRoot.Name}RequestModel.cs"))
            };
        }
    }
}