using Scaffolder.Abstracts;

namespace Scaffolder.Internal.Generators;

/// <summary>
/// Provides specifications for generating query-related code.
/// </summary>
public sealed class QueryCodeGenerator : ICodeGenerator
{
    /// <summary>
    /// Generates code generation specifications for query-related components.
    /// </summary>
    /// <param name="context">The context for code generation.</param>
    /// <returns>An enumerable of code generation specifications.</returns>
    public IEnumerable<CodeGenerationSpecification> Generate(CodeGenerationContext context)
    {
        var outputDirectory = Path.Combine(context.ApplicationProject.Directory.FullName, context.AggregateRoot.Name.Plural);

        // Shared QueryResult
        yield return new CodeGenerationSpecification
        {
            TemplateName = "SharedQueryResult",
            TemplateModel = new { context.ApplicationProject, context.AggregateRoot },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, "Shared", $"{context.AggregateRoot.Name}QueryResult.cs"))
        };

        // GetById Query
        yield return new CodeGenerationSpecification
        {
            TemplateName = "GetByIdQuery",
            TemplateModel = new { context.ApplicationProject, context.AggregateRoot },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, "GetById", $"Get{context.AggregateRoot.Name}ByIdQuery.cs"))
        };

        yield return new CodeGenerationSpecification
        {
            TemplateName = "GetByIdQueryHandler",
            TemplateModel = new { context.ApplicationProject, context.AggregateRoot, context.Options },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, "GetById", $"Get{context.AggregateRoot.Name}ByIdQueryHandler.cs"))
        };

        yield return new CodeGenerationSpecification
        {
            TemplateName = "GetByIdQueryValidator",
            TemplateModel = new { context.ApplicationProject, context.AggregateRoot },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, "GetById", $"Get{context.AggregateRoot.Name}ByIdQueryValidator.cs"))
        };

        // GetAll Query
        yield return new CodeGenerationSpecification
        {
            TemplateName = "GetAllQuery",
            TemplateModel = new { context.ApplicationProject, context.AggregateRoot },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, "GetAll", $"GetAll{context.AggregateRoot.Name.Plural}Query.cs"))
        };

        yield return new CodeGenerationSpecification
        {
            TemplateName = "GetAllQueryHandler",
            TemplateModel = new { context.ApplicationProject, context.AggregateRoot, context.Options },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, "GetAll", $"GetAll{context.AggregateRoot.Name.Plural}QueryHandler.cs"))
        };

        yield return new CodeGenerationSpecification
        {
            TemplateName = "GetAllQueryValidator",
            TemplateModel = new { context.ApplicationProject, context.AggregateRoot },
            OutputFile = new FileInfo(Path.Combine(outputDirectory, "GetAll", $"GetAll{context.AggregateRoot.Name.Plural}QueryValidator.cs"))
        };
    }
}