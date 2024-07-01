using Scaffolder.Abstracts;
using Scriban;
using Scriban.Runtime;

namespace Scaffolder.Internal;

/// <summary>
/// Implements ITemplatingEngine using the Scriban templating engine.
/// </summary>
internal sealed class ScribanTemplatingEngine : ITemplatingEngine
{
    private readonly TemplateContext _templateContext;

    public ScribanTemplatingEngine()
    {
        _templateContext = new TemplateContext();
        ConfigureTemplateContext(_templateContext);
    }

    /// <summary>
    /// Renders a template with the specified model using Scriban.
    /// </summary>
    /// <param name="template">The template to render.</param>
    /// <param name="model">The model to use when rendering the template.</param>
    /// <returns>The rendered template.</returns>
    public async Task<string> RenderAsync(string template, object model)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            throw new ArgumentException("Template cannot be null or whitespace.", nameof(template));
        }

        ArgumentNullException.ThrowIfNull(model);

        var scribanTemplate = Template.Parse(template);
        if (scribanTemplate.HasErrors)
        {
            throw new InvalidOperationException($"Error parsing template: {string.Join(", ", scribanTemplate.Messages)}");
        }

        var scriptObject = new ScriptObject();
        scriptObject.Import(model);
        _templateContext.PushGlobal(scriptObject);

        try
        {
            return await scribanTemplate.RenderAsync(_templateContext);
        }
        finally
        {
            _templateContext.PopGlobal();
        }
    }

    private void ConfigureTemplateContext(TemplateContext context)
    {
        // Configure Scriban's behavior here
        context.MemberRenamer = member => member.Name;

        // You can add custom functions like this:
        // context.PushGlobal(new ScriptObject {
        //     ["custom_function"] = new Func<string, string>(CustomFunction)
        // });
    }

    // Example of a custom function:
    // private static string CustomFunction(string input)
    // {
    //     return input.ToUpperInvariant();
    // }
}