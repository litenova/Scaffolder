using Scaffolder.Abstracts;

namespace Scaffolder.Providers.GetAllQuery;

/// <summary>
/// Provides specifications for generating query-related code.
/// </summary>
public class QuerySpecificationProvider : ICodeGenerator
{
    public IEnumerable<CodeGenerationSpecification> Generate(CodeGenerationContext context)
    {
        var outputDirectory = Path.Combine(context.SolutionDirectory.FullName, "src", "Application", context.AggregateRoot.Name.Plural);

        // GetById Query
        yield return new CodeGenerationSpecification
        {
            TemplateName = "GetByIdQuery",
            TemplateModel = new { context.AggregateRoot },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, "GetById", $"Get{context.AggregateRoot.Name}ByIdQuery.cs"))
        };

        yield return new CodeGenerationSpecification
        {
            TemplateName = "GetByIdQueryHandler",
            TemplateModel = new { context.AggregateRoot, context.Options },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, "GetById", $"Get{context.AggregateRoot.Name}ByIdQueryHandler.cs"))
        };

        yield return new CodeGenerationSpecification
        {
            TemplateName = "GetByIdQueryValidator",
            TemplateModel = new { context.AggregateRoot },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, "GetById", $"Get{context.AggregateRoot.Name}ByIdQueryValidator.cs"))
        };

        // GetAll Query
        yield return new CodeGenerationSpecification
        {
            TemplateName = "GetAllQuery",
            TemplateModel = new { context.AggregateRoot },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, "GetAll", $"GetAll{context.AggregateRoot.Name.Plural}Query.cs"))
        };

        yield return new CodeGenerationSpecification
        {
            TemplateName = "GetAllQueryHandler",
            TemplateModel = new { context.AggregateRoot, context.Options },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, "GetAll", $"GetAll{context.AggregateRoot.Name.Plural}QueryHandler.cs"))
        };

        yield return new CodeGenerationSpecification
        {
            TemplateName = "GetAllQueryValidator",
            TemplateModel = new { context.AggregateRoot },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, "GetAll", $"GetAll{context.AggregateRoot.Name.Plural}QueryValidator.cs"))
        };
    }
}