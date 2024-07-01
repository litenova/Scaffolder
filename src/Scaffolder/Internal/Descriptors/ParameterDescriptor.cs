using System.Collections.Immutable;
using Scaffolder.Utilities;

namespace Scaffolder.Internal.Descriptors;

public sealed record ParameterDescriptor
{
    public RichString Name { get; }

    public string Type { get; }

    public bool IsRequired { get; }

    public bool IsComplex { get; }

    public ImmutableArray<ParameterDescriptor> NestedParameters { get; }

    public ParameterDescriptor(RichString name, string type, bool isRequired = false, bool isComplex = false, ImmutableArray<ParameterDescriptor> nestedParameters = default)
    {
        Name = name;
        Type = type;
        IsRequired = isRequired;
        IsComplex = isComplex;
        NestedParameters = nestedParameters.IsDefault ? ImmutableArray<ParameterDescriptor>.Empty : nestedParameters;
    }
}