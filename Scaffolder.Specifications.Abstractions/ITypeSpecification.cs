namespace Scaffolder.Abstractions;

/// <summary>
/// Represents the specification for a type in the domain model.
/// </summary>
public interface ITypeSpecification
{
    /// <summary>
    /// Gets the name of the type without namespace.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the namespace where the type is defined.
    /// </summary>
    string Namespace { get; }

    /// <summary>
    /// Gets the fully qualified name of the type, including assembly, namespace, and type name.
    /// Example: "MyCompany.Domain.CustomerManagement.Customer, MyCompany.Domain"
    /// </summary>
    string FullName { get; }
}