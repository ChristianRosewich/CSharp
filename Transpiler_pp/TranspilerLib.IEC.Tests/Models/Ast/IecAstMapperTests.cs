using TranspilerLib.Data;
using TranspilerLib.IEC.Models.Ast;
using TranspilerLib.IEC.Models.Scanner;

namespace TranspilerLib.IEC.Models.Ast.Tests;

[TestClass]
public class IecAstMapperTests
{
    [TestMethod]
    public void TryGetAssignmentStatement_ReturnsAttachedTypedNode()
    {
        var assignment = new IecAssignmentStatement(
            new IecIdentifierExpression("Target"),
            new IecLiteralExpression(1));
        var block = new IECCodeBlock
        {
            Code = "Target := 1",
            Type = CodeBlockType.Assignment,
            AstNode = assignment,
        };

        var result = IecAstMapper.TryGetAssignmentStatement(block, out var actual);

        Assert.IsTrue(result);
        Assert.AreSame(assignment, actual);
    }

    [TestMethod]
    public void TryGetAssignmentStatement_ReturnsFalseWithoutTypedNode()
    {
        var block = new IECCodeBlock
        {
            Code = "Target := 1",
            Type = CodeBlockType.Assignment,
        };

        var result = IecAstMapper.TryGetAssignmentStatement(block, out var actual);

        Assert.IsFalse(result);
        Assert.IsNull(actual);
    }

    [TestMethod]
    public void TryAttachAssignmentStatement_MapsBinaryExpressionWithPrecedence()
    {
        var assignment = new IECCodeBlock
        {
            Code = ":=",
            Type = CodeBlockType.Assignment,
        };
        _ = new IECCodeBlock { Code = "Target", Type = CodeBlockType.Variable, Parent = assignment };
        _ = new IECCodeBlock { Code = "Left", Type = CodeBlockType.Variable, Parent = assignment };
        _ = new IECCodeBlock { Code = "+", Type = CodeBlockType.Operation, Parent = assignment };
        _ = new IECCodeBlock { Code = "Middle", Type = CodeBlockType.Variable, Parent = assignment };
        _ = new IECCodeBlock { Code = "*", Type = CodeBlockType.Operation, Parent = assignment };
        _ = new IECCodeBlock { Code = "Right", Type = CodeBlockType.Variable, Parent = assignment };

        var result = IecAstMapper.TryAttachAssignmentStatement(assignment);

        Assert.IsTrue(result);
        Assert.IsInstanceOfType<IecAssignmentStatement>(assignment.AstNode);
        var statement = (IecAssignmentStatement)assignment.AstNode!;
        Assert.IsInstanceOfType<IecBinaryExpression>(statement.Value);
        var addExpression = (IecBinaryExpression)statement.Value;
        Assert.AreEqual(IecBinaryOperator.Add, addExpression.OperatorType);
        Assert.IsInstanceOfType<IecIdentifierExpression>(addExpression.Left);
        Assert.IsInstanceOfType<IecBinaryExpression>(addExpression.Right);
        var multiplyExpression = (IecBinaryExpression)addExpression.Right;
        Assert.AreEqual(IecBinaryOperator.Multiply, multiplyExpression.OperatorType);
    }

    [TestMethod]
    public void TryAttachAssignmentStatement_MapsFunctionCallArgumentBinaryExpression()
    {
        var assignment = new IECCodeBlock
        {
            Code = ":=",
            Type = CodeBlockType.Assignment,
        };
        _ = new IECCodeBlock { Code = "Target", Type = CodeBlockType.Variable, Parent = assignment };
        _ = new IECCodeBlock { Code = "LIMIT(", Type = CodeBlockType.Function, Parent = assignment };
        _ = new IECCodeBlock { Code = "Low", Type = CodeBlockType.Variable, Parent = assignment };
        _ = new IECCodeBlock { Code = ",", Type = CodeBlockType.Operation, Parent = assignment };
        _ = new IECCodeBlock { Code = "Value", Type = CodeBlockType.Variable, Parent = assignment };
        _ = new IECCodeBlock { Code = "/", Type = CodeBlockType.Operation, Parent = assignment };
        _ = new IECCodeBlock { Code = "Divisor", Type = CodeBlockType.Variable, Parent = assignment };
        _ = new IECCodeBlock { Code = ",", Type = CodeBlockType.Operation, Parent = assignment };
        _ = new IECCodeBlock { Code = "High", Type = CodeBlockType.Variable, Parent = assignment };

        var result = IecAstMapper.TryAttachAssignmentStatement(assignment);

        Assert.IsTrue(result);
        Assert.IsInstanceOfType<IecAssignmentStatement>(assignment.AstNode);
        var statement = (IecAssignmentStatement)assignment.AstNode!;
        Assert.IsInstanceOfType<IecFunctionCallExpression>(statement.Value);
        var functionCall = (IecFunctionCallExpression)statement.Value;
        Assert.AreEqual("LIMIT", functionCall.FunctionName);
        Assert.AreEqual(3, functionCall.Arguments.Count);
        Assert.IsInstanceOfType<IecBinaryExpression>(functionCall.Arguments[1]);
        Assert.AreEqual(IecBinaryOperator.Divide, ((IecBinaryExpression)functionCall.Arguments[1]).OperatorType);
    }

    [TestMethod]
    public void TryAttachAssignmentStatement_MapsComparisonWithNestedFunctionCallArgument()
    {
        var assignment = new IECCodeBlock
        {
            Code = ":=",
            Type = CodeBlockType.Assignment,
        };
        _ = new IECCodeBlock { Code = "Target", Type = CodeBlockType.Variable, Parent = assignment };
        _ = new IECCodeBlock { Code = "SEL(", Type = CodeBlockType.Function, Parent = assignment };
        _ = new IECCodeBlock { Code = "ABS(", Type = CodeBlockType.Function, Parent = assignment };
        _ = new IECCodeBlock { Code = "Diff", Type = CodeBlockType.Variable, Parent = assignment };
        _ = new IECCodeBlock { Code = "<", Type = CodeBlockType.Operation, Parent = assignment };
        _ = new IECCodeBlock { Code = "Max", Type = CodeBlockType.Variable, Parent = assignment };
        _ = new IECCodeBlock { Code = "*", Type = CodeBlockType.Operation, Parent = assignment };
        _ = new IECCodeBlock { Code = "Cycle", Type = CodeBlockType.Variable, Parent = assignment };
        _ = new IECCodeBlock { Code = ",", Type = CodeBlockType.Operation, Parent = assignment };
        _ = new IECCodeBlock { Code = "0.0", Type = CodeBlockType.Number, Parent = assignment };
        _ = new IECCodeBlock { Code = ",", Type = CodeBlockType.Operation, Parent = assignment };
        _ = new IECCodeBlock { Code = "Diff", Type = CodeBlockType.Variable, Parent = assignment };

        var result = IecAstMapper.TryAttachAssignmentStatement(assignment);

        Assert.IsTrue(result);
        Assert.IsInstanceOfType<IecAssignmentStatement>(assignment.AstNode);
        var statement = (IecAssignmentStatement)assignment.AstNode!;
        Assert.IsInstanceOfType<IecFunctionCallExpression>(statement.Value);
        var functionCall = (IecFunctionCallExpression)statement.Value;
        Assert.AreEqual("SEL", functionCall.FunctionName);
        Assert.AreEqual(3, functionCall.Arguments.Count);
        Assert.IsInstanceOfType<IecBinaryExpression>(functionCall.Arguments[0]);

        var comparison = (IecBinaryExpression)functionCall.Arguments[0];
        Assert.AreEqual(IecBinaryOperator.LessThan, comparison.OperatorType);
        Assert.IsInstanceOfType<IecFunctionCallExpression>(comparison.Left);
        Assert.IsInstanceOfType<IecBinaryExpression>(comparison.Right);
        Assert.AreEqual(IecBinaryOperator.Multiply, ((IecBinaryExpression)comparison.Right).OperatorType);
    }
}
