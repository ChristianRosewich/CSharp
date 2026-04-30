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
    public void ExtractDeclarations_StoresDeclarationMetadata()
    {
        var declarations = IecDeclarationExtractor.ExtractDeclarations("VAR_INPUT\nInput : LREAL := 1.5;\nEND_VAR");
        var declaration = declarations.Single();

        Assert.AreEqual(IecDeclarationSection.Input, declaration.DeclarationMetadata.Section);
        Assert.AreEqual("1.5", declaration.DeclarationMetadata.InitializerText);
    }

    [TestMethod]
    public void ExtractDeclarations_ReadsOutputSection()
    {
        var declarations = IecDeclarationExtractor.ExtractDeclarations("VAR_OUTPUT\nResult : BOOL;\nEND_VAR");
        var declaration = declarations.Single();

        Assert.AreEqual(IecDeclarationSection.Output, declaration.Section);
        Assert.AreEqual("Result", declaration.Identifier);
        Assert.AreEqual("BOOL", declaration.TypeName);
    }

    [TestMethod]
    public void ExtractDeclarations_SkipsDeclaration_WithMissingIdentifier()
    {
        var declarations = IecDeclarationExtractor.ExtractDeclarations("VAR\n: LREAL;\nValid : INT;\nEND_VAR");

        Assert.AreEqual(1, declarations.Count);
        Assert.AreEqual("Valid", declarations.Single().Identifier);
    }

    [TestMethod]
    public void DeclarationMetadata_InheritsSharedMetadataBase()
    {
        IecMetadata metadata = new IecDeclarationMetadata(IecDeclarationSection.Input, "1.5");

        Assert.IsInstanceOfType<IecDeclarationMetadata>(metadata);
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

    [TestMethod]
    public void CreateCompilationUnit_UsesEmptyStatements_ForCurrentImplementation()
    {
        var compilationUnit = IecDeclarationExtractor.CreateCompilationUnit("VAR\nValue : INT;\nEND_VAR", "Value := 1;");

        Assert.AreEqual(1, compilationUnit.Declarations.Count);
        Assert.AreEqual(0, compilationUnit.Statements.Count);
    }
}
