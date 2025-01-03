namespace Scaffolder.Abstractions;

/// <summary>
/// Represents the architectural layer of a project in a DDD-structured solution.
/// The layer determines what kind of code will be generated.
/// </summary>
public enum ProjectLayer
{
    /// <summary>
    /// Contains the domain model, including aggregate roots, entities, value objects,
    /// domain events, and domain services. This is the core of the business logic.
    /// Generated files: None (this is source of truth)
    /// </summary>
    Domain,

    /// <summary>
    /// Contains application services, use case implementations, commands, queries,
    /// and their handlers. Orchestrates the domain model.
    /// Generated files: Commands, Queries, Handlers, DTOs
    /// </summary>
    Application,

    /// <summary>
    /// Contains API controllers and models for external communication.
    /// Provides HTTP-based access to the application.
    /// Generated files: Controllers, Request/Response Models, Mappings
    /// </summary>
    WebApi,

    /// <summary>
    /// Contains infrastructure concerns like persistence, external services,
    /// logging, and other technical capabilities.
    /// Generated files: Repositories, DbContext, Configurations
    /// </summary>
    Infrastructure,

    /// <summary>
    /// Other layers that don't fit the main DDD categories.
    /// No code generation is typically performed for these projects.
    /// </summary>
    Other
}