using System;
using System.Linq;
using System.Text.RegularExpressions;
using TranspilerLib.Data;
using TranspilerLib.IEC.Models.Ast;
using TranspilerLib.IEC.Models.Scanner;
using TranspilerLib.IEC.TestData;

namespace TranspilerLib.IEC.Models.Analysis.Tests;

/// <summary>
/// Regression and complexity tests for the exported <c>MF_ComputeAlignmentRot</c> structured-text fixture.
/// The fixture represents realistic IEC code and serves as a stable benchmark while the IEC pipeline grows.
/// </summary>
[TestClass]
public class MfComputeAlignmentRotExportTests
{
    [TestMethod]
    public void ExportFixture_Contains_Declaration_And_Implementation_Text()
    {
        var fixture = IecExportFixtureData.LoadMfComputeAlignmentRot();

        StringAssert.Contains(fixture.DeclarationText, "METHOD PROTECTED MF_ComputeAlignmentRot : LREAL;");
        StringAssert.Contains(fixture.DeclarationText, "VAR_INPUT");
        StringAssert.Contains(fixture.ImplementationText, "MF_ComputeAlignmentRot := _result;");
        StringAssert.Contains(fixture.ImplementationText, "_lrAngleSetpDiff := SEL(");
    }

    [TestMethod]
    public void Implementation_Tokenize_Contains_Expected_ControlFlow_And_Assignments()
    {
        var fixture = IecExportFixtureData.LoadMfComputeAlignmentRot();
        var sut = new IECCode { OriginalCode = fixture.ImplementationText };

        var tokens = sut.Tokenize().ToList();
        var assignmentCount = tokens.Count(t => t.type == CodeBlockType.Operation && t.Code == ":=");
        var ifCount = tokens.Count(t => string.Equals(t.Code, "IF", StringComparison.OrdinalIgnoreCase));
        var functionCalls = tokens.Count(t => t.type == CodeBlockType.Function && (t.Code == "SEL" || t.Code == "LIMIT" || t.Code == "SQRT"));

        Assert.AreEqual(30, assignmentCount, "The assignment count should stay stable for this regression fixture.");
        Assert.AreEqual(2, ifCount, "The current export fixture contains two IF decisions.");
        Assert.IsTrue(functionCalls >= 6, "Expected multiple IEC function calls in the exported implementation.");
        Assert.IsTrue(tokens.Any(t => string.Equals(t.Code, "END_IF", StringComparison.OrdinalIgnoreCase)), "The exported implementation should include END_IF.");
    }

    [TestMethod]
    public void Implementation_ComplexityMetrics_Match_Current_Baseline()
    {
        var fixture = IecExportFixtureData.LoadMfComputeAlignmentRot();
        var metrics = AnalyzeStructuredText(fixture.ImplementationText);

        Assert.AreEqual(46, metrics.NonEmptyLineCount, "The non-empty line count is used as a regression baseline for the export fixture.");
        Assert.AreEqual(30, metrics.AssignmentCount, "The assignment count is used as a regression baseline for the export fixture.");
        Assert.AreEqual(2, metrics.DecisionCount, "The current structured-text fixture has two decision points.");
        Assert.AreEqual(3, metrics.CyclomaticComplexity, "Cyclomatic complexity is tracked as a baseline for this exported method.");
    }

    [TestMethod]
    public void Implementation_Parse_Attaches_TypedAssignments_For_Representative_Statements()
    {
        var fixture = IecExportFixtureData.LoadMfComputeAlignmentRot();
        var sut = new IECCode { OriginalCode = fixture.ImplementationText };

        var root = sut.Parse();
        var assignments = root.SubBlocks
            .OfType<IECCodeBlock>()
            .Where(block => block.Type == CodeBlockType.Assignment)
            .ToList();
        var typedAssignments = assignments
            .Select(block => block.AstNode)
            .OfType<IecAssignmentStatement>()
            .ToList();

        Assert.IsTrue(assignments.Count >= 10, "The export fixture should parse into a representative assignment set.");
        Assert.IsTrue(typedAssignments.Count >= 5, "A representative subset of the export assignments should already attach typed AST nodes.");
        Assert.IsTrue(typedAssignments.Any(statement => statement.Target.Identifier == "_lrAngleAct" && statement.Value is IecIdentifierExpression), "Simple identifier assignments should be mapped into typed AST nodes.");
        Assert.IsTrue(typedAssignments.Any(statement => statement.Target.Identifier == "_lrAngleSetpDiff" && statement.Value is IecFunctionCallExpression functionCall && functionCall.FunctionName == "SEL"), "Function-call assignments should be mapped for the export fixture.");
        Assert.IsTrue(typedAssignments.Any(statement => statement.Value is IecFunctionCallExpression), "The export fixture should yield typed function-call assignments in the currently supported subset.");
    }

    [TestMethod]
    public void Implementation_Parse_Maps_FunctionCall_Patterns_For_ExportFixture()
    {
        var fixture = IecExportFixtureData.LoadMfComputeAlignmentRot();
        var sut = new IECCode { OriginalCode = fixture.ImplementationText };

        var root = sut.Parse();
        var typedAssignments = root.SubBlocks
            .OfType<IECCodeBlock>()
            .Where(block => block.Type == CodeBlockType.Assignment)
            .Select(block => block.AstNode)
            .OfType<IecAssignmentStatement>()
            .ToList();

        var setpointDiffAssignments = typedAssignments.Where(statement => statement.Target.Identifier == "_lrAngleSetpDiff").ToList();

        Assert.IsTrue(setpointDiffAssignments.Count >= 1, "Expected at least one typed assignment for _lrAngleSetpDiff.");

        var setpointDiffAssignment = setpointDiffAssignments.First(statement => statement.Value is IecFunctionCallExpression);
        var typedFunctionCalls = typedAssignments
            .Select(statement => statement.Value)
            .OfType<IecFunctionCallExpression>()
            .ToList();

        Assert.IsInstanceOfType<IecFunctionCallExpression>(setpointDiffAssignment.Value);
        var setpointDiffFunction = (IecFunctionCallExpression)setpointDiffAssignment.Value;
        Assert.AreEqual("SEL", setpointDiffFunction.FunctionName);
        Assert.IsTrue(setpointDiffFunction.Arguments.Count >= 1, "The export fixture SEL call should preserve at least one argument in the current supported subset.");
        Assert.IsTrue(setpointDiffFunction.Arguments[0] is IecBinaryExpression or IecFunctionCallExpression or IecIdentifierExpression, "The first SEL argument should remain a typed expression from the export fixture.");

        Assert.IsTrue(typedFunctionCalls.Count >= 1, "The export fixture should currently retain at least one typed function-call assignment.");
        Assert.IsTrue(typedFunctionCalls.Any(function => function.Arguments.Count >= 1), "At least one typed function call from the export fixture should preserve arguments in the current supported subset.");
        Assert.IsTrue(typedFunctionCalls.SelectMany(function => function.Arguments).Any(argument => argument is IecIdentifierExpression or IecUnaryExpression or IecBinaryExpression), "Typed function calls should retain expression-shaped arguments from the export fixture.");
    }

    /// <summary>
    /// Computes simple regression-oriented metrics for structured-text source code.
    /// The current implementation intentionally focuses on the operators and control-flow
    /// markers that are already relevant for the imported export fixture.
    /// </summary>
    /// <param name="structuredText">The structured-text source code to analyze.</param>
    /// <returns>The calculated regression metrics.</returns>
    private static (int NonEmptyLineCount, int AssignmentCount, int DecisionCount, int CyclomaticComplexity) AnalyzeStructuredText(string structuredText)
    {
        var nonEmptyLineCount = structuredText
            .Split(new[] { Environment.NewLine }, StringSplitOptions.None)
            .Count(line => !string.IsNullOrWhiteSpace(line));
        var assignmentCount = Regex.Matches(structuredText, @":=").Count;
        var ifCount = Regex.Matches(structuredText, @"(?im)^\s*IF\b").Count;
        var elsifCount = Regex.Matches(structuredText, @"(?im)^\s*ELSIF\b").Count;
        var andCount = Regex.Matches(structuredText, @"(?i)\bAND\b").Count;
        var orCount = Regex.Matches(structuredText, @"(?i)\bOR\b").Count;
        var decisionCount = ifCount + elsifCount + andCount + orCount;
        return (nonEmptyLineCount, assignmentCount, decisionCount, 1 + decisionCount);
    }
}
