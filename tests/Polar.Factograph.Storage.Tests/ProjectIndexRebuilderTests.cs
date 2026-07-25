using Polar.Factograph.Fog;
using Xunit;

namespace Polar.Factograph.Storage.Tests;

public sealed class ProjectIndexRebuilderTests
{
    [Fact]
    public async Task RebuildAsync_CommitsOnlyAfterAllRowsAreWritten()
    {
        RecordingWriter writer = new();
        ProjectIndexRebuilder rebuilder = new();

        ProjectIndexBuildStatistics statistics = await rebuilder.RebuildAsync(
            ReadRecords(CreateRecord("resource-1"), CreateRecord("resource-2")),
            writer);

        Assert.Equal(2, statistics.Resources);
        Assert.Equal(4, statistics.Triples);
        Assert.Equal(2, writer.Resources.Count);
        Assert.Equal(4, writer.Triples.Count);
        Assert.True(writer.Committed);
        Assert.False(writer.Aborted);
    }

    [Fact]
    public async Task RebuildAsync_AbortsGenerationWhenAWriteFails()
    {
        RecordingWriter writer = new(failOnResourceNumber: 2);
        ProjectIndexRebuilder rebuilder = new();

        IOException exception = await Assert.ThrowsAsync<IOException>(() => rebuilder.RebuildAsync(
            ReadRecords(CreateRecord("resource-1"), CreateRecord("resource-2")),
            writer));

        Assert.Contains("simulated", exception.Message, StringComparison.Ordinal);
        Assert.False(writer.Committed);
        Assert.True(writer.Aborted);
    }

    private static FogCurrentRecord CreateRecord(string id) => new(
        id,
        LegacyFogVocabulary.Namespace + "person",
        new DateTime(2026, 7, 25, 10, 0, 0, DateTimeKind.Utc),
        new[]
        {
            new FogProperty(
                LegacyFogVocabulary.Namespace + "name",
                FogPropertyKind.Literal,
                id)
        },
        "cassette-a",
        "CassetteA",
        "/data/a.fog",
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

    private sealed class RecordingWriter : IProjectIndexGenerationWriter
    {
        private readonly int? _failOnResourceNumber;

        public RecordingWriter(int? failOnResourceNumber = null)
        {
            _failOnResourceNumber = failOnResourceNumber;
        }

        public List<PolarDbResourceHeadRow> Resources { get; } = new();

        public List<PolarDbTripleRow> Triples { get; } = new();

        public bool Committed { get; private set; }

        public bool Aborted { get; private set; }

        public ValueTask WriteResourceAsync(
            PolarDbResourceHeadRow resource,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int nextNumber = Resources.Count + 1;
            if (_failOnResourceNumber == nextNumber)
            {
                throw new IOException("simulated resource write failure");
            }

            Resources.Add(resource);
            return ValueTask.CompletedTask;
        }

        public ValueTask WriteTriplesAsync(
            IReadOnlyList<PolarDbTripleRow> triples,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Triples.AddRange(triples);
            return ValueTask.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Committed = true;
            return Task.CompletedTask;
        }

        public Task AbortAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Aborted = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
