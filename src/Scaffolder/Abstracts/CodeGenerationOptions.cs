namespace Scaffolder.Abstracts;

/// <summary>
/// Represents the options for code generation.
/// </summary>
public sealed class CodeGenerationOptions
{
    /// <summary>
    /// Indicates whether to use a generic repository inside the query and command handlers.
    /// </summary>
    public bool UseGenericRepository { get; init; } = true;
}