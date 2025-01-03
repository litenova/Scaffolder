// Program.cs

using CliFx;
using Scaffolder.Cli.Commands;
using Scaffolder.Specifications;
using Scaffolder.Specifications.Utilities;

// Configure logging functions for specifications
Logger.Configure(
    info: message => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] info: {message}"),
    warning: message => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] warn: {message}"),
    error: message => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] fail: {message}"),
    debug: message => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] dbug: {message}")
);

// Configure and run CLI application
try
{
    var app = new CliApplicationBuilder()
        .SetDescription("Code generation tool for DDD-style applications")
        .SetExecutableName("scaffolder")
        .AddCommand<GenerateSpecificationsCommand>()
        .Build();

    return await app.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] fail: Application terminated unexpectedly");
    Console.WriteLine(ex);
    return 1;
}