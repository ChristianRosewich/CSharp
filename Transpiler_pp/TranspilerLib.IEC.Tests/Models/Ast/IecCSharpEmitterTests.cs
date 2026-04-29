using System;
using TranspilerLib.IEC.Models.Ast;
using TranspilerLib.IEC.TestData;

namespace TranspilerLib.IEC.Models.Ast.Tests;

/// <summary>
/// Tests the first C# emission slice for the typed IEC AST subset.
/// </summary>
[TestClass]
public class IecCSharpEmitterTests
{
    [TestMethod]
    public void EmitExpression_TranslatesArithmeticAndComparisonOperators()
    {
        var expression = new IecBinaryExpression(
            new IecUnaryExpression(IecUnaryOperator.Negate, new IecIdentifierExpression("Left")),
            IecBinaryOperator.LessThan,
            new IecBinaryExpression(new IecIdentifierExpression("Right"), IecBinaryOperator.Add, new IecLiteralExpression(2.5)));

        var result = IecCSharpEmitter.EmitExpression(expression);

        Assert.AreEqual("((-Left) < (Right + 2.5))", result);
    }

    [TestMethod]
    public void EmitExpression_TranslatesSupportedFunctionCalls()
    {
        var expression = new IecFunctionCallExpression(
            "SEL",
            [
                new IecBinaryExpression(new IecIdentifierExpression("Flag"), IecBinaryOperator.Equal, new IecLiteralExpression(true)),
                new IecFunctionCallExpression("ABS", [new IecIdentifierExpression("Input")]),
                new IecFunctionCallExpression("LIMIT", [new IecLiteralExpression(0.0), new IecIdentifierExpression("Value"), new IecLiteralExpression(10.0)]),
            ]);

        var result = IecCSharpEmitter.EmitExpression(expression);

        Assert.AreEqual("((Flag == true) ? Math.Abs(Input) : Math.Clamp(0, Value, 10))", result);
    }

    [TestMethod]
    public void EmitMethod_UsesDeclarationsAndStatements_FromExportBasedCompilationUnit()
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

        var result = IecCSharpEmitter.EmitMethod(compilationUnit, "Compute");

        StringAssert.Contains(result, "public static void Compute(");
        StringAssert.Contains(result, "double lrAngleSetp");
        StringAssert.Contains(result, "double lrDeadTime");
        StringAssert.Contains(result, "bool _xNew;");
        StringAssert.Contains(result, "_lrAngleSetpDiff = ((Math.Abs(_lrAngleSetpDiff) < (_lrMaxAngleVel * lrCycleTime)) ? 0 : _lrAngleSetpDiff);");
    }

    [TestMethod]
    public void EmitMethod_TranslatesIfAndReturnStatements()
    {
        var compilationUnit = new IecCompilationUnit(
        [
            new IecVariableDeclaration("Flag", "BOOL", IecDeclarationSection.Local),
            new IecVariableDeclaration("Value", "LREAL", IecDeclarationSection.Local),
        ],
        [
            new IecIfStatement(
                new IecIdentifierExpression("Flag"),
                [new IecAssignmentStatement(new IecIdentifierExpression("Value"), new IecLiteralExpression(1.0))],
                [new IecAssignmentStatement(new IecIdentifierExpression("Value"), new IecLiteralExpression(2.0))]),
            new IecReturnStatement(new IecIdentifierExpression("Value")),
        ]);

        var result = IecCSharpEmitter.EmitMethod(compilationUnit, "Compute");

        StringAssert.Contains(result, "public static double Compute()");
        StringAssert.Contains(result, "if (Flag)");
        StringAssert.Contains(result, "Value = 1;");
        StringAssert.Contains(result, "else");
        StringAssert.Contains(result, "Value = 2;");
        StringAssert.Contains(result, "return Value;");
    }

    [TestMethod]
    public void EmitMethod_MapsInputInOutAndInstanceDeclarations_ToParametersAndState()
    {
        var compilationUnit = new IecCompilationUnit(
        [
            new IecVariableDeclaration("Input", "LREAL", IecDeclarationSection.Input),
            new IecVariableDeclaration("Memory", "LREAL", IecDeclarationSection.InOut),
            new IecVariableDeclaration("Output", "BOOL", IecDeclarationSection.Output),
            new IecVariableDeclaration("_state", "LREAL", IecDeclarationSection.Instance, "0.5"),
            new IecVariableDeclaration("localValue", "INT", IecDeclarationSection.Local),
        ],
        [
            new IecAssignmentStatement(new IecIdentifierExpression("localValue"), new IecLiteralExpression(1)),
            new IecReturnStatement(new IecIdentifierExpression("_state")),
        ]);

        var result = IecCSharpEmitter.EmitMethod(compilationUnit, "Step");

        StringAssert.Contains(result, "private static double _state = 0.5;");
        StringAssert.Contains(result, "public static double Step(double Input, ref double Memory, out bool Output)");
        StringAssert.Contains(result, "int localValue;");
        Assert.IsFalse(result.Contains("double Input;", StringComparison.Ordinal));
        Assert.IsFalse(result.Contains("double Memory;", StringComparison.Ordinal));
        Assert.IsFalse(result.Contains("bool Output;", StringComparison.Ordinal));
    }

    [TestMethod]
    public void EmitMethod_UsesExportDeclarationSections_ForSignatureAndState()
    {
        var fixture = IecExportFixtureData.LoadMfComputeAlignmentRot();
        var declarations = IecDeclarationExtractor.ExtractDeclarations(fixture.DeclarationText);
        var compilationUnit = new IecCompilationUnit(
            declarations,
            [new IecReturnStatement(new IecIdentifierExpression("_result"))]);

        var result = IecCSharpEmitter.EmitMethod(compilationUnit, "Compute");

        StringAssert.Contains(result, "private static double _lrAngleAct;");
        StringAssert.Contains(result, "private static bool _xNew;");
        StringAssert.Contains(result, "public static double Compute(double lrAngleSetp, double lrAngleAct, double lrLastValue, double lrMaxAngleVel, double lrActualCfgDec, double lrAngleVelAlign, double lrDeadTime, double lrDeadBandThreshold, double lrKdFactor, ref double lrMemAngSetp, ref double lrMemAngAct)");
        var methodBodyStart = result.IndexOf("public static double Compute(", StringComparison.Ordinal);
        Assert.IsTrue(methodBodyStart >= 0);
        var methodBody = result[methodBodyStart..];
        Assert.IsFalse(methodBody.Contains($"{Environment.NewLine}        double _lrAngleAct;", StringComparison.Ordinal));
    }
}
