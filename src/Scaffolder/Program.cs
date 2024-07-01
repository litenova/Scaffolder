using CliFx;
using Microsoft.Build.Locator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scaffolder.Abstracts;
using Scaffolder.Cli;
using Scaffolder.Internal;
using Scaffolder.Internal.Generators;

namespace Scaffolder;

internal sealed class Program
{
    static Program()
    {
        // Initialize MSBuild (required for Roslyn)
        MSBuildLocator.RegisterDefaults();
    }

    private static async Task<int> Main(string[] args)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("Scaffolder", LogLevel.Information);

            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Register your services
        services.AddSingleton<ITemplateRepository, EmbeddedResourcesTemplateRepository>();
        services.AddSingleton<ITemplatingEngine, ScribanTemplatingEngine>();

        // Register all code generators
        services.AddTransient<ICodeGenerator, ApiControllerCodeGenerator>();
        services.AddTransient<ICodeGenerator, ApiModelCodeGenerator>();
        services.AddTransient<ICodeGenerator, CreateCommandCodeGenerator>();
        services.AddTransient<ICodeGenerator, QueryCodeGenerator>();
        services.AddTransient<ICodeGenerator, UseCaseCommandCodeGenerator>();

        services.AddTransient<ScaffolderCommand>();
        
        // Build the service provider
        var serviceProvider = services.BuildServiceProvider();
        
        return await new CliApplicationBuilder()
            .AddCommand<ScaffolderCommand>()
            .UseTypeActivator(serviceProvider.GetRequiredService)
            .Build()
            .RunAsync(args);
    }
}