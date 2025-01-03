using Scaffolder.Abstractions;

namespace Scaffolder.Specifications.Serialization.Abstractions;

/// <summary>
/// Serializes solution specifications to JSON files.
/// </summary>
public interface ISpecificationSerializer
{
    /// <summary>
    /// Serializes a solution specification to JSON files.
    /// </summary>
    /// <param name="solution">The solution specification to serialize.</param>
    /// <param name="options">The serialization options.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result containing information about the serialized files.</returns>
    /// <exception cref="IOException">Thrown when file operations fail.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when file access is denied.</exception>
    Task<SerializationResult> SerializeAsync(ISolutionSpecification solution, SpecificationsSerializationOptions? options = null, CancellationToken cancellationToken = default);
}