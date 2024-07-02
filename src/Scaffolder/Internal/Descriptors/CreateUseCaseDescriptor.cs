using System.Collections.Immutable;

namespace Scaffolder.Internal.Descriptors;

public sealed class CreateUseCaseDescriptor(string name, string returnType, ImmutableArray<ParameterDescriptor> parameters, CreateUseCaseDescriptor.CreateMechanism mechanism)
    : UseCaseDescriptor(name, returnType, parameters)
{
    public CreateMechanism Mechanism { get; } = mechanism;

    public enum CreateMechanism
    {
        Constructor,
        StaticCreateMethod,
        InitProperties,
        EmptyConstructor
    }
}