using Polar.DB.Typed;

namespace Polar.Factograph.Storage;

internal sealed class PolarDbTypedStoreSets : IDisposable
{
    private bool _disposed;

    private PolarDbTypedStoreSets(
        string generationPath,
        DbSet<PolarDbResourceHeadRow> resourceHeads,
        DbSet<PolarDbTripleRow> triples,
        DbSet<PolarDbNameSearchRow> nameSearch,
        DbSet<PolarDbWordSearchRow> wordSearch)
    {
        GenerationPath = generationPath;
        ResourceHeads = resourceHeads;
        Triples = triples;
        NameSearch = nameSearch;
        WordSearch = wordSearch;
    }

    public string GenerationPath { get; }

    public DbSet<PolarDbResourceHeadRow> ResourceHeads { get; }

    public DbSet<PolarDbTripleRow> Triples { get; }

    public DbSet<PolarDbNameSearchRow> NameSearch { get; }

    public DbSet<PolarDbWordSearchRow> WordSearch { get; }

    public static PolarDbTypedStoreSets OpenCurrent(string indexRoot)
    {
        string generationPath = FileSystemIndexGeneration.GetCurrentGenerationPath(indexRoot)
            ?? throw new FileNotFoundException(
                $"The project index has no CURRENT generation: {Path.GetFullPath(indexRoot)}");

        if (!Directory.Exists(generationPath))
        {
            throw new DirectoryNotFoundException(
                $"The project index CURRENT generation does not exist: {generationPath}");
        }

        DbSet<PolarDbResourceHeadRow>? resourceHeads = null;
        DbSet<PolarDbTripleRow>? triples = null;
        DbSet<PolarDbNameSearchRow>? nameSearch = null;
        DbSet<PolarDbWordSearchRow>? wordSearch = null;

        try
        {
            resourceHeads = PolarDbTypedSetFactory.OpenResourceHeads(generationPath);
            triples = PolarDbTypedSetFactory.OpenTriples(generationPath);
            nameSearch = PolarDbTypedSetFactory.OpenNameSearch(generationPath);
            wordSearch = PolarDbTypedSetFactory.OpenWordSearch(generationPath);
            return new PolarDbTypedStoreSets(
                generationPath,
                resourceHeads,
                triples,
                nameSearch,
                wordSearch);
        }
        catch
        {
            wordSearch?.Dispose();
            nameSearch?.Dispose();
            triples?.Dispose();
            resourceHeads?.Dispose();
            throw;
        }
    }

    public void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PolarDbTypedProjectStore));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        WordSearch.Dispose();
        NameSearch.Dispose();
        Triples.Dispose();
        ResourceHeads.Dispose();
    }
}
