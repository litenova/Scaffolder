using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Scaffolder.Builders;

public class BlockBuilder
{
    private readonly List<StatementSyntax> _statements = new();

    public BlockBuilder AddStatement(string statement)
    {
        _statements.Add(ParseStatement(statement));
        return this;
    }

    public BlockBuilder AddVariableDeclaration(string type, string name, string initializer = null)
    {
        var declaration = VariableDeclaration(ParseTypeName(type))
            .WithVariables(SingletonSeparatedList(
                VariableDeclarator(Identifier(name))
                    .WithInitializer(initializer != null ? EqualsValueClause(ParseExpression(initializer)) : null)));

        _statements.Add(LocalDeclarationStatement(declaration));
        return this;
    }

    public BlockBuilder AddAssignment(string left, string right)
    {
        _statements.Add(ExpressionStatement(
            AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                ParseExpression(left),
                ParseExpression(right))));
        return this;
    }

    public BlockBuilder AddMethodInvocation(string methodName, params string[] arguments)
    {
        var invocation = InvocationExpression(IdentifierName(methodName))
            .WithArgumentList(ArgumentList(SeparatedList(arguments.Select(arg => Argument(ParseExpression(arg))))));

        _statements.Add(ExpressionStatement(invocation));
        return this;
    }

    public BlockBuilder AddIfStatement(string condition, Action<BlockBuilder> thenAction, Action<BlockBuilder> elseAction = null)
    {
        var thenBlock = new BlockBuilder();
        thenAction(thenBlock);

        var ifStatement = IfStatement(ParseExpression(condition), thenBlock.Build());

        if (elseAction != null)
        {
            var elseBlock = new BlockBuilder();
            elseAction(elseBlock);
            ifStatement = ifStatement.WithElse(ElseClause(elseBlock.Build()));
        }

        _statements.Add(ifStatement);
        return this;
    }

    public BlockBuilder AddForEachLoop(string type, string identifier, string collection, Action<BlockBuilder> bodyAction)
    {
        var bodyBlock = new BlockBuilder();
        bodyAction(bodyBlock);

        var foreachStatement = ForEachStatement(
            ParseTypeName(type),
            Identifier(identifier),
            ParseExpression(collection),
            bodyBlock.Build());

        _statements.Add(foreachStatement);
        return this;
    }

    public BlockBuilder AddTryCatch(Action<BlockBuilder> tryAction, Action<BlockBuilder> catchAction, string exceptionType = "Exception", string exceptionVariable = "ex")
    {
        var tryBlock = new BlockBuilder();
        tryAction(tryBlock);

        var catchBlock = new BlockBuilder();
        catchAction(catchBlock);

        var tryCatchStatement = TryStatement()
            .WithBlock(tryBlock.Build())
            .AddCatches(
                CatchClause()
                    .WithDeclaration(CatchDeclaration(ParseTypeName(exceptionType), Identifier(exceptionVariable)))
                    .WithBlock(catchBlock.Build()));

        _statements.Add(tryCatchStatement);
        return this;
    }

    public BlockBuilder AddComment(string comment)
    {
        _statements.Add(ParseStatement($"// {comment}"));
        return this;
    }

    public BlockBuilder AddReturnStatement(string expression = null)
    {
        _statements.Add(ReturnStatement(expression != null ? ParseExpression(expression) : null));
        return this;
    }

    public BlockBuilder AddObjectInitializer(string typeName, string variableName, Action<ObjectInitializerBuilder> initializerAction)
    {
        var initializerBuilder = new ObjectInitializerBuilder();
        initializerAction(initializerBuilder);

        var initialization = LocalDeclarationStatement(
            VariableDeclaration(ParseTypeName(typeName))
                .WithVariables(SingletonSeparatedList(
                    VariableDeclarator(Identifier(variableName))
                        .WithInitializer(EqualsValueClause(
                            ObjectCreationExpression(ParseTypeName(typeName))
                                .WithInitializer(initializerBuilder.Build()))))));

        _statements.Add(initialization);
        return this;
    }

    public BlockSyntax Build() => Block(_statements);
}

public class ObjectInitializerBuilder
{
    private readonly List<AssignmentExpressionSyntax> _assignments = new();

    public ObjectInitializerBuilder AddProperty(string propertyName, string value)
    {
        _assignments.Add(
            AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                IdentifierName(propertyName),
                ParseExpression(value)));
        return this;
    }

    public InitializerExpressionSyntax Build()
    {
        return InitializerExpression(
            SyntaxKind.ObjectInitializerExpression,
            SeparatedList<ExpressionSyntax>(_assignments));
    }
}