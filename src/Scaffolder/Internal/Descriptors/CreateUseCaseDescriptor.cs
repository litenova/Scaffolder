using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

namespace Scaffolder.Internal.Descriptors;

public sealed class CreateUseCaseDescriptor : UseCaseDescriptor
{
    public CreateMechanism Mechanism { get; }

    private CreateUseCaseDescriptor(string name, string returnType, ImmutableArray<MemberDescriptor> parameters, CreateMechanism mechanism)
        : base(name, returnType, parameters)
    {
        Mechanism = mechanism;
    }

    public enum CreateMechanism
    {
        Constructor,
        StaticCreateMethod,
        InitProperties,
        EmptyConstructor
    }

    public static CreateUseCaseDescriptor Create(string name, string returnType, ImmutableArray<MemberDescriptor> parameters, CreateMechanism mechanism, ILogger logger)
    {
        var descriptor = new CreateUseCaseDescriptor(name, returnType, parameters, mechanism);

        logger.LogDebug("Created CreateUseCase: Name: {Name}, ReturnType: {ReturnType}, ParametersCount: {ParametersCount}, Mechanism: {Mechanism}",
            descriptor.Name, descriptor.ReturnType, descriptor.Parameters.Length, descriptor.Mechanism);

        return descriptor;
    }
}