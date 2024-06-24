using System.Collections;
using System.Collections.Immutable;
using Scaffolder.Utilities;

namespace Scaffolder.Descriptors;

public sealed class UseCaseDescriptor(string name, string returnType, ImmutableArray<ParameterDescriptor> parameters) : IReadOnlyCollection<ParameterDescriptor>
{
    public RichString Name { get; } = name;

    public string ReturnType { get; } = returnType;

    public int Count => parameters.Length;

    public IEnumerator<ParameterDescriptor> GetEnumerator() => ((IEnumerable<ParameterDescriptor>)parameters).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}