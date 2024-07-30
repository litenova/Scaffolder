using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Scaffolder.Builders;

public class ClassBuilder
{
    private readonly string _className;
    private readonly List<UsingDirectiveSyntax> _usings = [];
    private readonly List<MemberDeclarationSyntax> _members = [];
    private readonly List<AttributeListSyntax> _attributes = [];
    private readonly List<BaseTypeSyntax> _baseList = [];
    private SyntaxTokenList _modifiers = [];
    private string _namespace;

    public ClassBuilder(string className)
    {
        _className = className;
    }

    public ClassBuilder AddUsing(string namespaceName)
    {
        _usings.Add(UsingDirective(ParseName(namespaceName)));
        return this;
    }

    public ClassBuilder AddNamespace(string namespaceName)
    {
        _namespace = namespaceName;
        return this;
    }

    public ClassBuilder AddModifiers(params SyntaxKind[] modifiers)
    {
        _modifiers = _modifiers.AddRange(modifiers.Select(Token));
        return this;
    }

    public ClassBuilder AddAttribute(string attributeName, params string[] arguments)
    {
        var attribute = Attribute(IdentifierName(attributeName));
        if (arguments.Length > 0)
        {
            attribute = attribute.AddArgumentListArguments(
                arguments.Select(arg => AttributeArgument(ParseExpression(arg))).ToArray());
        }

        _attributes.Add(AttributeList(SingletonSeparatedList(attribute)));
        return this;
    }

    public ClassBuilder AddBaseType(string baseTypeName)
    {
        _baseList.Add(SimpleBaseType(ParseTypeName(baseTypeName)));
        return this;
    }

    public ClassBuilder AddProperty(string propertyName, string propertyType, Action<PropertyBuilder> buildAction = null)
    {
        var propertyBuilder = new PropertyBuilder(propertyName, propertyType);
        buildAction?.Invoke(propertyBuilder);
        _members.Add(propertyBuilder.Build());
        return this;
    }

    public ClassBuilder AddMethod(string methodName, string returnType, Action<MethodBuilder> buildAction)
    {
        var methodBuilder = new MethodBuilder(methodName, returnType);
        buildAction(methodBuilder);
        _members.Add(methodBuilder.Build());
        return this;
    }

    public ClassBuilder AddConstructor(Action<ConstructorBuilder> buildAction)
    {
        var constructorBuilder = new ConstructorBuilder(_className);
        buildAction(constructorBuilder);
        _members.Add(constructorBuilder.Build());
        return this;
    }

    public CompilationUnitSyntax Build()
    {
        var classDeclaration = ClassDeclaration(_className)
            .AddModifiers(_modifiers.ToArray())
            .AddAttributeLists(_attributes.ToArray())
            .AddBaseListTypes(_baseList.ToArray())
            .AddMembers(_members.ToArray());

        var namespaceDeclaration = NamespaceDeclaration(ParseName(_namespace))
            .AddMembers(classDeclaration);

        return CompilationUnit()
            .AddUsings(_usings.ToArray())
            .AddMembers(namespaceDeclaration);
    }
}