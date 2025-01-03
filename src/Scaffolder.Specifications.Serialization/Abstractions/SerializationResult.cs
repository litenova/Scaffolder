namespace Scaffolder.Specifications.Serialization.Abstractions;

/// <summary>
/// Represents the result of a serialization operation.
/// </summary>
/// <param name="OutputDirectory">The directory where files were written.</param>
/// <param name="SolutionFile">Information about the serialized solution file.</param>
/// <param name="AggregateRootFiles">Information about the serialized aggregate root files.</param>
public sealed record class SerializationResult(
    string OutputDirectory,
    SerializedFile SolutionFile,
    IReadOnlyList<SerializedFile> AggregateRootFiles)
{
    /// <summary>
    /// Gets the total size of all serialized files in bytes.
    /// </summary>
    public long TotalSize => SolutionFile.Size + AggregateRootFiles.Sum(f => f.Size);
}