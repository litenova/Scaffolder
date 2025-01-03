using System.Collections.Immutable;

namespace Scaffolder.Abstractions;

/// <summary>
/// Represents the specification for a Domain-Driven Design (DDD) Aggregate Root.
/// </summary>
public interface IAggregateRootSpecification : ITypeSpecification
{
    /// <summary>
    /// Gets the type specification for the identifier used by this aggregate root.
    /// </summary>
    ITypeSpecification IdType { get; }

    /// <summary>
    /// Gets the collection of properties that define the state of the aggregate root.
    /// </summary>
    IImmutableSet<IMemberSpecification> Members { get; }

    /// <summary>
    /// Gets the collection of use cases that create new instances of this aggregate root.
    /// </summary>
    IImmutableSet<IUseCaseSpecification> CreateUseCases { get; }

    /// <summary>
    /// Gets the collection of use cases that read/query this aggregate root.
    /// </summary>
    IImmutableSet<IUseCaseSpecification> ReadUseCases { get; }

    /// <summary>
    /// Gets the collection of use cases that update/modify this aggregate root.
    /// </summary>
    IImmutableSet<IUseCaseSpecification> UpdateUseCases { get; }

    /// <summary>
    /// Gets the collection of use cases that delete instances of this aggregate root.
    /// </summary>
    IImmutableSet<IUseCaseSpecification> DeleteUseCases { get; }
}