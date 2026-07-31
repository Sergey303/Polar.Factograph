using System.Runtime.CompilerServices;
using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Storage.Tests;

public sealed class PolarDbTypedProjectStoreTests
{
    [Fact]
    public async Task Rebuild_WritesAndReopensCompleteQueryableGeneration()
    {
        await using TemporaryDirectory directory = TemporaryDirectory.Create();
        await using PolarDbTypedIndexGenerationWriter writer =
            PolarDbTypedIndexGenerationWriter.Begin(
                directory.Path,
                Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"));
        ProjectIndexRebuilder rebuilder = new();

        ProjectIndexBuildStatistics statistics = await rebuilder.RebuildAsync(
            ReadRecords(CreateRecord()),
            writer);

        Assert.Equal(1, statistics.Resources);
        Assert.Equal(4, statistics.Triples);
        Assert.True(statistics.NameSearchRows > 0);
        Assert.True(statistics.WordSearchRows > 0);

        string currentPath = Assert.IsType<string>(
            FileSystemIndexGeneration.GetCurrentGenerationPath(directory.Path));
        Assert.True(Directory.Exists(currentPath));
        Assert.False(Directory.Exists(writer.StagingPath));

        HashSet<string> allowed = new(StringComparer.Ordinal) { "cassette-a" };
        using (PolarDbTypedProjectStore store = PolarDbTypedProjectStore.OpenCurrent(directory.Path))
        {
            ResourceHead? head = await store.GetResourceHeadAsync("person-1");
            Assert.NotNull(head);
            Assert.Equal("cassette-a", head.SourceCassetteId);

            ResourceHead storedHead = Assert.Single(store.ReadAllResourceHeads());
            Assert.Equal("person-1", storedHead.ResourceId);
            Assert.Equal(4, store.ReadAllTriples().Count);

            TripleRow[] outgoing = await ReadAllAsync(store.FindAsync(
                new TriplePattern(Subject: "person-1"),
                allowed));
            Assert.Equal(4, outgoing.Length);
            Assert.Contains(outgoing, triple =>
                triple.Predicate == LegacyFogVocabulary.Namespace + "member-of" &&
                triple.ObjectKind == TripleObjectKind.Iri &&
                triple.ObjectValue == "organization-1");

            IReadOnlyList<NameSearchHit> names = await store.FindNamesByKeyAsync(
                "ANN",
                allowed);
            NameSearchHit name = Assert.Single(names
                .Where(hit => hit.Value == "Anna Archive")
                .Distinct());
            Assert.Equal("en", name.Language);

            IReadOnlyList<WordSearchHit> words = await store.FindWordAsync(
                "HISTORICAL",
                allowed);
            WordSearchHit word = Assert.Single(words);
            Assert.Equal("Historical archive", word.Value);
        }

        using PolarDbTypedProjectStore reopened = PolarDbTypedProjectStore.OpenCurrent(directory.Path);
        ResourceHead? reopenedHead = await reopened.GetResourceHeadAsync("person-1");
        Assert.NotNull(reopenedHead);
        Assert.Equal("person-1", reopenedHead.ResourceId);
    }

    [Fact]
    public async Task Queries_ApplyCassetteVisibilityAndCompositePatterns()
    {
        await using TemporaryDirectory directory = TemporaryDirectory.Create();
        await using PolarDbTypedIndexGenerationWriter writer =
            PolarDbTypedIndexGenerationWriter.Begin(directory.Path);
        ProjectIndexRebuilder rebuilder = new();

        await rebuilder.RebuildAsync(
            ReadRecords(
                CreateRecord("person-1", "cassette-a"),
                CreateRecord("person-2", "cassette-b")),
            writer);

        using PolarDbTypedProjectStore store = PolarDbTypedProjectStore.OpenCurrent(directory.Path);
        HashSet<string> allowed = new(StringComparer.Ordinal) { "cassette-a" };

        TripleRow[] visible = await ReadAllAsync(store.FindAsync(
            new TriplePattern(
                Predicate: LegacyFogVocabulary.Namespace + "name",
                ObjectKind: TripleObjectKind.Literal,
                ObjectValue: "Anna Archive"),
            allowed));
        TripleRow result = Assert.Single(visible);
        Assert.Equal("person-1", result.Subject);

        IReadOnlyList<NameSearchHit> names = await store.FindNamesByKeyAsync("ANN", allowed);
        Assert.All(names, hit => Assert.Equal("cassette-a", hit.SourceCassetteId));
        Assert.DoesNotContain(names, hit => hit.ResourceId == "person-2");
    }

    [Fact]
    public void OpenCurrent_RejectsMissingGeneration()
    {
        using TemporaryDirectory directory = TemporaryDirectory.Create();

        FileNotFoundException exception = Assert.Throws<FileNotFoundException>(
            () => PolarDbTypedProjectStore.OpenCurrent(directory.Path));

        Assert.Contains("no CURRENT generation", exception.Message, StringComparison.Ordinal);
    }

    private static FogCurrentRecord CreateRecord(
        string resourceId = "person-1",
        string cassetteId = "cassette-a") => new(
        resourceId,
        LegacyFogVocabulary.Namespace + "person",
        new DateTime(2026, 7, 25, 13, 0, 0, DateTimeKind.Utc),
        new[]
        {
            new FogProperty(
                LegacyFogVocabulary.Namespace + "name",
                FogPropertyKind.Literal,
                "Anna Archive",
                Language: "en"),
            new FogProperty(
                LegacyFogVocabulary.Namespace + "description",
                FogPropertyKind.Literal,
                "Historical archive"),
            new FogProperty(
                LegacyFogVocabulary.Namespace + "member-of",
                FogPropertyKind.Resource,
                "organization-1")
        },
        cassetteId,
        cassetteId,
        $"/{cassetteId}/meta/current.fog",
        SourceOrdinal: 1,
        IsSynthetic: false);

    private static async IAsyncEnumerable<FogCurrentRecord> ReadRecords(
        params FogCurrentRecord[] records)
    {
        foreach (FogCurrentRecord record in records)
        {
            yield return record;
            await Task.Yield();
        }
    }

    private static async Task<TripleRow[]> ReadAllAsync(
        IAsyncEnumerable<TripleRow> source,
        CancellationToken cancellationToken = default)
    {
        List<TripleRow> result = new();
        await foreach (TripleRow triple in source.WithCancellation(cancellationToken))
        {
            result.Add(triple);
        }

        return result.ToArray();
    }

    private sealed class TemporaryDirectory : IDisposable, IAsyncDisposable
    {
        private TemporaryDirectory(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryDirectory Create()
        {
            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "polar-factograph-typed-store-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TemporaryDirectory(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
