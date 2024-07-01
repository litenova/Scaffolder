using System.Reflection;
using Microsoft.Extensions.Logging;
using Scaffolder.Abstracts;

namespace Scaffolder.Internal;

/// <summary>
/// Implements a repository for code generation templates using embedded resources with in-memory caching support.
/// </summary>
internal sealed class EmbeddedResourcesTemplateRepository : ITemplateRepository
{
    private readonly Assembly _assembly;
    private readonly ILogger<EmbeddedResourcesTemplateRepository> _logger;

    public EmbeddedResourcesTemplateRepository(ILogger<EmbeddedResourcesTemplateRepository> logger)
    {
        _logger = logger;
        _assembly = typeof(EmbeddedResourcesTemplateRepository).Assembly;
        _logger.LogInformation("Assembly: {AssemblyFullName}", _assembly.FullName);
        _logger.LogInformation("Assembly Location: {AssemblyLocation}", _assembly.Location);
        var resourceNames = _assembly.GetManifestResourceNames();
        _logger.LogInformation("Resource count: {ResourceCount}", resourceNames.Length);
        foreach (var name in resourceNames)
        {
            _logger.LogInformation("Resource: {ResourceName}", name);
        }
    }

    /// <summary>
    /// Retrieves a template by name, using in-memory cache if available.
    /// </summary>
    /// <param name="templateName">The name of the template to retrieve.</param>
    /// <returns>The template content.</returns>
    public string GetTemplate(string templateName)
    {
        var resourceName = $"Scaffolder.Templates.{templateName}.scriban";
        _logger.LogDebug("Attempting to load resource: {ResourceName}", resourceName);

        using var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            _logger.LogError("Template resource not found: {ResourceName}", resourceName);
            throw new InvalidOperationException($"Template resource not found: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        _logger.LogDebug("Template content loaded, length: {ContentLength}", content.Length);

        return content;
    }
}