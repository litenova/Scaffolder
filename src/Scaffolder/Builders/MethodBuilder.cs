using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Scaffolder.Builders;

public class MethodBuilder
{
    private readonly string _methodName;
    private readonly string _returnType;
    private readonly List<ParameterSyntax> _parameters = [];
    private readonly List<AttributeListSyntax> _attributes = [];
    private SyntaxTokenList _modifiers = [];
    private BlockSyntax _body;

    public MethodBuilder(string methodName, string returnType)
    {
        _methodName = methodName;
        _returnType = returnType;
    }

    public MethodBuilder AddModifiers(params SyntaxKind[] modifiers)
    {
        _modifiers = _modifiers.AddRange(modifiers.Select(SyntaxFactory.Token));
        return this;
    }

    public MethodBuilder AddAttribute(string attributeName, params string[] arguments)
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

    public MethodBuilder AddParameter(string parameterName, string parameterType, string defaultValue = null)
    {
        var parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
            .WithType(SyntaxFactory.ParseTypeName(parameterType));

        if (defaultValue != null)
        {
            parameter = parameter.WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.ParseExpression(defaultValue)));
        }

        _parameters.Add(parameter);
        return this;
    }

    public MethodBuilder WithBody(Action<BlockBuilder> buildAction)
    {
        var blockBuilder = new BlockBuilder();
        buildAction(blockBuilder);
        _body = blockBuilder.Build();
        return this;
    }

    public MethodDeclarationSyntax Build()
    {
        return SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(_returnType), _methodName)
            .AddModifiers(_modifiers.ToArray())
            .AddAttributeLists(_attributes.ToArray())
            .AddParameterListParameters(_parameters.ToArray())
            .WithBody(_body);
    }
}