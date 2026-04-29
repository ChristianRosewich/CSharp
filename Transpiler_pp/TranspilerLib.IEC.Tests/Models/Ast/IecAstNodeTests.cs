using TranspilerLib.IEC.Models.Ast;

namespace TranspilerLib.IEC.Models.Ast.Tests;

[TestClass]
public class IecAstNodeTests
{
    [TestMethod]
    public void CompilationUnit_StoresDeclarationsAndStatements()
    {
        var declaration = new IecVariableDeclaration("Value", "INT", sourcePos: 3);
        var statement = new IecAssignmentStatement(
            new IecIdentifierExpression("Value", 10),
            new IecLiteralExpression(5, 18),
            10);

        var unit = new IecCompilationUnit([declaration], [statement], 1);

        Assert.AreEqual(1, unit.SourcePos);
        Assert.AreEqual(1, unit.Declarations.Count);
        Assert.AreEqual("Value", unit.Declarations[0].Identifier);
        Assert.AreEqual("INT", unit.Declarations[0].TypeName);
        Assert.AreEqual(1, unit.Statements.Count);
        Assert.AreSame(statement, unit.Statements[0]);
    }

    [TestMethod]
    public void FunctionCall_StoresArguments()
    {
        var expression = new IecFunctionCallExpression(
            "rw_CONCAT",
            [new IecIdentifierExpression("a"), new IecLiteralExpression("B")],
            12);

        Assert.AreEqual("rw_CONCAT", expression.FunctionName);
        Assert.AreEqual(2, expression.Arguments.Count);
        Assert.IsInstanceOfType<IecIdentifierExpression>(expression.Arguments[0]);
        Assert.IsInstanceOfType<IecLiteralExpression>(expression.Arguments[1]);
        Assert.AreEqual(12, expression.SourcePos);
    }

    [TestMethod]
    public void BinaryExpression_StoresOperatorAndOperands()
    {
        var expression = new IecBinaryExpression(
            new IecIdentifierExpression("Left", 2),
            IecBinaryOperator.Add,
            new IecUnaryExpression(IecUnaryOperator.Negate, new IecLiteralExpression(4, 8), 7),
            5);

        Assert.AreEqual(IecBinaryOperator.Add, expression.OperatorType);
        Assert.IsInstanceOfType<IecIdentifierExpression>(expression.Left);
        Assert.IsInstanceOfType<IecUnaryExpression>(expression.Right);
        Assert.AreEqual(5, expression.SourcePos);
    }
}
