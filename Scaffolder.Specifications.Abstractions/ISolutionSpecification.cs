using System.Collections.Immutable;

namespace Scaffolder.Abstractions;

/// <summary>
/// Represents a specification for a Visual Studio solution.
/// Contains all necessary information for code generation across multiple projects.
/// </summary>
public interface ISolutionSpecification
{
    /// <summary>
    /// Gets the name of the solution without the .sln extension.
    /// Example: "MyCompany.ECommerce"
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the full path to the solution file (.sln).
    /// This is used for file operations and resolving relative paths.
    /// Example: "C:\Projects\MyCompany.ECommerce\MyCompany.ECommerce.sln"
    /// </summary>
    string FullPath { get; }

    /// <summary>
    /// Gets the collection of projects in the solution.
    /// These projects are analyzed to generate appropriate code based on their type.
    /// </summary>
    IImmutableSet<IProjectSpecification> Projects { get; }
}