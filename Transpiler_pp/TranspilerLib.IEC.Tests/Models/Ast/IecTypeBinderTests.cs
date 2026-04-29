using System.Linq;
using TranspilerLib.IEC.Models.Ast;
using TranspilerLib.IEC.TestData;

namespace TranspilerLib.IEC.Models.Ast.Tests;

/// <summary>
/// Tests lightweight IEC type binding and inference for the current typed AST subset.
/// </summary>
[TestClass]
public class IecTypeBinderTests
{
    [TestMethod]
    public void Bind_InfersTypes_For_BoundAssignmentExpressions()
    {
        var statement = new IecAssignmentStatement(
            new IecIdentifierExpression("Target"),
            new IecBinaryExpression(
                new IecIdentifierExpression("Left"),
                IecBinaryOperator.Add,
                new IecLiteralExpression(1.5)));
        var compilationUnit = new IecCompilationUnit(
        [
            new IecVariableDeclaration("Target", "LREAL", IecDeclarationSection.Local),
            new IecVariableDeclaration("Left", "INT", IecDeclarationSection.Input),
        ],
        [statement]);
        var bindingResult = IecIdentifierBinder.Bind(compilationUnit);

        var result = IecTypeBinder.Bind(compilationUnit, bindingResult);

        Assert.IsTrue(result.StatementTypes.Any(entry => ReferenceEquals(entry.Key, statement) && entry.Value == "LREAL"));
        Assert.IsTrue(result.ExpressionTypes.Any(entry => ReferenceEquals(entry.Key, statement.Target) && entry.Value == "LREAL"));
        Assert.IsTrue(result.ExpressionTypes.Any(entry => ReferenceEquals(entry.Key, statement.Value) && entry.Value == "LREAL"));
        Assert.AreEqual(0, result.UnresolvedExpressions.Count);
    }

    [TestMethod]
    public void Bind_InfersBoolType_For_ComparisonExpression()
    {
        var comparison = new IecBinaryExpression(
            new IecIdentifierExpression("Actual"),
            IecBinaryOperator.LessThan,
            new IecIdentifierExpression("Limit"));
        var statement = new IecAssignmentStatement(new IecIdentifierExpression("Target"), comparison);
        var compilationUnit = new IecCompilationUnit(
        [
            new IecVariableDeclaration("Target", "BOOL", IecDeclarationSection.Local),
            new IecVariableDeclaration("Actual", "LREAL", IecDeclarationSection.Input),
            new IecVariableDeclaration("Limit", "LREAL", IecDeclarationSection.Input),
        ],
        [statement]);
        var bindingResult = IecIdentifierBinder.Bind(compilationUnit);

        var result = IecTypeBinder.Bind(compilationUnit, bindingResult);

        Assert.IsTrue(result.ExpressionTypes.Any(entry => ReferenceEquals(entry.Key, comparison) && entry.Value == "BOOL"));
        Assert.IsTrue(result.StatementTypes.Any(entry => ReferenceEquals(entry.Key, statement) && entry.Value == "BOOL"));
    }

    [TestMethod]
    public void Bind_InfersTypes_For_ExportBasedStatement()
    {
        var fixture = IecExportFixtureData.LoadMfComputeAlignmentRot();
        var declarations = IecDeclarationExtractor.ExtractDeclarations(fixture.DeclarationText);
        var statement = new IecAssignmentStatement(
            new IecIdentifierExpression("_lrAngleSetpDiff"),
            new IecFunctionCallExpression(
                "SEL",
                [
                    new IecBinaryExpression(
                        new IecFunctionCallExpression("ABS", [new IecIdentifierExpression("_lrAngleSetpDiff")]),
                        IecBinaryOperator.LessThan,
                        new IecBinaryExpression(
                            new IecIdentifierExpression("_lrMaxAngleVel"),
                            IecBinaryOperator.Multiply,
                            new IecIdentifierExpression("lrCycleTime"))),
                    new IecLiteralExpression(0.0),
                    new IecIdentifierExpression("_lrAngleSetpDiff"),
                ]));
        var compilationUnit = new IecCompilationUnit(declarations, [statement]);
        var bindingResult = IecIdentifierBinder.Bind(compilationUnit);

        var result = IecTypeBinder.Bind(compilationUnit, bindingResult);

        Assert.IsTrue(result.StatementTypes.Any(entry => ReferenceEquals(entry.Key, statement) && entry.Value == "LREAL"));
        Assert.IsTrue(result.ExpressionTypes.Any(entry => ReferenceEquals(entry.Key, statement.Value) && entry.Value == "LREAL"));
        Assert.IsTrue(result.ExpressionTypes.Any(entry => entry.Key is IecBinaryExpression binary && binary.OperatorType == IecBinaryOperator.LessThan && entry.Value == "BOOL"));
        Assert.IsTrue(result.UnresolvedExpressions.OfType<IecIdentifierExpression>().Any(identifier => identifier.Identifier == "lrCycleTime"));
    }
}
