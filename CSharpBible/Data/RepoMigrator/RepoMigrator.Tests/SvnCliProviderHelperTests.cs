using System.Reflection;
using RepoMigrator.Providers.SvnCli;

namespace RepoMigrator.Tests;

[TestClass]
public sealed class SvnCliProviderHelperTests
{
    [TestMethod]
    public void ParseMissingFromStatus_ReturnsMissingAndObstructedEntries()
    {
        const string sStatusOutput = "!       src/old.cs\nM       src/active.cs\n~       docs\n?       new.txt\n";

        var arrMissing = InvokeParseMissingFromStatus(sStatusOutput).ToArray();

        CollectionAssert.AreEqual(new[] { "src/old.cs", "docs" }, arrMissing);
    }

    [TestMethod]
    [DataRow("C:/wc", "C:/wc/.svn/entries", true)]
    [DataRow("C:/wc", "C:/wc/src/.svn/text-base/file.svn-base", true)]
    [DataRow("C:/wc", "C:/wc/src/file.cs", false)]
    [DataRow("C:/wc", "C:/wc", false)]
    public void IsSvnAdministrativePath_ReturnsExpectedValue(string sRootPath, string sCandidatePath, bool xExpected)
    {
        var xActual = InvokeIsSvnAdministrativePath(sRootPath, sCandidatePath);

        Assert.AreEqual(xExpected, xActual);
    }

    [TestMethod]
    public void EscapeProp_EscapesQuoteCharacters()
    {
        var sActual = InvokeEscapeProp("a\"b\"c");

        Assert.AreEqual("a\\\"b\\\"c", sActual);
    }

    private static IEnumerable<string> InvokeParseMissingFromStatus(string sStatusOutput)
    {
        var method = typeof(SvnCliProvider).GetMethod("ParseMissingFromStatus", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        var result = method.Invoke(null, new object[] { sStatusOutput }) as IEnumerable<string>;
        Assert.IsNotNull(result);
        return result;
    }

    private static bool InvokeIsSvnAdministrativePath(string sRootPath, string sCandidatePath)
    {
        var method = typeof(SvnCliProvider).GetMethod("IsSvnAdministrativePath", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        return (bool)method.Invoke(null, new object[] { sRootPath, sCandidatePath })!;
    }

    private static string InvokeEscapeProp(string sValue)
    {
        var method = typeof(SvnCliProvider).GetMethod("EscapeProp", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(method);
        return (string)method.Invoke(null, new object[] { sValue })!;
    }
}
