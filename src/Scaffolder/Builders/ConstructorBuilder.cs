using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Scaffolder.Builders;

public class ConstructorBuilder
{
    private readonly string _className;
    private readonly List<ParameterSyntax> _parameters = [];
    private SyntaxTokenList _modifiers = [];
    private BlockSyntax _body;

    public ConstructorBuilder(string className)
    {
        _className = className;
    }

    public ConstructorBuilder AddModifiers(params SyntaxKind[] modifiers)
    {
        _modifiers = _modifiers.AddRange(modifiers.Select(SyntaxFactory.Token));
        return this;
    }

    public ConstructorBuilder AddParameter(string parameterName, string parameterType)
    {
        _parameters.Add(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
                .WithType(SyntaxFactory.ParseTypeName(parameterType))
        );
        return this;
    }

    public ConstructorBuilder WithBody(Action<BlockBuilder> buildAction)
    {
        var blockBuilder = new BlockBuilder();
        buildAction(blockBuilder);
        _body = blockBuilder.Build();
        return this;
    }

    public ConstructorDeclarationSyntax Build()
    {
        return SyntaxFactory.ConstructorDeclaration(_className)
            .AddModifiers(_modifiers.ToArray())
            .AddParameterListParameters(_parameters.ToArray())
            .WithBody(_body);
    }
}