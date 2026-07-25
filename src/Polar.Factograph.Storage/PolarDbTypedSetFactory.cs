using Polar.DB.Typed;

namespace Polar.Factograph.Storage;

internal static class PolarDbTypedSetFactory
{
    public static DbSet<PolarDbResourceHeadRow> OpenResourceHeads(string rootPath) =>
        new(rootPath, options => options
            .Name(PolarDbPhysicalSchema.ResourceHeads.StorageName)
            .UseKey(row => row.ResourceId)
            .UseExternalKey(row => row.SourceCassetteId));

    public static DbSet<PolarDbTripleRow> OpenTriples(string rootPath) =>
        new(rootPath, options => options
            .Name(PolarDbPhysicalSchema.Triples.StorageName)
            .UseKey(row => row.TripleId)
            .UseExternalKey(row => row.Subject)
            .UseExternalKey(row => row.Predicate)
            .UseExternalKey(row => row.ObjectValue)
            .UseExternalKey(row => row.SourceCassetteId)
            .UseExternalKey(row => row.SubjectPredicateKey)
            .UseExternalKey(row => row.PredicateObjectKey));

    public static DbSet<PolarDbNameSearchRow> OpenNameSearch(string rootPath) =>
        new(rootPath, options => options
            .Name(PolarDbSearchPhysicalSchema.NameSearch.StorageName)
            .UseKey(row => row.SearchRowId)
            .UseExternalKey(row => row.SearchKey)
            .UseExternalKey(row => row.ResourceId)
            .UseExternalKey(row => row.SourceCassetteId));

    public static DbSet<PolarDbWordSearchRow> OpenWordSearch(string rootPath) =>
        new(rootPath, options => options
            .Name(PolarDbSearchPhysicalSchema.WordSearch.StorageName)
            .UseKey(row => row.SearchRowId)
            .UseExternalKey(row => row.Word)
            .UseExternalKey(row => row.ResourceId)
            .UseExternalKey(row => row.SourceCassetteId));
}
