namespace Scaffolder.Abstracts;

/// <summary>
/// Represents a repository for code generation templates (e.g., Liquid templates).
/// </summary>
public interface ITemplateRepository
{
    /// <summary>
    /// Retrieves a template by name.
    /// </summary>
    /// <param name="templateName">The name of the template to retrieve.</param>
    /// <returns>The template content.</returns>
    string GetTemplate(string templateName);
}