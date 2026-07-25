using Polar.DB.Typed;

namespace Polar.Factograph.Storage;

internal static class PolarDbTypedExternalIndexBuilder
{
    private const string Sentinel = "__polar_factograph_build_external_index__";

    public static void Build(
        DbSet<PolarDbResourceHeadRow> resourceHeads,
        DbSet<PolarDbTripleRow> triples,
        DbSet<PolarDbNameSearchRow> names,
        DbSet<PolarDbWordSearchRow> words)
    {
        _ = resourceHeads.Find(row => row.SourceCassetteId, Sentinel);

        _ = triples.Find(row => row.Subject, Sentinel);
        _ = triples.Find(row => row.Predicate, Sentinel);
        _ = triples.Find(row => row.ObjectValue, Sentinel);
        _ = triples.Find(row => row.SourceCassetteId, Sentinel);
        _ = triples.Find(row => row.SubjectPredicateKey, Sentinel);
        _ = triples.Find(row => row.PredicateObjectKey, Sentinel);

        _ = names.Find(row => row.SearchKey, Sentinel);
        _ = names.Find(row => row.ResourceId, Sentinel);
        _ = names.Find(row => row.SourceCassetteId, Sentinel);

        _ = words.Find(row => row.Word, Sentinel);
        _ = words.Find(row => row.ResourceId, Sentinel);
        _ = words.Find(row => row.SourceCassetteId, Sentinel);
    }
}
