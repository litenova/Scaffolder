using System.Security.Cryptography;
using System.Text;

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
    Task<string> GetTemplateAsync(string templateName);
}

/// <summary>
/// Implements a repository for code generation templates with file-based caching support.
/// </summary>
public class FileCachedTemplateRepository : ITemplateRepository
{
    private readonly string _templateDirectory;
    private readonly string _cacheDirectory;

    public FileCachedTemplateRepository(string templateDirectory)
    {
        _templateDirectory = templateDirectory ?? throw new ArgumentNullException(nameof(templateDirectory));
        
        _cacheDirectory = Path.Combine(Path.GetTempPath(), "TemplateCache");
        
        Directory.CreateDirectory(_cacheDirectory);
    }

    /// <summary>
    /// Retrieves a template by name, using file-based cache if available.
    /// </summary>
    /// <param name="templateName">The name of the template to retrieve.</param>
    /// <returns>The template content.</returns>
    public async Task<string> GetTemplateAsync(string templateName)
    {
        var templatePath = Path.Combine(_templateDirectory, $"{templateName}.cs.scriban");
        var cacheFilePath = GetCacheFilePath(templateName);

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template file not found: {templatePath}");
        }

        var templateLastWriteTime = File.GetLastWriteTimeUtc(templatePath);

        if (File.Exists(cacheFilePath))
        {
            var cacheLastWriteTime = File.GetLastWriteTimeUtc(cacheFilePath);
            if (cacheLastWriteTime >= templateLastWriteTime)
            {
                return await File.ReadAllTextAsync(cacheFilePath);
            }
        }

        var templateContent = await File.ReadAllTextAsync(templatePath);
        await File.WriteAllTextAsync(cacheFilePath, templateContent);
        return templateContent;
    }

    private string GetCacheFilePath(string templateName)
    {
        var hash = ComputeHash(templateName);
        return Path.Combine(_cacheDirectory, $"{hash}.cache");
    }

    private static string ComputeHash(string input)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}