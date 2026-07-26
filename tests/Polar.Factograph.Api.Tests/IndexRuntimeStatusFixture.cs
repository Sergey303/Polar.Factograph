namespace Polar.Factograph.Api.Tests;

internal sealed class IndexRuntimeStatusFixture : IDisposable
{
    private IndexRuntimeStatusFixture(string root)
    {
        Root = root;
    }

    public string Root { get; }

    public static IndexRuntimeStatusFixture Create()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "polar-factograph-index-status-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return new IndexRuntimeStatusFixture(root);
    }

    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}
