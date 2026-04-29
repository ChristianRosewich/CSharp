using System.Linq;
using TranspilerLib.IEC.Models.Ast;
using TranspilerLib.IEC.TestData;

namespace TranspilerLib.IEC.Models.Ast.Tests;

/// <summary>
/// Tests declaration extraction and symbol-table construction for realistic IEC export fixtures.
/// </summary>
[TestClass]
public class IecDeclarationExtractorTests
{
    [TestMethod]
    public void ExtractDeclarations_Reads_ExportDeclarationSections()
    {
        var fixture = IecExportFixtureData.LoadMfComputeAlignmentRot();

        var declarations = IecDeclarationExtractor.ExtractDeclarations(fixture.DeclarationText);

        Assert.IsTrue(declarations.Count >= 20, "The export fixture should yield a representative declaration set.");
        Assert.IsTrue(declarations.Any(declaration => declaration.Identifier == "lrAngleSetp" && declaration.Section == IecDeclarationSection.Input && declaration.TypeName == "LREAL"));
        Assert.IsTrue(declarations.Any(declaration => declaration.Identifier == "lrMemAngSetp" && declaration.Section == IecDeclarationSection.InOut && declaration.TypeName == "LREAL"));
        Assert.IsTrue(declarations.Any(declaration => declaration.Identifier == "_lrAngleAct" && declaration.Section == IecDeclarationSection.Instance && declaration.TypeName == "LREAL"));
        Assert.IsTrue(declarations.Any(declaration => declaration.Identifier == "lrDeadTime" && declaration.InitializerText == "0.1"));
        Assert.IsTrue(declarations.Any(declaration => declaration.Identifier == "_xNew" && declaration.TypeName == "BOOL"));
    }

    [TestMethod]
    public void CreateCompilationUnit_Builds_SymbolTable_From_ExportDeclarations()
    {
        var fixture = IecExportFixtureData.LoadMfComputeAlignmentRot();

        var compilationUnit = IecDeclarationExtractor.CreateCompilationUnit(fixture.DeclarationText, fixture.ImplementationText);

        Assert.IsTrue(compilationUnit.Declarations.Count >= 20, "The compilation unit should contain the extracted declarations.");
        Assert.IsTrue(compilationUnit.Symbols.TryGet("lrAngleVelAlign", out var angleVelAlignDeclaration));
        Assert.IsNotNull(angleVelAlignDeclaration);
        Assert.AreEqual(IecDeclarationSection.Input, angleVelAlignDeclaration.Section);
        Assert.AreEqual("LREAL", angleVelAlignDeclaration.TypeName);

        Assert.IsTrue(compilationUnit.Symbols.TryGet("_lrAxisVelTarget", out var axisVelTargetDeclaration));
        Assert.IsNotNull(axisVelTargetDeclaration);
        Assert.AreEqual(IecDeclarationSection.Instance, axisVelTargetDeclaration.Section);
        Assert.AreEqual("LREAL", axisVelTargetDeclaration.TypeName);
    }
}
