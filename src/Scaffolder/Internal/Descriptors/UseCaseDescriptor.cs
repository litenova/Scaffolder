using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Scaffolder.Utilities;

namespace Scaffolder.Internal.Descriptors;

public class UseCaseDescriptor
{
    public RichString Name { get; }

    public string ReturnType { get; }

    public ImmutableArray<MemberDescriptor> Parameters { get; }

    protected UseCaseDescriptor(string name, string returnType, ImmutableArray<MemberDescriptor> parameters)
    {
        Name = name;
        ReturnType = returnType;
        Parameters = parameters;
    }

    public static UseCaseDescriptor Create(IMethodSymbol methodSymbol, ILogger logger)
    {
        var parameters = MemberDescriptor.CreateManyFromParameters(methodSymbol.Parameters, logger);

        var useCase = new UseCaseDescriptor(
            methodSymbol.Name,
            methodSymbol.ReturnType.ToString() ?? throw new InvalidOperationException(),
            parameters
        );

        logger.LogDebug("Created use case: {UseCaseName}, ReturnType: {ReturnType}, ParametersCount: {ParametersCount}",
            useCase.Name, useCase.ReturnType, useCase.Parameters.Length);

        return useCase;
    }
}