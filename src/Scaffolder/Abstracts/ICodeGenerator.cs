namespace Scaffolder.Abstracts;

/// <summary>
/// Represents a provider for code generation specifications.
/// </summary>
public interface ICodeGenerator
{
    /// <summary>
    /// Retrieves a collection of code generation specifications.
    /// </summary>
    /// <param name="context">The context for code generation.</param>
    /// <returns>A collection of code generation specifications.</returns>
    IEnumerable<CodeGenerationSpecification> Generate(CodeGenerationContext context);
}