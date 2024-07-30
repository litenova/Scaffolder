using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Scaffolder.Builders;

public class PropertyBuilder
{
    private readonly string _propertyName;
    private readonly string _propertyType;
    private readonly List<AttributeListSyntax> _attributes = [];
    private SyntaxTokenList _modifiers = [];
    private bool _hasGetter = true;
    private bool _hasSetter = false;
    private bool _hasInitSetter = false;

    public PropertyBuilder(string propertyName, string propertyType)
    {
        _propertyName = propertyName;
        _propertyType = propertyType;
    }

    public PropertyBuilder AddModifiers(params SyntaxKind[] modifiers)
    {
        _modifiers = _modifiers.AddRange(modifiers.Select(SyntaxFactory.Token));
        return this;
    }

    public PropertyBuilder AddAttribute(string attributeName, params string[] arguments)
    {
        var attribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(attributeName));
        if (arguments.Length > 0)
        {
            attribute = attribute.AddArgumentListArguments(
                arguments.Select(arg => SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression(arg))).ToArray());
        }

        _attributes.Add(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute)));
        return this;
    }

    public PropertyBuilder WithGetter(bool hasGetter = true)
    {
        _hasGetter = hasGetter;
        return this;
    }

    public PropertyBuilder WithSetter(bool hasSetter = true)
    {
        _hasSetter = hasSetter;
        return this;
    }

    public PropertyBuilder WithInitSetter(bool hasInitSetter = true)
    {
        _hasInitSetter = hasInitSetter;
        return this;
    }

    public PropertyDeclarationSyntax Build()
    {
        var accessors = new List<AccessorDeclarationSyntax>();
        if (_hasGetter) accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
        if (_hasSetter) accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
        if (_hasInitSetter) accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.InitAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

        return SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(_propertyType), _propertyName)
            .AddModifiers(_modifiers.ToArray())
            .AddAttributeLists(_attributes.ToArray())
            .AddAccessorListAccessors(accessors.ToArray());
    }
}