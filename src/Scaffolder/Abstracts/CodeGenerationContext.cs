using Scaffolder.Internal.Descriptors;

namespace Scaffolder.Abstracts;

/// <summary>
/// Represents the context for code generation.
/// </summary>
public sealed class CodeGenerationContext
{
    /// <summary>
    /// The aggregate root for which code is being generated.
    /// </summary>
    public required AggregateRootDescriptor AggregateRoot { get; init; }

    /// <summary>
    /// The project that represents the application layer.
    /// </summary>
    public required ProjectDescriptor ApplicationProject { get; init; }

    /// <summary>
    /// The project that represents the web api layer.
    /// </summary>
    public required ProjectDescriptor WebApiProject { get; init; }

    /// <summary>
    /// The directory where generated files should be output.
    /// </summary>
    public required DirectoryInfo SolutionDirectory { get; init; }

    /// <summary>
    /// The options for code generation.
    /// </summary>
    public required CodeGenerationOptions Options { get; init; }

    /// <summary>
    /// The solution
    /// </summary>
    public required SolutionDescriptor Solution { get; set; }
}