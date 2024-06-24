namespace Scaffolder.Abstracts;

/// <summary>
/// Represents a specification for generating code.
/// </summary>
public sealed class CodeGenerationSpecification
{
    /// <summary>
    /// The name of the template to use.
    /// </summary>
    public required string TemplateName { get; init; }

    /// <summary>
    /// The model to use when rendering the template.
    /// </summary>
    public required object TemplateModel { get; init; }

    /// <summary>
    /// The full path to the output file.
    /// </summary>
    public required FileInfo OutputFile { get; init; }
}