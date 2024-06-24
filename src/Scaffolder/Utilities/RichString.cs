using Humanizer;

namespace Scaffolder.Utilities;

public sealed class RichString(string value)
{
    public string Original => value;

    public string Plural => value.Pluralize();

    public string Singular => value.Singularize();

    public string PascalCase => value.Pascalize();

    public string CamelCase => value.Camelize();

    public string TitleCase => value.Titleize();

    public string LowerCase => value.ToLower();

    public string UpperCase => value.ToUpper();

    public string Humanize => value.Humanize();

    public string Dehumanize => value.Dehumanize();

    public string Underscore => value.Underscore();

    public string Dasherize => value.Dasherize();

    public override string ToString() => value;

    public static implicit operator string(RichString richString) => richString.ToString();

    public static implicit operator RichString(string value) => new(value);
}