using System.Collections.Immutable;
using Scaffolder.Utilities;

namespace Scaffolder.Internal.Descriptors;

public sealed record PropertyDescriptor
{
    public RichString Name { get; }

    public string Type { get; }

    public bool IsRequired { get; }

    public bool IsComplex { get; }

    public ImmutableArray<PropertyDescriptor> NestedProperties { get; }

    public PropertyDescriptor(RichString name, string type, bool isRequired, bool isComplex = false, ImmutableArray<PropertyDescriptor> nestedProperties = default)
    {
        Name = name;
        Type = type;
        IsRequired = isRequired;
        IsComplex = isComplex;
        NestedProperties = nestedProperties.IsDefault ? ImmutableArray<PropertyDescriptor>.Empty : nestedProperties;
    }
}