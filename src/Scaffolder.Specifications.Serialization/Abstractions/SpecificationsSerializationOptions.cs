namespace Scaffolder.Specifications.Serialization.Abstractions;

/// <summary>
/// Defines options for specification serialization.
/// </summary>
/// <remarks>
/// The serializer creates:
/// - solution.json: Contains solution and project information
/// - {aggregateRoot}.json: One file per aggregate root (e.g., order.json, customer.json)
/// </remarks>
public sealed record class SpecificationsSerializationOptions
{
    /// <summary>
    /// Gets the directory where specification files will be written.
    /// </summary>
    /// <remarks>
    /// The directory will be created if it doesn't exist.
    /// </remarks>
    public required string OutputDirectory { get; init; }

    /// <summary>
    /// Gets a value indicating whether to format the JSON with indentation.
    /// </summary>
    public bool Indent { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether to overwrite existing files.
    /// </summary>
    /// <remarks>
    /// When false, throws if any specification files already exist.
    /// </remarks>
    public bool Overwrite { get; init; } = true;
}