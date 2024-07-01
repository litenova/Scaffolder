using System.Collections.Immutable;
using Scaffolder.Utilities;

namespace Scaffolder.Internal.Descriptors;

public sealed class UseCaseDescriptor
{
    public RichString Name { get; }

    public string ReturnType { get; }

    public ImmutableArray<ParameterDescriptor> Parameters { get; }

    public UseCaseDescriptor(string name, string returnType, ImmutableArray<ParameterDescriptor> parameters)
    {
        Name = name;
        ReturnType = returnType;
        Parameters = parameters;
    }
}