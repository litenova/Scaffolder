using Scaffolder.Utilities;

namespace Scaffolder.Descriptors;

public sealed record PropertyDescriptor(RichString Name, string Type, bool IsRequired);

