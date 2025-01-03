namespace Scaffolder.Specifications.Serialization.Abstractions;

/// <summary>
/// Represents information about a serialized file.
/// </summary>
/// <param name="Path">The full path to the file.</param>
/// <param name="Name">The name of the file.</param>
/// <param name="Size">The size of the file in bytes.</param>
public sealed record class SerializedFile(string Path, string Name, long Size);