using System.Text;
using Polar.Factograph.Domain;

namespace Polar.Factograph.Fog.Tests;

internal sealed class WritableDocumentCassette : IDisposable
{
    private WritableDocumentCassette(string root)
    {
        Root = root;
        Definition = new CassetteDefinition
        {
            Id = "current",
            Name = "Cassette",
            Path = root,
            Enabled = true,
            AllowWrite = true
        };
    }

    public string Root { get; }
    public CassetteDefinition Definition { get; }

    public static WritableDocumentCassette Create()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "polar-factograph-document-write-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return new WritableDocumentCassette(root);
    }

    public static MemoryStream Content(string value) =>
        new(Encoding.UTF8.GetBytes(value), writable: false);

    public string OriginalPath(CassetteDocumentWriteResult result) => Path.Combine(
        Root,
        "originals",
        result.FolderName,
        result.FileName);

    public void Dispose()
    {
        if (Directory.Exists(Root))
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}
