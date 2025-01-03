using System.Collections.Immutable;

namespace Scaffolder.Abstractions;

/// <summary>
/// Represents a use case (domain operation) specification that can be performed on an aggregate root.
/// This specification contains all necessary information for generating command/query handlers,
/// API endpoints, and related infrastructure.
/// </summary>
public interface IUseCaseSpecification
{
    /// <summary>
    /// Gets the name of the use case.
    /// Examples: "Create", "Update", "Cancel", "Approve", "MarkAsPaid"
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the XML documentation for this use case.
    /// This will be used to generate documentation for commands, handlers, and API endpoints.
    /// </summary>
    string XmlDocumentation { get; }

    /// <summary>
    /// Gets the collection of parameters that this use case accepts.
    /// These will be used to generate command/query properties and API request models.
    /// </summary>
    IImmutableSet<IMemberSpecification> Parameters { get; }

    /// <summary>
    /// Gets the return type specification of this use case, if any.
    /// This will be used to generate command/query result types and API response models.
    /// Null indicates void return type.
    /// </summary>
    IMemberSpecification? ReturnType { get; }
}