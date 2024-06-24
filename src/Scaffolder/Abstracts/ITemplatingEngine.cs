namespace Scaffolder.Abstracts;

/// <summary>
/// Represents a templating engine that can render given templates with a model. (e.g., Liquid templating engine)
/// </summary>
public interface ITemplatingEngine
{
    /// <summary>
    /// Renders a template with the specified model.
    /// </summary>
    /// <param name="template">The template to render.</param>
    /// <param name="model">The model to use when rendering the template.</param>
    /// <returns>The rendered template.</returns>
    Task<string> RenderAsync(string template, object model);
}