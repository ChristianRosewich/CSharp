using RepoMigrator.App.Logic.Models;

namespace RepoMigrator.Tests;

[TestClass]
public sealed class TargetSelectionResultTests
{
    [TestMethod]
    public void TargetSelectionResult_Defaults_AreInitialized()
    {
        var result = new TargetSelectionResult();

        Assert.AreEqual(0, result.Branches.Count);
        Assert.IsNull(result.DefaultBranch);
    }

    [TestMethod]
    public void TargetSelectionResult_AssignedValues_ArePreserved()
    {
        var result = new TargetSelectionResult
        {
            Branches = ["main", "release"],
            DefaultBranch = "main"
        };

        CollectionAssert.AreEqual(new[] { "main", "release" }, result.Branches.ToArray());
        Assert.AreEqual("main", result.DefaultBranch);
    }
}
