using System.Diagnostics;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;
using Scaffolder.Descriptors;

namespace Scaffolder;

internal class Program
{
    static Program()
    {
        // Initialize MSBuild (required for Roslyn)
        MSBuildLocator.RegisterDefaults();
    }

    private static async Task Main(string[] args)
    {
        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddConsole();
        });
        var logger = loggerFactory.CreateLogger<Program>();

        // Get path from args or console input
        string path = GetPath(args);

        var options = new Options { Path = path };

        try
        {
            if (Directory.Exists(options.Path))
            {
                await ProcessSolutionsInDirectoryAsync(options.Path, logger);
            }
            else if (File.Exists(options.Path) && options.Path.EndsWith(".sln"))
            {
                await ProcessSolutionAsync(options.Path, logger);
            }
            else
            {
                logger.LogError("The specified path is not a valid directory or .sln file.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while processing");
        }
    }

    // ... rest of the methods remain the same

    static string GetPath(string[] args)
    {
        if (args.Length > 0)
            return args[0];

        if (Debugger.IsAttached)
        {
            Console.Write("Enter path to solution file or directory: ");
            return Console.ReadLine() ?? string.Empty;
        }

        Console.WriteLine("Please provide a path to a solution file or directory as an argument.");
        Environment.Exit(1);
        return string.Empty; // This line will never be reached, but it's needed for compilation
    }

    static async Task ProcessSolutionsInDirectoryAsync(string directoryPath, ILogger logger)
    {
        var solutionFiles = Directory.GetFiles(directoryPath, "*.sln", SearchOption.AllDirectories);
        foreach (var solutionFile in solutionFiles)
        {
            await ProcessSolutionAsync(solutionFile, logger);
        }
    }

    static async Task ProcessSolutionAsync(string solutionPath, ILogger logger)
    {
        logger.LogInformation("Processing solution: {SolutionPath}", solutionPath);
        var solution = await SolutionDescriptor.LoadAsync(solutionPath, logger);

        foreach (var project in solution)
        {
            logger.LogInformation("Processing project: {ProjectName}", project.Name);
            foreach (var aggregateRoot in project)
            {
                logger.LogInformation("Processing aggregate root: {AggregateRootName}", aggregateRoot.Name);
            }
        }
    }
}