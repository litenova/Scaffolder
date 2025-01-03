using System.Collections.Immutable;

namespace Scaffolder.Abstractions;

/// <summary>
/// Represents a specification for a project within a solution.
/// Contains all necessary information for code generation within a specific project context.
/// </summary>
public interface IProjectSpecification : ITypeSpecification
{
    /// <summary>
    /// Gets the full path to the project file (.csproj).
    /// This is used for file operations and generating new files in the correct location.
    /// Example: "C:\Projects\MyCompany.ECommerce\src\MyCompany.ECommerce.Domain\MyCompany.ECommerce.Domain.csproj"
    /// </summary>
    string FullPath { get; }

    /// <summary>
    /// Gets the root namespace of the project.
    /// This is used as the base namespace for all generated types within this project.
    /// Example: "MyCompany.ECommerce.Domain"
    /// </summary>
    string RootNamespace { get; }

    /// <summary>
    /// Gets the assembly name of the project.
    /// This is used for generating fully qualified type names and managing project references.
    /// Example: "MyCompany.ECommerce.Domain"
    /// </summary>
    string AssemblyName { get; }

    /// <summary>
    /// Gets the collection of aggregate roots defined in this project.
    /// Only domain projects (ProjectType.Domain) will typically contain aggregate roots.
    /// These are used as the basis for generating application and infrastructure code.
    /// </summary>
    IImmutableSet<IAggregateRootSpecification> AggregateRoots { get; }

    /// <summary>
    /// Gets the architectural layer which determines what kind of code will be generated.
    /// Different layers will result in different generated artifacts.
    /// </summary>
    ProjectLayer Layer { get; }
}